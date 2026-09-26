using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Classic;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.2 | Date: 2026-07-28
// Description: ปรับการหน่วงโหลดคลังเสียงให้ยกเลิกได้โดยไม่พักเธรดโดยไม่จำเป็น
// ============================================================================

namespace OpenUtau.Core {
    public class SingerManager : SingletonBase<SingerManager> {
        public Dictionary<string, USinger> Singers { get; private set; } = new Dictionary<string, USinger>();
        public Dictionary<USingerType, List<USinger>> SingerGroups { get; private set; } = new Dictionary<USingerType, List<USinger>>();

        private readonly ConcurrentQueue<USinger> reloadQueue = new ConcurrentQueue<USinger>();
        private CancellationTokenSource reloadCancellation;

        private HashSet<USinger> singersUsed = new HashSet<USinger>();

        public void Initialize() {
            SearchAllSingers();
        }

        public void SearchAllSingers() {
            Log.Information("Searching singers.");
            Directory.CreateDirectory(PathManager.Inst.SingersPath);
            var stopWatch = Stopwatch.StartNew();
            var singers = ClassicSingerLoader.FindAllSingers()
                .Concat(Vogen.VogenSingerLoader.FindAllSingers())
                .Distinct();
            Singers = singers
                .ToLookup(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());
            SingerGroups = singers
                .GroupBy(s => s.SingerType)
                .ToDictionary(s => s.Key, s => s.LocalizedOrderBy(singer => singer.LocalizedName).ToList());
            stopWatch.Stop();
            Log.Information($"Search all singers: {stopWatch.Elapsed}");
        }

        public USinger GetSinger(string name) {
            Log.Information($"Attach singer to track: {name}");
            name = name.Replace("%VOICE%", "");
            if (Singers.ContainsKey(name)) {
                return Singers[name];
            }
            return null;
        }

        public void ScheduleReload(USinger singer) {
            reloadQueue.Enqueue(singer);
            ScheduleReload();
        }

        private void ScheduleReload() {
            var newCancellation = new CancellationTokenSource();
            var oldCancellation = Interlocked.Exchange(ref reloadCancellation, newCancellation);
            if (oldCancellation != null) {
                oldCancellation.Cancel();
            }

            _ = Task.Run(async () => {
                try {
                    await Task.Delay(200, newCancellation.Token).ConfigureAwait(false);
                    await Refresh().ConfigureAwait(false);
                } catch (OperationCanceledException) when (newCancellation.IsCancellationRequested) {
                    // การร้องขอครั้งใหม่เข้ามาแทนที่งานเดิม จึงยกเลิกงานหน่วงเดิมตามปกติ
                } finally {
                    Interlocked.CompareExchange(ref reloadCancellation, null, newCancellation);
                    newCancellation.Dispose();
                }
            });
        }

        private async Task Refresh() {
            var singers = new HashSet<USinger>();
            while (reloadQueue.TryDequeue(out USinger singer)) {
                singers.Add(singer);
            }
            foreach (var singer in singers) {
                Log.Information($"Reloading {singer.Id}");
                PostToUI(() => DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Reloading {singer.Id}")));
                int retries = 5;
                while (retries > 0) {
                    retries--;
                    try {
                        singer.Reload();
                        break;
                    } catch (Exception e) {
                        if (retries == 0) {
                            Log.Error(e, $"Failed to reload {singer.Id}");
                        } else {
                            Log.Error(e, $"Retrying reload {singer.Id}");
                            // Non-blocking delay: Thread.Sleep here would tie up a
                            // threadpool thread for up to 1s per failed singer,
                            // which can starve other queued work (rendering, UI
                            // dispatch, etc.) under load.
                            await Task.Delay(200).ConfigureAwait(false);
                        }
                    }
                }
                Log.Information($"Reloaded {singer.Id}");
                PostToUI(() => {
                    DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, $"Reloaded {singer.Id}"));
                    DocManager.Inst.ExecuteCmd(new OtoChangedNotification(external: true));
                });
            }
        }

        private static void PostToUI(Action action) {
            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, DocManager.Inst.MainScheduler);
        }

        //Check which singers are in use and free memory for those that are not
        public void ReleaseSingersNotInUse(UProject project) {
            //Check which singers are in use
            var singersInUse = new HashSet<USinger>();
            foreach (var track in project.tracks) {
                var singer = track.Singer;
                if (singer != null && singer.Found && !singersInUse.Contains(singer)) {
                    singersInUse.Add(singer);
                }
            }
            //Release singers that are no longer in use
            foreach (var singer in singersUsed) {
                if (!singersInUse.Contains(singer)) {
                    singer.FreeMemory();
                }
            }
            //Update singers used
            singersUsed = singersInUse;
        }
    }
}
