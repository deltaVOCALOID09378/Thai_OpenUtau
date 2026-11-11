// ============================================================================
// 1. วัน/เดือน/ปี: 10 พฤศจิกายน พ.ศ. 2569 / 10 November 2026
// 2. ชื่อไฟล์โค้ด: DeltaSingerProfileResolver.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.2
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/DeltaSingers/DeltaSingerProfileResolver.cs
// 5. หน้าที่: ตัวจัดการค้นหา เชื่อมโยง และติดตั้งรูปโปรไฟล์ประจำตัว (Icon / Avatar จาก E:\A Profile for Singer Picture)
//            และภาพแบบเต็มตัว (Full Body / Portrait จาก E:\A Full Body Picture)
//            ของ DELTA SYNTH ไปยังคลังเสียงทั้งหมดโดยอัตโนมัติ พร้อมระบบแยกแยะไฟล์ไม่ให้สลับกัน
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine | Original by DELTA SYNTH
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace OpenUtau.Core.SingerHub {
    public static class DeltaSingerProfileResolver {
        public const string DefaultFullBodyDir = @"E:\A Full Body Picture";
        public const string DefaultProfilesDir = @"E:\A Profile for Singer Picture";
        public const string DefaultVoicebanksDir = @"E:\All Voicebank in DELTA SYNTH";

        private static readonly object cacheLock = new object();
        private static Dictionary<string, string>? cachedProfileImages;
        private static Dictionary<string, string>? cachedFullBodyImages;
        private static DateTime lastProfileScan = DateTime.MinValue;
        private static DateTime lastFullBodyScan = DateTime.MinValue;

        private static readonly Dictionary<string, string[]> SingerAliases = new(StringComparer.OrdinalIgnoreCase) {
            { "Ahctan", new[] { "Achtan" } },
            { "Achtan", new[] { "Ahctan" } },
            { "Chansamorn", new[] { "Charnsamorn", "Shansamorn" } },
            { "Charnsamorn", new[] { "Chansamorn", "Shansamorn" } },
            { "Shansamorn", new[] { "Chansamorn", "Charnsamorn" } },
            { "Kikakowa Usagi", new[] { "Kikokawa Usagi" } },
            { "Kikokawa Usagi", new[] { "Kikakowa Usagi" } },
            { "Tenshi Saburo", new[] { "Tenshio Saburo", "Tenshino Koe" } },
            { "Tenshio Saburo", new[] { "Tenshi Saburo" } },
            { "Quint", new[] { "Quint New" } },
            { "Quint New", new[] { "Quint" } },
            { "Savanna", new[] { "Code Zero", "Mrs. CodeZero", "CodeZero" } },
            { "Code Zero", new[] { "Savanna", "Mrs. CodeZero", "CodeZero" } },
            { "Mrs. CodeZero", new[] { "Savanna", "Code Zero", "CodeZero" } },
            { "Kanomwan", new[] { "Andromeda" } },
            { "Andromeda", new[] { "Kanomwan" } },
            { "Kira", new[] { "New Day" } },
            { "New Day", new[] { "Kira" } },
            { "Tackpee", new[] { "Tagpy" } },
            { "Tagpy", new[] { "Tackpee" } },
            { "Fuwari Bento", new[] { "Fumari Bento" } },
            { "Fumari Bento", new[] { "Fuwari Bento" } },
            { "Koizumi Satoru", new[] { "Koizumu Satoru" } },
            { "Koizumu Satoru", new[] { "Koizumi Satoru" } },
            { "ARZBTV", new[] { "ARZB TV" } },
            { "ARZB TV", new[] { "ARZBTV" } },
        };

        /// <summary>
        /// ดึงรายการไฟล์ภาพโปรไฟล์ (Icon / Avatar) ทั้งหมดจากไดเรกทอรีต้นฉบับพร้อมระบบ Cache
        /// </summary>
        public static Dictionary<string, string> GetAvailableProfileImages(string? sourceDir = null) {
            string dir = string.IsNullOrEmpty(sourceDir) ? DefaultProfilesDir : sourceDir;
            lock (cacheLock) {
                if (cachedProfileImages != null && (DateTime.Now - lastProfileScan).TotalMinutes < 5) {
                    return cachedProfileImages;
                }

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(dir)) {
                    try {
                        foreach (string file in Directory.EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly)) {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file).Trim();
                            map[fileNameWithoutExt] = file;
                            string normalized = NormalizeKey(fileNameWithoutExt);
                            if (!string.IsNullOrEmpty(normalized) && !map.ContainsKey(normalized)) {
                                map[normalized] = file;
                            }
                        }
                    } catch (Exception ex) {
                        Log.Warning(ex, "Failed to scan profile pictures directory: {Dir}", dir);
                    }
                }
                cachedProfileImages = map;
                lastProfileScan = DateTime.Now;
                return cachedProfileImages;
            }
        }

        /// <summary>
        /// ดึงรายการไฟล์ภาพเต็มตัว (Full Body / Portrait) ทั้งหมดจากไดเรกทอรีต้นฉบับพร้อมระบบ Cache
        /// </summary>
        public static Dictionary<string, string> GetAvailableFullBodyImages(string? sourceDir = null) {
            string dir = string.IsNullOrEmpty(sourceDir) ? DefaultFullBodyDir : sourceDir;
            lock (cacheLock) {
                if (cachedFullBodyImages != null && (DateTime.Now - lastFullBodyScan).TotalMinutes < 5) {
                    return cachedFullBodyImages;
                }

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(dir)) {
                    try {
                        foreach (string file in Directory.EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly)) {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file).Trim();
                            map[fileNameWithoutExt] = file;
                            string normalized = NormalizeKey(fileNameWithoutExt);
                            if (!string.IsNullOrEmpty(normalized) && !map.ContainsKey(normalized)) {
                                map[normalized] = file;
                            }
                        }
                    } catch (Exception ex) {
                        Log.Warning(ex, "Failed to scan full-body pictures directory: {Dir}", dir);
                    }
                }
                cachedFullBodyImages = map;
                lastFullBodyScan = DateTime.Now;
                return cachedFullBodyImages;
            }
        }

        public static string NormalizeKey(string text) {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9\u0E00-\u0E7F]", "");
        }

        private static string? SearchImageMap(Dictionary<string, string> images, string? singerName, string? location) {
            if (images.Count == 0) return null;

            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(singerName)) candidates.Add(singerName);
            if (!string.IsNullOrWhiteSpace(location)) {
                string folderName = Path.GetFileName(location.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(folderName)) candidates.Add(folderName);
            }

            foreach (string candidate in candidates) {
                string trimmed = candidate.Trim();
                if (images.TryGetValue(trimmed, out var exactMatch) && File.Exists(exactMatch)) {
                    return exactMatch;
                }

                string normCandidate = NormalizeKey(trimmed);
                if (images.TryGetValue(normCandidate, out var normMatch) && File.Exists(normMatch)) {
                    return normMatch;
                }

                // ค้นหาผ่านชื่อพ้อง (SingerAliases)
                foreach (var aliasKvp in SingerAliases) {
                    string aNorm = NormalizeKey(aliasKvp.Key);
                    if (normCandidate.Contains(aNorm)) {
                        if (images.TryGetValue(aliasKvp.Key, out var aliasFile) && File.Exists(aliasFile)) {
                            return aliasFile;
                        }
                        foreach (var alt in aliasKvp.Value) {
                            if (images.TryGetValue(alt, out var altFile) && File.Exists(altFile)) {
                                return altFile;
                            }
                        }
                    }
                }

                // ค้นหาเปรียบเทียบกับนักร้องทางการใน DeltaSynthSingerRegistry
                foreach (var profile in DeltaSynthSingerRegistry.Profiles) {
                    string pNormName = NormalizeKey(profile.Name);
                    string pNormId = NormalizeKey(profile.Id);
                    string pNormTh = NormalizeKey(profile.NameTh);

                    bool matches = (!string.IsNullOrEmpty(pNormName) && normCandidate.Contains(pNormName)) ||
                                   (!string.IsNullOrEmpty(pNormId) && normCandidate.Contains(pNormId)) ||
                                   (!string.IsNullOrEmpty(pNormTh) && normCandidate.Contains(pNormTh));

                    if (matches) {
                        if (images.TryGetValue(profile.Name, out var pMatch) && File.Exists(pMatch)) {
                            return pMatch;
                        }
                        if (images.TryGetValue(pNormName, out var pNormFile) && File.Exists(pNormFile)) {
                            return pNormFile;
                        }
                    }
                }

                // ตรวจหาแบบ Substring match กับชื่อไฟล์ภาพที่มีอยู่
                foreach (var kvp in images) {
                    if (kvp.Key.Length >= 3 && normCandidate.Contains(kvp.Key)) {
                        if (File.Exists(kvp.Value)) return kvp.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// ค้นหาไฟล์ภาพโปรไฟล์ (Icon / Avatar สำหรับหัวแทร็ก) จากไดเรกทอรี E:\A Profile for Singer Picture
        /// </summary>
        public static string? FindMatchingProfileImage(string? singerName, string? location = null, string? sourceDir = null) {
            var images = GetAvailableProfileImages(sourceDir);
            return SearchImageMap(images, singerName, location);
        }

        /// <summary>
        /// ค้นหาไฟล์ภาพเต็มตัว (Full Body / Portrait สำหรับเปียโนโรล) จากไดเรกทอรี E:\A Full Body Picture
        /// </summary>
        public static string? FindMatchingFullBodyImage(string? singerName, string? location = null, string? sourceDir = null) {
            var images = GetAvailableFullBodyImages(sourceDir);
            return SearchImageMap(images, singerName, location);
        }

        /// <summary>
        /// ค้นหาและคืนค่า Path ของ Avatar (รูปโปรไฟล์สำหรับหัวแทร็ก)
        /// ป้องกันไม่ให้นำรูป Full Body / Portrait มาใช้เป็น Avatar
        /// </summary>
        public static string? ResolveAvatar(string? singerName, string? location, string? currentAvatar) {
            if (!string.IsNullOrEmpty(currentAvatar) && File.Exists(currentAvatar)) {
                string fileName = Path.GetFileName(currentAvatar).ToLowerInvariant();
                if (!fileName.Equals("portrait.png") && !fileName.Equals("full.png") && !fileName.Equals("character.png")) {
                    return currentAvatar;
                }
            }

            if (!string.IsNullOrEmpty(location) && Directory.Exists(location)) {
                string[] localNames = new[] {
                    "icon.png", "Icon.png", "icon.bmp", "Icon.bmp",
                    "avatar.png", "Avatar.png", "image.png", "Image.png",
                    Path.Combine("Picture", "Icon.png"), Path.Combine("Picture", "icon.png"),
                    Path.Combine("Picture", "Avatar.png"), Path.Combine("Picture", "avatar.png")
                };
                foreach (var name in localNames) {
                    string localPath = Path.Combine(location, name);
                    if (File.Exists(localPath)) return localPath;
                }
            }

            return FindMatchingProfileImage(singerName, location);
        }

        /// <summary>
        /// ค้นหาและคืนค่า Path ของ Portrait (ภาพแบบเต็มตัวสำหรับเปียโนโรล)
        /// ตรวจสอบไม่ให้นำรูป Profile / Icon มาใช้เป็น Portrait เด็ดขาด
        /// </summary>
        public static string? ResolvePortrait(string? singerName, string? location, string? currentPortrait) {
            if (!string.IsNullOrEmpty(currentPortrait) && File.Exists(currentPortrait)) {
                string fileName = Path.GetFileName(currentPortrait).ToLowerInvariant();
                if (!fileName.Equals("icon.png") && !fileName.Equals("avatar.png")) {
                    if (!string.IsNullOrEmpty(location) && Directory.Exists(location)) {
                        string localIcon = Path.Combine(location, "icon.png");
                        if (File.Exists(localIcon) && new FileInfo(localIcon).Length == new FileInfo(currentPortrait).Length) {
                            // portrait.png มีขนาดเท่ากับ icon.png แสดงว่าเคยถูกก็อปปี้ผิดพลาด
                            string localFull = Path.Combine(location, "Full.png");
                            if (File.Exists(localFull) && new FileInfo(localFull).Length != new FileInfo(localIcon).Length) {
                                return localFull;
                            }
                            string picFull = Path.Combine(location, "Picture", "Full.png");
                            if (File.Exists(picFull) && new FileInfo(picFull).Length != new FileInfo(localIcon).Length) {
                                return picFull;
                            }
                        } else {
                            return currentPortrait;
                        }
                    } else {
                        return currentPortrait;
                    }
                }
            }

            if (!string.IsNullOrEmpty(location) && Directory.Exists(location)) {
                string[] preferredFullNames = new[] {
                    "Full.png", "full.png",
                    Path.Combine("Picture", "Full.png"), Path.Combine("Picture", "full.png"),
                    Path.Combine("Picture", "portrait.png")
                };
                foreach (var name in preferredFullNames) {
                    string localPath = Path.Combine(location, name);
                    if (File.Exists(localPath)) {
                        string iconPath = Path.Combine(location, "icon.png");
                        if (!File.Exists(iconPath) || new FileInfo(iconPath).Length != new FileInfo(localPath).Length) {
                            return localPath;
                        }
                    }
                }

                string localPortrait = Path.Combine(location, "portrait.png");
                if (File.Exists(localPortrait)) {
                    string iconPath = Path.Combine(location, "icon.png");
                    if (!File.Exists(iconPath) || new FileInfo(iconPath).Length != new FileInfo(localPortrait).Length) {
                        return localPortrait;
                    }
                }
            }

            return FindMatchingFullBodyImage(singerName, location);
        }

        /// <summary>
        /// ติดตั้งและจัดระเบียบรูปภาพนักร้องทั้งหมดลงในโฟลเดอร์คลังเสียงจริงบนดิสก์
        /// - ภาพแบบเต็มตัว (Full Body จาก E:\A Full Body Picture) -> portrait.png และ Full.png
        /// - รูปโปรไฟล์ (Profile จาก E:\A Profile for Singer Picture) -> icon.png และ Icon.png
        /// </summary>
        public static int DeployPicturesToVoicebanks(string? voicebanksRoot = null, string? fullBodyDir = null, string? profileDir = null) {
            string root = string.IsNullOrEmpty(voicebanksRoot) ? DefaultVoicebanksDir : voicebanksRoot;
            string fullSrc = string.IsNullOrEmpty(fullBodyDir) ? DefaultFullBodyDir : fullBodyDir;
            string profSrc = string.IsNullOrEmpty(profileDir) ? DefaultProfilesDir : profileDir;

            if (!Directory.Exists(root)) {
                Log.Warning("DeployPicturesToVoicebanks: Voicebanks root not found: {Root}", root);
                return 0;
            }

            int count = 0;
            try {
                var voicebankDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var charFile in Directory.EnumerateFiles(root, "character.txt", SearchOption.AllDirectories)) {
                    string? dir = Path.GetDirectoryName(charFile);
                    if (!string.IsNullOrEmpty(dir)) voicebankDirs.Add(dir);
                }
                foreach (var yamlFile in Directory.EnumerateFiles(root, "character.yaml", SearchOption.AllDirectories)) {
                    string? dir = Path.GetDirectoryName(yamlFile);
                    if (!string.IsNullOrEmpty(dir)) voicebankDirs.Add(dir);
                }

                foreach (string vbDir in voicebankDirs) {
                    string charTxt = Path.Combine(vbDir, "character.txt");
                    string singerName = string.Empty;
                    if (File.Exists(charTxt)) {
                        try {
                            foreach (string line in File.ReadLines(charTxt)) {
                                var parts = line.Split('=', 2);
                                if (parts.Length == 2 && parts[0].Trim().Equals("name", StringComparison.OrdinalIgnoreCase)) {
                                    singerName = parts[1].Trim();
                                    break;
                                }
                            }
                        } catch { }
                    }

                    string? matchedFull = FindMatchingFullBodyImage(singerName, vbDir, fullSrc);
                    string? matchedProf = FindMatchingProfileImage(singerName, vbDir, profSrc);

                    bool modified = false;

                    // 1. ติดตั้งรูปโปรไฟล์ (Icon / Avatar) สำหรับหัวแทร็กเพลง
                    if (!string.IsNullOrEmpty(matchedProf) && File.Exists(matchedProf)) {
                        try {
                            string targetIcon = Path.Combine(vbDir, "icon.png");
                            string targetIconCapital = Path.Combine(vbDir, "Icon.png");
                            File.Copy(matchedProf, targetIcon, true);
                            File.Copy(matchedProf, targetIconCapital, true);

                            string picDir = Path.Combine(vbDir, "Picture");
                            if (Directory.Exists(picDir)) {
                                File.Copy(matchedProf, Path.Combine(picDir, "Icon.png"), true);
                                File.Copy(matchedProf, Path.Combine(picDir, "icon.png"), true);
                            }
                            modified = true;
                        } catch (Exception ex) {
                            Log.Error(ex, "Failed to copy profile picture to {VbDir}", vbDir);
                        }
                    }

                    // 2. ติดตั้งภาพแบบเต็มตัว (Full Body / Portrait) สำหรับเปียโนโรล
                    if (!string.IsNullOrEmpty(matchedFull) && File.Exists(matchedFull)) {
                        try {
                            string targetPortrait = Path.Combine(vbDir, "portrait.png");
                            string targetFull = Path.Combine(vbDir, "Full.png");
                            File.Copy(matchedFull, targetPortrait, true);
                            File.Copy(matchedFull, targetFull, true);

                            string picDir = Path.Combine(vbDir, "Picture");
                            if (Directory.Exists(picDir)) {
                                File.Copy(matchedFull, Path.Combine(picDir, "Full.png"), true);
                                File.Copy(matchedFull, Path.Combine(picDir, "portrait.png"), true);
                            }
                            modified = true;
                        } catch (Exception ex) {
                            Log.Error(ex, "Failed to copy full-body picture to {VbDir}", vbDir);
                        }
                    }

                    // 3. อัปเดต character.txt ให้ชี้ image=icon.png
                    if (File.Exists(charTxt)) {
                        try {
                            var lines = File.ReadAllLines(charTxt).ToList();
                            bool hasImage = false;
                            for (int i = 0; i < lines.Count; i++) {
                                if (lines[i].TrimStart().StartsWith("image=", StringComparison.OrdinalIgnoreCase)) {
                                    lines[i] = "image=icon.png";
                                    hasImage = true;
                                    break;
                                }
                            }
                            if (!hasImage) {
                                lines.Add("image=icon.png");
                            }
                            File.WriteAllLines(charTxt, lines);
                        } catch { }
                    }

                    // 4. อัปเดต character.yaml ให้ชี้ image: icon.png และ portrait: portrait.png
                    string charYaml = Path.Combine(vbDir, "character.yaml");
                    if (File.Exists(charYaml)) {
                        try {
                            var yamlLines = File.ReadAllLines(charYaml).ToList();
                            bool hasImgYaml = false;
                            bool hasPortYaml = false;
                            for (int i = 0; i < yamlLines.Count; i++) {
                                if (yamlLines[i].TrimStart().StartsWith("image:", StringComparison.OrdinalIgnoreCase)) {
                                    yamlLines[i] = "image: icon.png";
                                    hasImgYaml = true;
                                } else if (yamlLines[i].TrimStart().StartsWith("portrait:", StringComparison.OrdinalIgnoreCase)) {
                                    yamlLines[i] = "portrait: portrait.png";
                                    hasPortYaml = true;
                                }
                            }
                            if (!hasImgYaml) yamlLines.Add("image: icon.png");
                            if (!hasPortYaml) {
                                yamlLines.Add("portrait: portrait.png");
                                yamlLines.Add("portrait_opacity: 0.75");
                            }
                            File.WriteAllLines(charYaml, yamlLines);
                        } catch { }
                    }

                    if (modified) {
                        count++;
                    }
                }
            } catch (Exception ex) {
                Log.Error(ex, "Failed during DeployPicturesToVoicebanks in {Root}", root);
            }

            return count;
        }

        /// <summary>
        /// เมธอดคงเดิมเพื่อความเข้ากันได้ย้อนหลัง (Backward Compatibility)
        /// </summary>
        public static int DeployProfilePicturesToVoicebanks(string? voicebanksRoot = null, string? sourceDir = null) {
            return DeployPicturesToVoicebanks(voicebanksRoot, DefaultFullBodyDir, sourceDir ?? DefaultProfilesDir);
        }
    }
}
