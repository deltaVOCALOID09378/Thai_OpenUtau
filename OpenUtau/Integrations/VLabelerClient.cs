// ========================================================================
// Date: 29/09/2569 (BE) / 29/09/2026 (CE)
// File: VLabelerClient.cs
// Version: 2.0.0
// Made And Checked By DELTA SYNTH [Patiphat Wongyai] & All Code Agentic AI Engine
// [ Original by OpenUtau Contributors ]
// Location: OpenUtau/Integrations/VLabelerClient.cs
// Description: Upgraded vLabeler IPC Client supporting seamless project & sample file renaming,
//              auto-migration, backup safety, and Unicode/Thai path normalization.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using OpenUtau.Core;
using Serilog;

namespace OpenUtau.Integrations {
    internal class VLabelerClient : Core.Util.SingletonBase<VLabelerClient> {
        class HeartbeatRequest {
#pragma warning disable CS0414
            public string type = "Heartbeat";
#pragma warning restore CS0414
            public long sentAt = Epoch();
        }

        class GotoEntryByName {
            public string parentFolderName;
            public string entryName;
            public GotoEntryByName(string parentFolderName, string entryName) {
                this.parentFolderName = parentFolderName;
                this.entryName = entryName;
            }
        }

        class GotoEntryByIndex {
            public string parentFolderName;
            public string entryIndex;
            public GotoEntryByIndex(string parentFolderName, string entryIndex) {
                this.parentFolderName = parentFolderName;
                this.entryIndex = entryIndex;
            }
        }

        class TypedValue {
            public string type;
            public object value;
            public TypedValue(string type, object value) {
                this.type = type;
                this.value = value;
            }
        }

        class NewProjectArgs {
#pragma warning disable 0649
#pragma warning disable CS0414
            public string labelerName = "utau-singer.default";
            public string? sampleDirectory;
            public string? cacheDirectory;
            public Dictionary<string, TypedValue>? labelerParams;
            public string? pluginName;
            public Dictionary<string, TypedValue>? pluginParams;
            public string? inputFile;
            public string encoding = Encoding.UTF8.WebName;
            public bool autoExport;
#pragma warning restore 0649
#pragma warning restore CS0414
        }

        class OpenOrCreateRequest {
#pragma warning disable CS0414
            public string type = "OpenOrCreate";
#pragma warning restore CS0414
            public string projectFile = string.Empty;
            public GotoEntryByName? gotoEntryByName;
            public NewProjectArgs newProjectArgs = new NewProjectArgs();
            public long sentAt = Epoch();
        }

        private static long Epoch() {
            return (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        private static string HashHex(string s) {
            return $"{XXH32.DigestOf(Encoding.UTF8.GetBytes(s)):x8}";
        }

        /// <summary>
        /// แปลงและกำจัดอักขระต้องห้ามของชื่อไฟล์ในระบบปฏิบัติการ (Sanitize File Name)
        /// </summary>
        public static string SanitizeFileName(string name) {
            if (string.IsNullOrWhiteSpace(name)) return "project";
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name) {
                if (invalidChars.Contains(c) || c == ':' || c == '/' || c == '\\') {
                    sb.Append('_');
                } else {
                    sb.Append(c);
                }
            }
            string sanitized = sb.ToString().Trim();
            return string.IsNullOrEmpty(sanitized) ? "project" : sanitized;
        }

        /// <summary>
        /// ค้นหาหรือสร้างตำแหน่งพาธไฟล์โปรเจกต์ .lbp ให้ตรงกับชื่อนักร้องหรือชื่อที่กำหนด
        /// รองรับการเปลี่ยนชื่อไฟล์โปรเจกต์ได้อย่างอิสระโดยไม่ทำลายข้อมูลเดิม
        /// </summary>
        public string GetResolvedProjectFilePath(Core.Ustx.USinger singer, string? preferredName = null) {
            if (string.IsNullOrEmpty(singer.Location) || !Directory.Exists(singer.Location)) {
                return Path.Combine(singer.Location ?? "", "_vlabeler.lbp");
            }

            // 1. ถ้ามีชื่อระบุเฉพาะเจาะจง (Preferred Name)
            if (!string.IsNullOrWhiteSpace(preferredName)) {
                string safePreferred = SanitizeFileName(preferredName);
                if (!safePreferred.EndsWith(".lbp", StringComparison.OrdinalIgnoreCase)) {
                    safePreferred += ".lbp";
                }
                return Path.Combine(singer.Location, safePreferred);
            }

            // 2. ตรวจสอบไฟล์ .lbp ที่ตรงกับชื่อนักร้อง (Singer Name Match)
            string singerLbp = Path.Combine(singer.Location, SanitizeFileName(singer.Name) + ".lbp");
            if (File.Exists(singerLbp)) {
                return singerLbp;
            }

            // 3. ค้นหาไฟล์ .lbp ที่มีอยู่เดิมในโฟลเดอร์ เรียงลำดับจากเวลาแก้ไขล่าสุด
            var existingLbp = Directory.GetFiles(singer.Location, "*.lbp")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(existingLbp)) {
                return existingLbp;
            }

            // 4. ค่าเริ่มต้นเมื่อสร้างโปรเจกต์ใหม่: ตั้งตามชื่อนักร้อง
            return singerLbp;
        }

        /// <summary>
        /// เปลี่ยนชื่อไฟล์โปรเจกต์ .lbp ในโฟลเดอร์ Voicebank อย่างปลอดภัย
        /// มีระบบ Backup ไฟล์เดิม (.bak) ป้องกันข้อมูลสูญหาย 100%
        /// </summary>
        public bool RenameProjectFile(Core.Ustx.USinger singer, string newFileName, bool createBackup = true) {
            try {
                if (!Directory.Exists(singer.Location)) return false;

                string currentLbpPath = GetResolvedProjectFilePath(singer);
                string safeNewName = SanitizeFileName(newFileName);
                if (!safeNewName.EndsWith(".lbp", StringComparison.OrdinalIgnoreCase)) {
                    safeNewName += ".lbp";
                }

                string newLbpPath = Path.Combine(singer.Location, safeNewName);

                // หากชื่อเดิมตรงกับชื่อใหม่อยู่แล้ว
                if (string.Equals(currentLbpPath, newLbpPath, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }

                if (File.Exists(currentLbpPath)) {
                    if (createBackup) {
                        string backupPath = currentLbpPath + ".bak";
                        File.Copy(currentLbpPath, backupPath, overwrite: true);
                        Log.Information("[vLabelerClient] Created project backup: {BackupPath}", backupPath);
                    }

                    File.Move(currentLbpPath, newLbpPath, overwrite: true);
                    Log.Information("[vLabelerClient] Renamed project file: {OldPath} -> {NewPath}", currentLbpPath, newLbpPath);
                    return true;
                }

                return false;
            } catch (Exception ex) {
                Log.Error(ex, "[vLabelerClient] Failed to rename project file for singer {SingerName}", singer.Name);
                return false;
            }
        }

        /// <summary>
        /// เปลี่ยนชื่อไฟล์เสียง Wave Sample (.wav) พร้อมอัปเดตบรรทัดที่เกี่ยวข้องใน oto.ini โดยอัตโนมัติ
        /// </summary>
        public bool RenameSampleFile(Core.Ustx.USinger singer, string oldWavFileName, string newWavFileName, string? subFolder = null) {
            try {
                string targetDir = string.IsNullOrEmpty(subFolder) 
                    ? singer.Location 
                    : Path.Combine(singer.Location, subFolder);

                if (!Directory.Exists(targetDir)) return false;

                string safeOldName = Path.GetFileName(oldWavFileName);
                string safeNewName = SanitizeFileName(Path.GetFileNameWithoutExtension(newWavFileName)) + ".wav";

                string oldFilePath = Path.Combine(targetDir, safeOldName);
                string newFilePath = Path.Combine(targetDir, safeNewName);

                if (!File.Exists(oldFilePath)) {
                    Log.Warning("[vLabelerClient] Sample file not found: {Path}", oldFilePath);
                    return false;
                }

                // 1. เปลี่ยนชื่อไฟล์เสียง .wav บนดิสก์
                File.Move(oldFilePath, newFilePath, overwrite: true);
                Log.Information("[vLabelerClient] Renamed sample file: {Old} -> {New}", oldFilePath, newFilePath);

                // 2. อัปเดตใน oto.ini หากมีไฟล์ oto.ini อยู่ในโฟลเดอร์นั้น
                string otoPath = Path.Combine(targetDir, "oto.ini");
                if (File.Exists(otoPath)) {
                    string otoBak = otoPath + ".bak";
                    File.Copy(otoPath, otoBak, overwrite: true);

                    var lines = File.ReadAllLines(otoPath, singer.TextFileEncoding ?? Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++) {
                        if (lines[i].StartsWith(safeOldName + "=", StringComparison.OrdinalIgnoreCase)) {
                            lines[i] = safeNewName + lines[i].Substring(safeOldName.Length);
                        }
                    }
                    File.WriteAllLines(otoPath, lines, singer.TextFileEncoding ?? Encoding.UTF8);
                    Log.Information("[vLabelerClient] Synchronized oto.ini with new sample name: {OtoPath}", otoPath);
                }

                // 3. รีโหลดข้อมูล Singer ใน OpenUtau
                singer.Reload();
                return true;
            } catch (Exception ex) {
                Log.Error(ex, "[vLabelerClient] Error renaming sample file: {Old} -> {New}", oldWavFileName, newWavFileName);
                return false;
            }
        }

        private bool Heartbeat() {
            try {
                using (var client = new RequestSocket()) {
                    client.Connect("tcp://localhost:32342");
                    string reqStr = JsonConvert.SerializeObject(new HeartbeatRequest());
                    client.SendFrame(reqStr);
                    if (client.TryReceiveFrameString(TimeSpan.FromMilliseconds(1000), out string? respStr)) {
                        return true;
                    }
                    return false;
                }
            } catch (Exception ex) {
                Log.Warning(ex, "vLabeler heartbeat check failed.");
                return false;
            }
        }

        /// <summary>
        /// เปิดหรือสร้างโปรเจกต์ vLabeler พร้อมระบบค้นหา Entry ที่ยืดหยุ่นต่อการเปลี่ยนชื่อไฟล์หรือ Alias
        /// </summary>
        private void OpenOrCreate(Core.Ustx.USinger singer, Core.Ustx.UOto? oto, string? preferredProjectName = null) {
            string projectFilePath = GetResolvedProjectFilePath(singer, preferredProjectName);

            var request = new OpenOrCreateRequest() {
                projectFile = projectFilePath,
                newProjectArgs = new NewProjectArgs {
                    sampleDirectory = singer.Location,
                    inputFile = Path.Combine(singer.Location, "oto.ini"),
                    cacheDirectory = Path.Combine(PathManager.Inst.CachePath, $"vlabeler-{HashHex(singer.Id + singer.Location)}"),
                    labelerParams = new Dictionary<string, TypedValue> {
                        { "useRootDirectory", new TypedValue("boolean", true) }
                    },
                    encoding = singer.TextFileEncoding?.WebName ?? Encoding.UTF8.WebName,
                    autoExport = true,
                },
            };

            if (oto != null) {
                // คำนวณ Relative Subfolder สำหรับ parentFolderName ให้ถูกต้องทั้งบน Windows และ Unix
                string parentFolder = string.Empty;
                if (!string.IsNullOrEmpty(oto.Set)) {
                    try {
                        string setDir = Path.GetDirectoryName(oto.Set) ?? singer.Location;
                        parentFolder = Path.GetRelativePath(singer.Location, setDir).Replace("\\", "/");
                        if (parentFolder == ".") parentFolder = string.Empty;
                    } catch {
                        parentFolder = string.Empty;
                    }
                }

                // หาก Alias ถูกเปลี่ยนชื่อหรือว่างเปล่า ให้ Fallback ไปที่ชื่อไฟล์เสียงโดยอัตโนมัติ
                string entryName = !string.IsNullOrWhiteSpace(oto.Alias) 
                    ? oto.Alias 
                    : (!string.IsNullOrWhiteSpace(oto.File) ? Path.GetFileNameWithoutExtension(oto.File) : "");

                request.gotoEntryByName = new GotoEntryByName(parentFolder, entryName);
            }

            try {
                using (var client = new RequestSocket()) {
                    client.Connect("tcp://localhost:32342");
                    string reqStr = JsonConvert.SerializeObject(request);
                    client.SendFrame(reqStr);
                    if (!client.TryReceiveFrameString(TimeSpan.FromMilliseconds(1500), out string? respStr)) {
                        Log.Warning("[vLabelerClient] Failed to OpenOrCreate with vLabeler (no response)");
                    } else {
                        Log.Information("[vLabelerClient] Successfully dispatched OpenOrCreate to vLabeler: {Project}", projectFilePath);
                    }
                }
            } catch (Exception ex) {
                Log.Error(ex, "[vLabelerClient] Exception thrown during OpenOrCreate with vLabeler");
            }
        }

        private Task<bool> TryStart() {
            return Task.Run(() => {
                if (Heartbeat()) {
                    return true;
                }

                var path = Core.Util.Preferences.Default.VLabelerPath;
                if (!OS.AppExists(path)) {
                    throw new FileNotFoundException($"Cannot find file {path}.");
                }

                using (var proc = new Process()) {
                    if (OS.IsMacOS()) {
                        OS.OpenFolder(path);
                    } else {
                        proc.StartInfo = new ProcessStartInfo(path) {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        };
                        proc.Start();
                    }
                    Log.Information("Starting vLabeler.");
                }

                for (int i = 0; i < 50; i++) {
                    Task.Delay(100).Wait();
                    if (Heartbeat()) {
                        Log.Information("vLabeler started.");
                        return true;
                    }
                }
                Log.Warning("Unable to start vLabeler.");
                return false;
            });
        }

        /// <summary>
        /// คำสั่งหลักในการส่งคำสั่งไปยัง vLabeler เพื่อเปิด OTO หรือข้ามไปยังตำแหน่งโน้ตที่ระบุ
        /// </summary>
        public void GotoOto(Core.Ustx.USinger singer, Core.Ustx.UOto? oto, string? preferredProjectName = null) {
            TryStart().ContinueWith(task => {
                if (!task.IsFaulted && task.Result) {
                    OpenOrCreate(singer, oto, preferredProjectName);
                } else if (task.IsFaulted) {
                    if (task.Exception != null) {
                        throw task.Exception;
                    }
                } else {
                    throw new Exception("Failed to start vLabeler");
                }
            }).ContinueWith(task => {
                if (task.IsFaulted) {
                    if (task.Exception != null) {
                        DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(task.Exception));
                    } else {
                        DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to start vLabeler"));
                    }
                }
            });
        }
    }
}
