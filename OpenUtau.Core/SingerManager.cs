// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : SingerManager.cs
// Version      : 1.6
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai (Delta) ]
// File Location: OpenUtau.Core/SingerManager.cs
// Description  : Singer lifecycle & memory manager. Optimized caching and Threading.
// ============================================================================
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

namespace OpenUtau.Core {
    public class SingerManager : SingletonBase<SingerManager> {
        public Dictionary<string, USinger> Singers { get; private set; } = new();
        public Dictionary<USingerType, List<USinger>> SingerGroups { get; private set; } = new();
        public List<USinger> DeltaSynthSingers { get; private set; } = new();

        private readonly ConcurrentQueue<USinger> reloadQueue = new();
        private CancellationTokenSource reloadCancellation;
        private HashSet<USinger> singersUsed = new();

        public void Initialize() => SearchAllSingers();

        public void SearchAllSingers() {
            Log.Information("Searching singers.");
            Directory.CreateDirectory(PathManager.Inst.SingersPath);
            var stopWatch = Stopwatch.StartNew();
            
            var singers = ClassicSingerLoader.FindAllSingers().Concat(Vogen.VogenSingerLoader.FindAllSingers()).Distinct().ToList();
            Singers = singers.ToDictionary(s => s.Id, s => s);
            SingerGroups = singers.GroupBy(s => s.SingerType).ToDictionary(g => g.Key, g => g.LocalizedOrderBy(singer => singer.LocalizedName).ToList());
            DeltaSynthSingers = singers.Where(s => s.IsDeltaSynthSinger).LocalizedOrderBy(singer => singer.LocalizedName).ToList();
            
            stopWatch.Stop();
            Log.Information($"Search all singers: {stopWatch.Elapsed}");
        }

        public USinger GetSinger(string name) {
            Log.Information($"Attach singer to track: {name}");
            name = name.Replace("%VOICE%", "");
            return Singers.TryGetValue(name, out var singer) ? singer : null;
        }

        public void ScheduleReload(USinger singer) {
            reloadQueue.Enqueue(singer);
            ScheduleReload();
        }

        private void ScheduleReload() {
            var newCancellation = new CancellationTokenSource();
            var oldCancellation = Interlocked.Exchange(ref reloadCancellation, newCancellation);
            oldCancellation?.Cancel();

            _ = Task.Run(async () => {
                try {
                    await Task.Delay(200, newCancellation.Token).ConfigureAwait(false);
                    await Refresh().ConfigureAwait(false);
                } catch (OperationCanceledException) { }
                finally {
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
                        if (retries == 0) Log.Error(e, $"Failed to reload {singer.Id}");
                        else {
                            Log.Error(e, $"Retrying reload {singer.Id}");
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

        public void ReleaseSingersNotInUse(UProject project) {
            var singersInUse = new HashSet<USinger>(project.tracks.Select(t => t.Singer).Where(s => s != null && s.Found));
            foreach (var singer in singersUsed.Where(s => !singersInUse.Contains(s))) {
                singer.FreeMemory();
            }
            singersUsed = singersInUse;
        }
    }
}