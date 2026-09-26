// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)

#pragma warning disable CS0618, CS0649, CS8632, CS0108 // v1.0 - 06/07/2026 - ปิดการแจ้งเตือน Warning บางส่วนของคอมไพเลอร์เพื่อลดข้อความกวนใจ
#nullable enable // v1.0 - 06/07/2026 - เปิดใช้งานฟีเจอร์ Nullable Reference Types เพื่อความปลอดภัยของหน่วยความจำ
#pragma warning disable CS8632 // v1.0 - 06/07/2026 - ปิดการแจ้งเตือนเรื่องการใช้ Nullable ในบางเวอร์ชันของ C#

using System; // v1.0 - 06/07/2026 - นำเข้าไลบรารีพื้นฐานของระบบ
using System.Collections.Generic; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการใช้งาน List
using System.IO; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการจัดการไฟล์และโฟลเดอร์
using System.Linq; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการคิวรีข้อมูลแบบ LINQ
using System.Net.Http; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการเชื่อมต่อ HTTP
using System.Security.Cryptography; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการเข้ารหัส (เช่น SHA256)
using System.Text; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการจัดการข้อความ
using System.Text.RegularExpressions; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับใช้งาน Regular Expressions
using System.Threading.Tasks; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการทำงานแบบ Asynchronous
using Newtonsoft.Json; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับจัดการข้อมูลรูปแบบ JSON
using OpenUtau.Core.Util; // v1.0 - 06/07/2026 - นำเข้าเนมสเปซอรรถประโยชน์ของ OpenUtau
using Serilog; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับการบันทึก Log
using SharpCompress.Archives; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับจัดการไฟล์บีบอัด
using SharpCompress.Readers; // v1.0 - 06/07/2026 - นำเข้าไลบรารีสำหรับอ่านไฟล์บีบอัด

namespace OpenUtau.Core.SingerHub { // v1.0 - 06/07/2026 - กำหนดเนมสเปซหลักสำหรับระบบ SingerHub

    public class SingerHubEntry { // v1.0 - 06/07/2026 - สร้างคลาสเพื่อเก็บข้อมูลนักร้องแต่ละตัวที่ดึงมาจาก JSON
        [JsonProperty("name")] // v1.0 - 06/07/2026 - แมปคุณสมบัตินี้กับคีย์ "name" ใน JSON
        public string Name { get; set; } = string.Empty; // v1.0 - 06/07/2026 - ชื่อของนักร้อง

        [JsonProperty("code")] // v1.0 - 06/07/2026 - แมปคีย์ "code"
        public string Code { get; set; } = string.Empty; // v1.0 - 06/07/2026 - รหัสของนักร้อง

        [JsonProperty("version")] // v1.0 - 06/07/2026 - แมปคีย์ "version"
        public string Version { get; set; } = string.Empty; // v1.0 - 06/07/2026 - เวอร์ชันของเสียง

        [JsonProperty("downloadUrl")] // v1.0 - 06/07/2026 - แมปคีย์ "downloadUrl"
        public string DownloadUrl { get; set; } = string.Empty; // v1.0 - 06/07/2026 - ลิงก์สำหรับดาวน์โหลดไฟล์เสียง

        [JsonProperty("company")] // v1.0 - 06/07/2026 - แมปคีย์ "company"
        public string Company { get; set; } = "DELTA SYNTH"; // v1.0 - 06/07/2026 - กำหนดค่าเริ่มต้นเป็น DELTA SYNTH แทนที่ของเดิม

        [JsonProperty("type")] // v1.0 - 06/07/2026 - แมปคีย์ "type"
        public string Type { get; set; } = "diffsinger"; // v1.0 - 06/07/2026 - กำหนดชนิดเสียงเริ่มต้นเป็น diffsinger

        [JsonProperty("host")] // v1.0 - 06/07/2026 - แมปคีย์ "host"
        public string Host { get; set; } = "delta"; // v1.0 - 06/07/2026 - กำหนดชื่อโฮสต์เริ่มต้นเป็น delta สำหรับแยกระบบ

        [JsonProperty("icon")] // v1.0 - 06/07/2026 - แมปคีย์ "icon"
        public string Icon { get; set; } = string.Empty; // v1.0 - 06/07/2026 - URL ของรูปไอคอน

        [JsonProperty("tos")] // v1.0 - 06/07/2026 - แมปคีย์ "tos"
        public string Tos { get; set; } = "https://deltasynth.com/termsofuse"; // v1.0 - 06/07/2026 - แก้ไขลิงก์เงื่อนไขการใช้งานให้เป็นของ DELTA SYNTH
    }

    public class SingerHubRegistry { // v1.0 - 06/07/2026 - คลาสสำหรับรับข้อมูลรายการนักร้องทั้งหมดจาก JSON
        [JsonProperty("singers")] // v1.0 - 06/07/2026 - แมปคีย์ "singers"
        public List<SingerHubEntry> Singers { get; set; } = new List<SingerHubEntry>(); // v1.0 - 06/07/2026 - เก็บรายชื่อนักร้องในรูปแบบ List
    }

    public class InstalledHubSinger { // v1.0 - 06/07/2026 - คลาสเพื่อแสดงข้อมูลนักร้องที่ถูกติดตั้งอยู่ในเครื่องแล้ว
        public string FolderPath { get; set; } = string.Empty; // v1.0 - 06/07/2026 - ที่อยู่โฟลเดอร์ของนักร้อง
        public string DisplayName { get; set; } = string.Empty; // v1.0 - 06/07/2026 - ชื่อที่ใช้แสดงผล
        public string Version { get; set; } = string.Empty; // v1.0 - 06/07/2026 - เวอร์ชันของเสียง
        public bool IsDelta { get; set; } // v1.0 - 06/07/2026 - เปลี่ยนชื่อจาก IsLunai เป็น IsDelta เพื่อตรวจสอบว่าเป็นเสียงของ DELTA SYNTH หรือไม่
    }

    public class SingerHubClient { // v1.0 - 06/07/2026 - คลาสหลักสำหรับเชื่อมต่อและจัดการดาวน์โหลดนักร้อง
        public const string DefaultRegistryUrl = "https://deltasynth.com/api/singers.json"; // v1.0 - 06/07/2026 - กำหนด URL หลักให้ชี้ไปยังเซิร์ฟเวอร์ของ DELTA SYNTH

        static readonly Regex NameVersionRegex = new Regex(@"^(.+?)\s+v(\d+)$", RegexOptions.Compiled); // v1.0 - 06/07/2026 - ตรวจสอบรูปแบบชื่อและเวอร์ชันแบบปกติ
        static readonly Regex UfrNameVersionRegex = new Regex( // v1.0 - 06/07/2026 - ตรวจสอบรูปแบบชื่อของ UFR
            @"^(.+?)\s+-\s+UFR\s+V[^\s]*\s+V([\d.]+)\s*$", // v1.0 - 06/07/2026 - แพทเทิร์น regex
            RegexOptions.Compiled | RegexOptions.IgnoreCase); // v1.0 - 06/07/2026 - ตั้งค่าให้ไม่สนใจตัวพิมพ์เล็กใหญ่
        static readonly Regex TrailingVersionRegex = new Regex( // v1.0 - 06/07/2026 - ตรวจสอบเวอร์ชันที่อยู่ท้ายสุด
            @"V([\d.]+)\s*$", // v1.0 - 06/07/2026 - แพทเทิร์น regex
            RegexOptions.Compiled | RegexOptions.IgnoreCase); // v1.0 - 06/07/2026 - ตั้งค่าออปชัน Regex
        static readonly Regex BrapaNameVersionRegex = new Regex( // v1.0 - 06/07/2026 - ตรวจสอบรูปแบบชื่อของ BRAPA
            @"^(.+?)\s+(?:DS\s+)?v(\d+)\s*$", // v1.0 - 06/07/2026 - แพทเทิร์น regex
            RegexOptions.Compiled | RegexOptions.IgnoreCase); // v1.0 - 06/07/2026 - ตั้งค่าออปชัน Regex

        static string RegistryCachePath(string url) { // v1.0 - 06/07/2026 - ฟังก์ชันสร้างชื่อไฟล์แคชจาก URL
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).Substring(0, 16); // v1.0 - 06/07/2026 - แฮช URL เป็นอักขระ 16 ตัวเพื่อใช้เป็นชื่อไฟล์
            return Path.Combine(PathManager.Inst.CachePath, $"singerhub-registry-{hash}.json"); // v1.0 - 06/07/2026 - ส่งคืนพาร์ทของไฟล์แคช
        }

        static List<SingerHubEntry> ParseRegistryJson(string json) { // v1.0 - 06/07/2026 - ฟังก์ชันแปลงข้อมูลข้อความ JSON เป็นออบเจกต์
            var registry = JsonConvert.DeserializeObject<SingerHubRegistry>(json); // v1.0 - 06/07/2026 - ทำการแปลงด้วย Newtonsoft
            return registry?.Singers ?? new List<SingerHubEntry>(); // v1.0 - 06/07/2026 - คืนค่ารายการนักร้อง (หากพังจะคืนค่า List ว่าง)
        }

        static async Task<List<SingerHubEntry>> TryLoadCachedRegistryAsync(string url) { // v1.0 - 06/07/2026 - ฟังก์ชันพยายามโหลดข้อมูลจากไฟล์แคชแทนหากอินเทอร์เน็ตมีปัญหา
            var cachePath = RegistryCachePath(url); // v1.0 - 06/07/2026 - ดึงพาร์ทแคช
            if (!File.Exists(cachePath)) { // v1.0 - 06/07/2026 - ถ้าไม่มีไฟล์แคชอยู่
                return new List<SingerHubEntry>(); // v1.0 - 06/07/2026 - คืนค่า List ว่าง
            }
            Log.Warning("Singer Hub registry fetch failed for {Url}; using cached registry at {Path}", url, cachePath); // v1.0 - 06/07/2026 - แจ้งเตือนลง Log ว่าต้องใช้แคช
            return await FetchRegistryFromFileAsync(cachePath).ConfigureAwait(false); // v1.0 - 06/07/2026 - เรียกฟังก์ชันโหลดไฟล์แคช
        }

        public async Task<List<SingerHubEntry>> FetchRegistryAsync(string? registryUrl = null) { // v1.0 - 06/07/2026 - ฟังก์ชันหลักสำหรับดึงข้อมูล JSON จากเซิร์ฟเวอร์
            var url = (registryUrl ?? DefaultRegistryUrl).Trim(); // v1.0 - 06/07/2026 - ใช้ URL ที่กำหนด หรือใช้ของ DELTA SYNTH หากไม่ได้ระบุ
            if (string.IsNullOrEmpty(url)) return new List<SingerHubEntry>(); // v1.0 - 06/07/2026 - หาก URL ว่าง ให้คืนค่าเปล่า
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) { // v1.0 - 06/07/2026 - หากเป็นพาร์ทไฟล์ในเครื่อง (Local)
                var path = new Uri(url).LocalPath; // v1.0 - 06/07/2026 - แกะเอาเฉพาะที่อยู่ในเครื่อง
                return await FetchRegistryFromFileAsync(path).ConfigureAwait(false); // v1.0 - 06/07/2026 - โหลดจากไฟล์โดยตรง
            }
            if (File.Exists(url)) { // v1.0 - 06/07/2026 - ถ้า URL คือไฟล์ที่หาเจอในระบบ
                return await FetchRegistryFromFileAsync(url).ConfigureAwait(false); // v1.0 - 06/07/2026 - โหลดจากไฟล์
            }
            try { // v1.0 - 06/07/2026 - พยายามเชื่อมต่อเครือข่าย
                using var client = new HttpClient(); // v1.0 - 06/07/2026 - สร้างตัวเชื่อมต่อ
                client.DefaultRequestHeaders.Add("User-Agent", "OpenUtau-DELTA"); // v1.0 - 06/07/2026 - เปลี่ยน User-Agent เป็นชื่อของ DELTA SYNTH เพื่อระบุตัวตน
                client.Timeout = TimeSpan.FromSeconds(90); // v1.0 - 06/07/2026 - ตั้งเวลารอสูงสุด 90 วินาที
                using var response = await client.GetAsync(url).ConfigureAwait(false); // v1.0 - 06/07/2026 - ยิงคำขอ
                response.EnsureSuccessStatusCode(); // v1.0 - 06/07/2026 - ตรวจสอบว่าสำเร็จ (Code 200) หรือไม่
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false); // v1.0 - 06/07/2026 - ดึงข้อความ JSON ออกมา
                Directory.CreateDirectory(PathManager.Inst.CachePath); // v1.0 - 06/07/2026 - สร้างโฟลเดอร์แคชหากยังไม่มี
                await File.WriteAllTextAsync(RegistryCachePath(url), json).ConfigureAwait(false); // v1.0 - 06/07/2026 - เขียนบันทึกลงแคช
                return ParseRegistryJson(json); // v1.0 - 06/07/2026 - นำไปแปลงเป็นออบเจกต์
            } catch (Exception e) when (e is HttpRequestException or TaskCanceledException or TimeoutException) { // v1.0 - 06/07/2026 - หากเกิดข้อผิดพลาดด้านเครือข่าย
                Log.Warning(e, "Failed to download Singer Hub registry from {Url}", url); // v1.0 - 06/07/2026 - เขียน Log ข้อผิดพลาด
                return await TryLoadCachedRegistryAsync(url).ConfigureAwait(false); // v1.0 - 06/07/2026 - ลองกลับไปดึงจากแคชดู
            }
        }

        public static Task<List<SingerHubEntry>> FetchRegistryFromFileAsync(string filePath) { // v1.0 - 06/07/2026 - ฟังก์ชันอ่าน JSON จากไฟล์ดิบ
            return Task.Run(() => { // v1.0 - 06/07/2026 - รันใน Thread รองเพื่อไม่ให้ UI ค้าง
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) // v1.0 - 06/07/2026 - ถ้าหาไฟล์ไม่เจอ
                    return new List<SingerHubEntry>(); // v1.0 - 06/07/2026 - คืนค่า List ว่าง
                var json = File.ReadAllText(filePath); // v1.0 - 06/07/2026 - อ่านข้อมูลจากไฟล์
                var registry = JsonConvert.DeserializeObject<SingerHubRegistry>(json); // v1.0 - 06/07/2026 - แปลงกลับเป็นออบเจกต์
                return registry?.Singers ?? new List<SingerHubEntry>(); // v1.0 - 06/07/2026 - คืนข้อมูลรายชื่อออกมา
            });
        }

        public static bool IsDeltaSinger(string folderPath) { // v1.0 - 06/07/2026 - เปลี่ยนชื่อเป็น IsDeltaSinger เพื่อตรวจสอบสิทธิ์ว่าเป็นของ DELTA SYNTH
            if (string.IsNullOrEmpty(folderPath)) { // v1.0 - 06/07/2026 - ถ้าไม่มีชื่อโฟลเดอร์
                return false; // v1.0 - 06/07/2026 - คืนค่าเท็จ
            }
            if (CreditsFileIndicatesDelta(Path.Combine(folderPath, "credits.txt"))) { // v1.0 - 06/07/2026 - ตรวจสอบในโฟลเดอร์ปัจจุบันว่าใช่ของ DELTA หรือไม่
                return true; // v1.0 - 06/07/2026 - คืนค่าจริง
            }
            var parent = Path.GetDirectoryName(folderPath); // v1.0 - 06/07/2026 - ดึงโฟลเดอร์หลักขึ้นไปหนึ่งระดับ
            return !string.IsNullOrEmpty(parent) // v1.0 - 06/07/2026 - ถ้าหาเจอ
                && CreditsFileIndicatesDelta(Path.Combine(parent, "credits.txt")); // v1.0 - 06/07/2026 - ตรวจสอบไฟล์ในโฟลเดอร์นั้นว่ามีคำของ DELTA หรือไม่
        }

        /// <summary>
        /// v1.1 [2026-07-06] Made And Checked By DELTA SYNTH & Gemini AI
        /// Backward-compatible alias สำหรับ IsDeltaSinger() — ใช้รองรับโค้ดเก่าที่ยังเรียก IsLunaiSinger()
        /// ใน DiffSingerSinger.cs และไฟล์อื่นๆ ที่ยังไม่ได้ rename
        /// ทำงานเหมือน IsDeltaSinger() ทุกประการ
        /// </summary>
        public static bool IsLunaiSinger(string folderPath) { // v1.1 - 06/07/2026 - Alias ของ IsDeltaSinger() เพื่อ backward compatibility
            return IsDeltaSinger(folderPath); // v1.1 - 06/07/2026 - เรียก IsDeltaSinger ซึ่งทำการตรวจสอบ credits.txt
        }

        static bool CreditsFileIndicatesDelta(string creditsPath) { // v1.0 - 06/07/2026 - ฟังก์ชันสำหรับตรวจสอบคำสงวนลิขสิทธิ์ของ DELTA SYNTH
            if (!File.Exists(creditsPath)) { // v1.0 - 06/07/2026 - ถ้าไม่มีไฟล์ credit
                return false; // v1.0 - 06/07/2026 - คืนค่าเท็จ
            }
            try { // v1.0 - 06/07/2026 - ป้องกันระบบแครช
                foreach (var line in File.ReadLines(creditsPath)) { // v1.0 - 06/07/2026 - อ่านทีละบรรทัด
                    var trimmed = line.Trim().TrimStart('\uFEFF'); // v1.0 - 06/07/2026 - ลบช่องว่างและอักขระพิเศษ
                    if (trimmed.Length == 0) { // v1.0 - 06/07/2026 - ถ้าบรรทัดว่าง
                        continue; // v1.0 - 06/07/2026 - ข้ามไป
                    }
                    return trimmed.StartsWith("DELTA SYNTH", StringComparison.OrdinalIgnoreCase); // v1.0 - 06/07/2026 - ค้นหาคำว่า "DELTA SYNTH" ในบรรทัดแรกที่พบ
                }
                return false; // v1.0 - 06/07/2026 - ถ้าวนจนจบแล้วไม่เจอ คืนค่าเท็จ
            } catch { // v1.0 - 06/07/2026 - หากมีปัญหาการอ่านไฟล์
                return false; // v1.0 - 06/07/2026 - คืนค่าเท็จ
            }
        }

        public static bool TryGetVersionFromCharacterYaml(string folderPath, out string version) { // v1.0 - 06/07/2026 - ดึงเวอร์ชันจากไฟล์ yaml
            version = string.Empty; // v1.0 - 06/07/2026 - ตั้งค่าเวอร์ชันเริ่มต้นเป็นว่างเปล่า
            if (string.IsNullOrEmpty(folderPath)) { // v1.0 - 06/07/2026 - ถ้าโฟลเดอร์เป็นค่าว่าง
                return false; // v1.0 - 06/07/2026 - ออกจากการตรวจสอบ
            }
            try { // v1.0 - 06/07/2026 - ดักจับข้อผิดพลาด
                var yamlPath = Path.Combine(folderPath, "character.yaml"); // v1.0 - 06/07/2026 - สร้างพาร์ทไฟล์
                if (!File.Exists(yamlPath)) { // v1.0 - 06/07/2026 - ถ้าไม่มีไฟล์
                    return false; // v1.0 - 06/07/2026 - คืนค่าเท็จ
                }
                foreach (var line in File.ReadLines(yamlPath)) { // v1.0 - 06/07/2026 - อ่านทีละบรรทัด
                    if (string.IsNullOrWhiteSpace(line)) { // v1.0 - 06/07/2026 - ถ้าว่างเปล่า
                        continue; // v1.0 - 06/07/2026 - ข้าม
                    }
                    var trimmed = line.TrimStart(); // v1.0 - 06/07/2026 - ตัดช่องว่างด้านหน้า
                    if (!trimmed.StartsWith("version:", StringComparison.OrdinalIgnoreCase)) { // v1.0 - 06/07/2026 - ถ้าไม่ขึ้นด้วย "version:"
                        continue; // v1.0 - 06/07/2026 - ข้าม
                    }
                    var idx = trimmed.IndexOf(':'); // v1.0 - 06/07/2026 - หาตำแหน่งเครื่องหมาย colon
                    if (idx < 0 || idx + 1 >= trimmed.Length) { // v1.0 - 06/07/2026 - ถ้าไม่พบหรือไม่มีข้อมูลต่อท้าย
                        continue; // v1.0 - 06/07/2026 - ข้าม
                    }
                    var value = trimmed.Substring(idx + 1).Trim(); // v1.0 - 06/07/2026 - ตัดเอาค่าด้านหลังมา
                    if (string.IsNullOrEmpty(value)) { // v1.0 - 06/07/2026 - ถ้าว่างเปล่า
                        continue; // v1.0 - 06/07/2026 - ข้าม
                    }
                    version = value.Trim('\"', '\''); // v1.0 - 06/07/2026 - ตัดเครื่องหมายคำพูดทิ้ง
                    return !string.IsNullOrEmpty(version); // v1.0 - 06/07/2026 - คืนค่าผลการตรวจสอบ
                }
            } catch { // v1.0 - 06/07/2026 - เมื่อมีข้อผิดพลาด
                // Ignore parsing errors and fall back to other strategies.
            }
            return false; // v1.0 - 06/07/2026 - คืนค่าเท็จ
        }

        public async Task<List<InstalledHubSinger>> GetInstalledAsync(IEnumerable<SingerHubEntry> registry) { // v1.0 - 06/07/2026 - ตรวจสอบรายชื่อนักร้องในเครื่อง
            return await Task.Run(() => { // v1.0 - 06/07/2026 - รันแบบ Async
                var result = new List<InstalledHubSinger>(); // v1.0 - 06/07/2026 - สร้าง List ไว้รอรับข้อมูล
                var ufrEntries = registry // v1.0 - 06/07/2026 - คิวรีแยกเฉพาะระบบ UFR
                    .Where(e => string.Equals(e.Host ?? "delta", "ufr", StringComparison.OrdinalIgnoreCase)) // v1.0 - 06/07/2026 - เปลี่ยนค่า default fallback เป็น delta แทน lunai
                    .ToList(); // v1.0 - 06/07/2026 - ดึงมาเป็น List
                var brapaEntries = registry // v1.0 - 06/07/2026 - คิวรีแยกของ BRAPA
                    .Where(e => string.Equals(e.Host ?? "delta", "brapa", StringComparison.OrdinalIgnoreCase)) // v1.0 - 06/07/2026 - เปลี่ยน fallback เป็น delta
                    .ToList(); // v1.0 - 06/07/2026 - ดึงมาเป็น List

                var singers = SingerManager.Inst.Singers.Values; // v1.0 - 06/07/2026 - ดึงรายชื่อนักร้องที่มีในระบบ OpenUtau
                foreach (var singer in singers) { // v1.0 - 06/07/2026 - วนลูปตรวจสอบนักร้องทีละคน
                    if (string.IsNullOrEmpty(singer?.Name) || string.IsNullOrEmpty(singer.Location)) continue; // v1.0 - 06/07/2026 - ถ้าข้อมูลไม่ครบให้ข้าม
                    var rawName = singer.Name?.Trim() ?? string.Empty; // v1.0 - 06/07/2026 - ดึงชื่อมาเตรียมวิเคราะห์
                    string configVersion; // v1.0 - 06/07/2026 - เตรียมตัวแปรเก็บเวอร์ชัน
                    var hasConfigVersion = TryGetVersionFromCharacterYaml(singer.Location, out configVersion); // v1.0 - 06/07/2026 - ลองดึงเวอร์ชันจาก YAML

                    if (ufrEntries.Count > 0 && !string.IsNullOrEmpty(rawName)) { // v1.0 - 06/07/2026 - เช็คสำหรับ UFR
                        foreach (var entry in ufrEntries) { // v1.0 - 06/07/2026 - วนลูปเทียบชื่อ
                            var targetName = entry.Name?.Trim(); // v1.0 - 06/07/2026 - ชื่อเป้าหมาย
                            if (string.IsNullOrEmpty(targetName)) { // v1.0 - 06/07/2026 - ถ้าว่าง
                                continue; // v1.0 - 06/07/2026 - ข้าม
                            }
                            if (rawName.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0) { // v1.0 - 06/07/2026 - ถ้าชื่อตรงกัน
                                var displayNameUfr = targetName; // v1.0 - 06/07/2026 - ใช้ชื่อนั้น
                                string versionUfr; // v1.0 - 06/07/2026 - เตรียมตัวแปรเวอร์ชัน
                                if (hasConfigVersion && !string.IsNullOrEmpty(configVersion)) { // v1.0 - 06/07/2026 - ถ้ามีใน yaml
                                    versionUfr = configVersion; // v1.0 - 06/07/2026 - ใช้ค่านั้น
                                } else if (!TryParseTrailingVersion(rawName, out versionUfr)) { // v1.0 - 06/07/2026 - ถ้าไม่มี ลองตัดจากชื่อ
                                    versionUfr = entry.Version?.Trim() ?? string.Empty; // v1.0 - 06/07/2026 - ถ้ายังไม่ได้ ก็ใช้จาก Registry
                                }
                                result.Add(new InstalledHubSinger { // v1.0 - 06/07/2026 - เพิ่มนักร้องเข้าระบบ
                                    FolderPath = singer.Location, // v1.0 - 06/07/2026 - ระบุพาร์ท
                                    DisplayName = displayNameUfr, // v1.0 - 06/07/2026 - ระบุชื่อ
                                    Version = versionUfr, // v1.0 - 06/07/2026 - ระบุเวอร์ชัน
                                    IsDelta = IsDeltaSinger(singer.Location), // v1.0 - 06/07/2026 - เปลี่ยนเป็น IsDelta แทน IsLunai
                                });
                                goto NextSinger; // v1.0 - 06/07/2026 - ข้ามไปตรวจสอบนักร้องคนต่อไป
                            }
                        }
                    }

                    if (brapaEntries.Count > 0 && !string.IsNullOrEmpty(rawName)) { // v1.0 - 06/07/2026 - ทำการเช็คในกลุ่มของ BRAPA รูปแบบเดียวกัน
                        foreach (var entry in brapaEntries) { // v1.0 - 06/07/2026 - วนลูป
                            var targetName = entry.Name?.Trim(); // v1.0 - 06/07/2026 - ดึงชื่อเป้าหมาย
                            if (string.IsNullOrEmpty(targetName)) { // v1.0 - 06/07/2026 - ถ้าว่าง
                                continue; // v1.0 - 06/07/2026 - ข้าม
                            }
                            if (rawName.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0) { // v1.0 - 06/07/2026 - เทียบชื่อ
                                var displayNameBrapa = targetName; // v1.0 - 06/07/2026 - ระบุชื่อ
                                string versionBrapa; // v1.0 - 06/07/2026 - เตรียมเวอร์ชัน
                                if (hasConfigVersion && !string.IsNullOrEmpty(configVersion)) { // v1.0 - 06/07/2026 - ตรวจสอบ YAML
                                    versionBrapa = configVersion; // v1.0 - 06/07/2026 - ใช้ค่า YAML
                                } else if (!TryParseBrapaNameVersion(rawName, out _, out versionBrapa)) { // v1.0 - 06/07/2026 - ตรวจสอบ Regex
                                    versionBrapa = entry.Version?.Trim() ?? string.Empty; // v1.0 - 06/07/2026 - ใช้ค่า Registry
                                }
                                result.Add(new InstalledHubSinger { // v1.0 - 06/07/2026 - เพิ่มนักร้อง
                                    FolderPath = singer.Location, // v1.0 - 06/07/2026 - ระบุพาร์ท
                                    DisplayName = displayNameBrapa, // v1.0 - 06/07/2026 - ระบุชื่อ
                                    Version = versionBrapa, // v1.0 - 06/07/2026 - ระบุเวอร์ชัน
                                    IsDelta = IsDeltaSinger(singer.Location), // v1.0 - 06/07/2026 - เปลี่ยนเป็น IsDelta เพื่อตรวจสอบว่าเป็นของ DELTA หรือไม่
                                });
                                goto NextSinger; // v1.0 - 06/07/2026 - ข้ามไปตัวถัดไป
                            }
                        }
                    }

                    string displayName; // v1.0 - 06/07/2026 - ตัวแปรเก็บชื่อมาตรฐาน
                    string version; // v1.0 - 06/07/2026 - ตัวแปรเก็บเวอร์ชันมาตรฐาน
                    if (TryParseNameVersion(rawName, out displayName, out version) // v1.0 - 06/07/2026 - ใช้ Regex ดึงข้อมูล
                        || TryParseUfrNameVersion(rawName, out displayName, out version) // v1.0 - 06/07/2026 - กรณีหลุดใช้ Regex ตัวอื่น
                        || TryParseBrapaNameVersion(rawName, out displayName, out version)) { // v1.0 - 06/07/2026 - ลองทุกแพทเทิร์น

                        if (hasConfigVersion && !string.IsNullOrEmpty(configVersion)) { // v1.0 - 06/07/2026 - หาก yaml มีก็ให้ความสำคัญก่อน
                            version = configVersion; // v1.0 - 06/07/2026 - อัปเดตเวอร์ชัน
                        }

                        result.Add(new InstalledHubSinger { // v1.0 - 06/07/2026 - ลงทะเบียนนักร้องแบบมาตรฐาน (ปัจจุบันทำงานร่วมกับ DELTA SYNTH)
                            FolderPath = singer.Location, // v1.0 - 06/07/2026 - ระบุพาร์ท
                            DisplayName = displayName, // v1.0 - 06/07/2026 - ระบุชื่อ
                            Version = version, // v1.0 - 06/07/2026 - ระบุเวอร์ชัน
                            IsDelta = IsDeltaSinger(singer.Location), // v1.0 - 06/07/2026 - ตรวจสอบความเป็น DELTA
                        });
                    } else { // v1.0 - 06/07/2026 - หากดึงอะไรไม่ได้เลย
                        string fallbackVersion = string.Empty; // v1.0 - 06/07/2026 - เตรียมตัวแปรชั่วคราว
                        if (hasConfigVersion && !string.IsNullOrEmpty(configVersion)) { // v1.0 - 06/07/2026 - ดึงจาก yaml แทน
                            fallbackVersion = configVersion; // v1.0 - 06/07/2026 - ตั้งค่าเวอร์ชัน
                        } else if (!string.IsNullOrWhiteSpace(singer.Version)) { // v1.0 - 06/07/2026 - หรือดึงจากคลาส singer เก่า
                            fallbackVersion = singer.Version.Trim(); // v1.0 - 06/07/2026 - ตั้งค่า
                        }

                        if (!string.IsNullOrEmpty(fallbackVersion)) { // v1.0 - 06/07/2026 - ถ้ามีเวอร์ชันแล้ว
                            result.Add(new InstalledHubSinger { // v1.0 - 06/07/2026 - แอดเข้าสู่ List
                                FolderPath = singer.Location, // v1.0 - 06/07/2026 - ระบุพาร์ท
                                DisplayName = singer.Name.Trim(), // v1.0 - 06/07/2026 - ระบุชื่อที่คลีนแล้ว
                                Version = fallbackVersion, // v1.0 - 06/07/2026 - ระบุเวอร์ชัน
                                IsDelta = IsDeltaSinger(singer.Location), // v1.0 - 06/07/2026 - ตรวจสอบ DELTA SYNTH
                            });
                        }
                    }
NextSinger: // v1.0 - 06/07/2026 - Label สำหรับกระโดด
                    ; // v1.0 - 06/07/2026 - สิ้นสุดรอบการทำงานของนักร้องหนึ่งคน
                }
                return result; // v1.0 - 06/07/2026 - คืนค่าทั้งหมดเมื่อเช็คครบ
            });
        }

        public static bool TryParseNameVersion(string nameLine, out string displayName, out string version) { // v1.0 - 06/07/2026 - ฟังก์ชันวิเคราะห์ชื่อ
            displayName = nameLine?.Trim() ?? string.Empty; // v1.0 - 06/07/2026 - ลบช่องว่าง
            version = string.Empty; // v1.0 - 06/07/2026 - รีเซ็ตเวอร์ชัน
            if (string.IsNullOrWhiteSpace(displayName)) return false; // v1.0 - 06/07/2026 - ถ้าว่างให้ออก
            var m = NameVersionRegex.Match(displayName); // v1.0 - 06/07/2026 - จับคู่ Regex
            if (m.Success) { // v1.0 - 06/07/2026 - ถ้าเจอ
                displayName = m.Groups[1].Value.Trim(); // v1.0 - 06/07/2026 - ดึงชื่อ
                version = m.Groups[2].Value; // v1.0 - 06/07/2026 - ดึงเวอร์ชัน
                return true; // v1.0 - 06/07/2026 - คืนค่าความสำเร็จ
            }
            return false; // v1.0 - 06/07/2026 - คืนค่าล้มเหลว
        }

        public static bool TryParseUfrNameVersion(string nameLine, out string displayName, out string version) { // v1.0 - 06/07/2026 - ฟังก์ชันวิเคราะห์ชื่อแบบ UFR
            displayName = nameLine?.Trim() ?? string.Empty; // v1.0 - 06/07/2026 - ลบช่องว่าง
            version = string.Empty; // v1.0 - 06/07/2026 - รีเซ็ตเวอร์ชัน
            if (string.IsNullOrWhiteSpace(displayName)) return false; // v1.0 - 06/07/2026 - ถ้าว่างให้ออก
            var m = UfrNameVersionRegex.Match(displayName); // v1.0 - 06/07/2026 - จับคู่ Regex
            if (m.Success) { // v1.0 - 06/07/2026 - ถ้าเจอ
                displayName = m.Groups[1].Value.Trim(); // v1.0 - 06/07/2026 - ดึงชื่อ
                version = m.Groups[2].Value.Trim(); // v1.0 - 06/07/2026 - ดึงเวอร์ชัน
                return !string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(version); // v1.0 - 06/07/2026 - คืนค่าจริงถ้ามีข้อมูล
            }
            return false; // v1.0 - 06/07/2026 - คืนค่าล้มเหลว
        }

        public static bool TryParseTrailingVersion(string nameLine, out string version) { // v1.0 - 06/07/2026 - ฟังก์ชันวิเคราะห์เวอร์ชันส่วนท้าย
            version = string.Empty; // v1.0 - 06/07/2026 - รีเซ็ต
            if (string.IsNullOrWhiteSpace(nameLine)) return false; // v1.0 - 06/07/2026 - ถ้าว่างให้ออก
            var m = TrailingVersionRegex.Match(nameLine); // v1.0 - 06/07/2026 - จับคู่
            if (!m.Success) return false; // v1.0 - 06/07/2026 - ถ้าไม่พบให้ออก
            version = m.Groups[1].Value.Trim(); // v1.0 - 06/07/2026 - ดึงค่า
            return !string.IsNullOrEmpty(version); // v1.0 - 06/07/2026 - คืนผลลัพธ์
        }

        public static bool TryParseBrapaNameVersion(string nameLine, out string displayName, out string version) { // v1.0 - 06/07/2026 - วิเคราะห์แบบ BRAPA
            displayName = nameLine?.Trim() ?? string.Empty; // v1.0 - 06/07/2026 - ตัดคำ
            version = string.Empty; // v1.0 - 06/07/2026 - รีเซ็ต
            if (string.IsNullOrWhiteSpace(displayName)) return false; // v1.0 - 06/07/2026 - ถ้าว่างให้ออก
            var m = BrapaNameVersionRegex.Match(displayName); // v1.0 - 06/07/2026 - ตรวจสอบ Regex
            if (m.Success) { // v1.0 - 06/07/2026 - ถ้ามี
                displayName = m.Groups[1].Value.Trim(); // v1.0 - 06/07/2026 - ใส่ชื่อ
                version = m.Groups[2].Value.Trim(); // v1.0 - 06/07/2026 - ใส่เวอร์ชัน
                return !string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(version); // v1.0 - 06/07/2026 - คืนผล
            }
            return false; // v1.0 - 06/07/2026 - คืนค่าล้มเหลว
        }

        public static int CompareVersions(string a, string b) { // v1.0 - 06/07/2026 - ฟังก์ชันเปรียบเทียบความใหม่ของเวอร์ชัน
            if (Version.TryParse(a?.Trim(), out var va) && Version.TryParse(b?.Trim(), out var vb)) // v1.0 - 06/07/2026 - ถ้าแปลงเป็นคลาส Version ได้
                return va.CompareTo(vb); // v1.0 - 06/07/2026 - ใช้คลาส Version เทียบ
            if (int.TryParse(a?.Trim(), out var ia) && int.TryParse(b?.Trim(), out var ib)) // v1.0 - 06/07/2026 - ถ้าเป็นตัวเลขตรงๆ
                return ia.CompareTo(ib); // v1.0 - 06/07/2026 - เทียบแบบคณิตศาสตร์
            return string.Compare(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase); // v1.0 - 06/07/2026 - ท้ายที่สุดเทียบด้วยความยาวสตริง
        }

        static string GetArchiveFolderName(string downloadUrl) { // v1.0 - 06/07/2026 - ฟังก์ชันแกะชื่อโฟลเดอร์ออกจาก URL ไฟล์ ZIP
            if (string.IsNullOrWhiteSpace(downloadUrl)) return "Singer"; // v1.0 - 06/07/2026 - ถ้าว่างให้ตั้งชื่อว่า Singer
            try { // v1.0 - 06/07/2026 - กันแครช
                var fileName = Path.GetFileName(Uri.UnescapeDataString(new Uri(downloadUrl).AbsolutePath)); // v1.0 - 06/07/2026 - ถอดรหัส URL และเอาชื่อไฟล์
                var name = Path.GetFileNameWithoutExtension(fileName); // v1.0 - 06/07/2026 - ตัดนามสกุลไฟล์ทิ้ง
                if (string.IsNullOrWhiteSpace(name)) return "Singer"; // v1.0 - 06/07/2026 - ถ้าไม่ได้ชื่อ ให้คืนค่า Singer
                var invalid = Path.GetInvalidFileNameChars(); // v1.0 - 06/07/2026 - ค้นหาอักขระต้องห้ามในระบบ OS
                foreach (var c in invalid) // v1.0 - 06/07/2026 - วนลูป
                    name = name.Replace(c, '_'); // v1.0 - 06/07/2026 - แทนที่ด้วย _
                return name.Trim(); // v1.0 - 06/07/2026 - คืนชื่อที่คลีนแล้ว
            } catch { // v1.0 - 06/07/2026 - ถ้าพัง
                return "Singer"; // v1.0 - 06/07/2026 - คืนค่าสำรอง
            }
        }

        public async Task InstallOrUpdateAsync( // v1.0 - 06/07/2026 - ฟังก์ชันติดตั้งหรืออัปเดตนักร้องจากไฟล์ดาวน์โหลด
            string downloadUrl, // v1.0 - 06/07/2026 - รับลิงก์ดาวน์โหลด
            string? existingFolderPathToRemove, // v1.0 - 06/07/2026 - รับโฟลเดอร์เก่าเพื่อเตรียมลบ
            string? host, // v1.0 - 06/07/2026 - รับโฮสต์
            IProgress<int>? progress = null) { // v1.0 - 06/07/2026 - ตัวแปรแจ้งความคืบหน้า (Progress)
            if (string.IsNullOrWhiteSpace(downloadUrl)) // v1.0 - 06/07/2026 - ตรวจสอบว่าลิงก์ต้องมี
                throw new ArgumentException("Download URL is required.", nameof(downloadUrl)); // v1.0 - 06/07/2026 - แจ้ง Error หากไม่มีลิงก์
            var basePath = PathManager.Inst.SingersInstallPath; // v1.0 - 06/07/2026 - ดึงพาร์ทติดตั้งนักร้องหลัก
            string installPath; // v1.0 - 06/07/2026 - ตัวแปรที่อยู่ติดตั้ง
            if (!string.IsNullOrEmpty(host) && host.Equals("ufr", StringComparison.OrdinalIgnoreCase)) { // v1.0 - 06/07/2026 - ยกเว้นให้ระบบ UFR ลงโฟลเดอร์นอก
                installPath = basePath; // v1.0 - 06/07/2026 - ใช้พาร์ทหลัก
            } else { // v1.0 - 06/07/2026 - สำหรับ DELTA SYNTH และระบบอื่น
                var folderName = GetArchiveFolderName(downloadUrl); // v1.0 - 06/07/2026 - ดึงชื่อจากลิงก์
                installPath = Path.Combine(basePath, folderName); // v1.0 - 06/07/2026 - ต่อกันเป็นพาร์ทโฟลเดอร์ย่อย
            }

            byte[] data; // v1.0 - 06/07/2026 - ตัวแปรเก็บข้อมูลไบต์
            using (var client = new HttpClient()) { // v1.0 - 06/07/2026 - เปิดการเชื่อมต่อเพื่อดาวน์โหลด
                client.Timeout = TimeSpan.FromMinutes(10); // v1.0 - 06/07/2026 - ตั้งเวลาสูงสุด 10 นาที
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead); // v1.0 - 06/07/2026 - โหลดข้อมูล
                response.EnsureSuccessStatusCode(); // v1.0 - 06/07/2026 - ตรวจสอบ 200 OK
                var contentLength = response.Content.Headers.ContentLength; // v1.0 - 06/07/2026 - ดึงขนาดไฟล์
                using var responseStream = await response.Content.ReadAsStreamAsync(); // v1.0 - 06/07/2026 - อ่านเป็นสตรีม
                using var ms = new MemoryStream(); // v1.0 - 06/07/2026 - เตรียม MemoryStream สำหรับเก็บข้อมูล
                var buffer = new byte[81920]; // v1.0 - 06/07/2026 - กำหนดขนาด Buffer 80KB ต่อครั้ง
                long totalRead = 0; // v1.0 - 06/07/2026 - จำนวนที่โหลดได้แล้ว
                int read; // v1.0 - 06/07/2026 - ขนาดแพ็กเก็ตที่เข้ามา
                if (contentLength.HasValue && progress != null) progress.Report(0); // v1.0 - 06/07/2026 - แจ้งเริ่มต้นดาวน์โหลด 0%
                while ((read = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0) { // v1.0 - 06/07/2026 - วนลูปดาวน์โหลด
                    ms.Write(buffer, 0, read); // v1.0 - 06/07/2026 - เขียนลง Memory
                    totalRead += read; // v1.0 - 06/07/2026 - นับเพิ่ม
                    if (contentLength.HasValue && progress != null) { // v1.0 - 06/07/2026 - คำนวณเปอร์เซ็นต์
                        var percent = (int)(totalRead * 100 / contentLength.Value); // v1.0 - 06/07/2026 - สูตรเปอร์เซ็นต์
                        progress.Report(Math.Min(99, percent)); // v1.0 - 06/07/2026 - อัปเดต UI สูงสุดที่ 99 (รอคลายไฟล์)
                    }
                }
                if (progress != null) progress.Report(100); // v1.0 - 06/07/2026 - ดาวน์โหลดเสร็จ 100%
                data = ms.ToArray(); // v1.0 - 06/07/2026 - แปลงเป็น Array ไว้
            }

            await Task.Run(() => { // v1.0 - 06/07/2026 - เริ่มการคลายไฟล์บน Thread รอง
                if (!string.IsNullOrEmpty(existingFolderPathToRemove) && Directory.Exists(existingFolderPathToRemove)) { // v1.0 - 06/07/2026 - ถ้าเป็นการอัปเดตและมีของเก่า
                    try { // v1.0 - 06/07/2026 - ลองลบของเก่าทิ้ง
                        Directory.Delete(existingFolderPathToRemove, true); // v1.0 - 06/07/2026 - สั่งลบทั้งยวง
                    } catch (Exception e) { // v1.0 - 06/07/2026 - ถ้าลบไม่ได้
                        Log.Warning(e, "Failed to remove old singer folder {Path}", existingFolderPathToRemove); // v1.0 - 06/07/2026 - แปะ Log ไว้
                        throw; // v1.0 - 06/07/2026 - โยน Error ทิ้งให้ระบบจัดการ
                    }
                }
                Directory.CreateDirectory(installPath); // v1.0 - 06/07/2026 - สร้างโฟลเดอร์สำหรับแตกไฟล์
                using var archive = ArchiveFactory.OpenArchive(new MemoryStream(data), new ReaderOptions()); // v1.0 - 06/07/2026 - โหลดระบบถอดรหัส Zip
                foreach (var entry in archive.Entries) { // v1.0 - 06/07/2026 - วนลูปอ่านแต่ละไฟล์ใน Zip
                    if (string.IsNullOrEmpty(entry.Key) || entry.Key.Contains("..")) continue; // v1.0 - 06/07/2026 - ตรวจสอบความปลอดภัย ป้องกัน Path Traversal
                    var destPath = Path.Combine(installPath, entry.Key); // v1.0 - 06/07/2026 - ตั้งที่อยู่ปลายทางไฟล์
                    var dir = Path.GetDirectoryName(destPath); // v1.0 - 06/07/2026 - ดึงโฟลเดอร์ปลายทาง
                    if (!entry.IsDirectory && !string.IsNullOrEmpty(dir)) { // v1.0 - 06/07/2026 - ถ้าไม่ใช่โฟลเดอร์
                        Directory.CreateDirectory(dir); // v1.0 - 06/07/2026 - สร้างโฟลเดอร์ให้ชัวร์ก่อนเขียน
                        entry.WriteToFile(destPath); // v1.0 - 06/07/2026 - เขียนไฟล์ลง Harddisk
                    }
                }
            });
        }

        public async Task UninstallAsync(string folderPath) { // v1.0 - 06/07/2026 - ฟังก์ชันถอนการติดตั้งนักร้อง
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) // v1.0 - 06/07/2026 - ถ้าไม่มีโฟลเดอร์อยู่แล้ว
                return; // v1.0 - 06/07/2026 - ถือว่าเสร็จงาน ให้ออกเลย
            await Task.Run(() => Directory.Delete(folderPath, true)); // v1.0 - 06/07/2026 - สั่งลบโฟลเดอร์แบบ Asynchronous
        }
    }
}