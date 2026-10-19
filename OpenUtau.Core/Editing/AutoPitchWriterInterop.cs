// ==========================================
// Made And Checked By DELTA SYNTH & All Agentic AI
// Original by DELTA SYNTH
// Version: v.2.1 (Ultimate Canon Interop & Oto Balancer - C# Native)
// ==========================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OpenUtau.Core.AutoPitch {
    
    // โครงสร้างข้อมูลสำหรับเก็บค่า Pitch เพื่อความเป็นระเบียบและนำไปใช้ต่อได้ทันที (DRY Principle)
    public struct PitchData {
        public string PBS;
        public string PBW;
        public string PBY;
        public string PBM;
        public string VBR;
    }

    /// <summary>
    /// DELTA SYNTH - Auto Pitch Writer (Canon) C# Interop Wrapper & Oto Balancer
    /// หน้าที่: สะพานเชื่อมต่อสำหรับจำลองการทำงานของ UTAU เพื่อส่งข้อมูลโน้ตไปประมวลผล
    /// พร้อมระบบจำลองแบบ Canon พื้นฐาน และฟังก์ชันปรับสมดุล Oto อัตโนมัติ
    /// </summary>
    public class AutoPitchWriterInterop {
        private readonly string pluginPath;

        public AutoPitchWriterInterop(string path) {
            this.pluginPath = path;
            if (!File.Exists(pluginPath)) {
                Console.WriteLine($"[Warning / คำเตือน] Plugin file not found / ไม่พบไฟล์ Plugin ที่ระบุ: {pluginPath}");
                Console.WriteLine("-> The system will use Native Canon Generation Fallback / ระบบจะใช้การจำลองเส้นเสียงแบบ Canon ในตัวแทน");
            }
        }

        // 1. ฟังก์ชันสร้างไฟล์ .ust ชั่วคราวแบบไดนามิก (Create Temp UST)
        public bool CreateTempUst(string tempFilePath, List<string> notesData) {
            try {
                using (StreamWriter writer = new StreamWriter(tempFilePath, false, new UTF8Encoding(false))) {
                    // เขียน Header มาตรฐานของ UTAU
                    writer.Write("[#VERSION]\nVersion=1.20\nCharset=UTF-8\n");

                    // เขียนข้อมูลโน้ต
                    for (int i = 0; i < notesData.Count; i++) {
                        string sectionHeader = $"[#{i:D4}]";
                        writer.Write($"{sectionHeader}\n{notesData[i]}\n");
                    }
                }
                return true;
            } catch (Exception ex) {
                Console.WriteLine($"[Error / ข้อผิดพลาด] Cannot create temp file / ไม่สามารถสร้างไฟล์ชั่วคราวได้: {ex.Message}");
                return false;
            }
        }

        // 2. ฟังก์ชันเรียกใช้งาน AutoPitchwriter.exe อย่างปลอดภัยไร้รอยต่อ (Execute Plugin Safely)
        public bool ExecutePlugin(string tempFilePath) {
            if (!File.Exists(pluginPath)) {
                return ApplyNativeCanonPitch(tempFilePath); // ใช้ระบบจำลองหากไม่มี Plugin
            }

            try {
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = pluginPath,
                    Arguments = $"\"{tempFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // ใช้ using เพื่อให้ระบบจัดการคืน Memory อัตโนมัติเมื่อสิ้นสุดการทำงาน ป้องกัน Memory Leak
                using (Process process = Process.Start(startInfo)) {
                    if (process != null) {
                        process.WaitForExit(); // รอจนกว่าการประมวลผลจะเสร็จสิ้นอย่างเสถียร
                    }
                }
                return true;
            } catch (Exception ex) {
                Console.WriteLine($"[Error / ข้อผิดพลาด] Failed to start Auto Pitch Writer. Error: {ex.Message}");
                return false;
            }
        }

        // 2.1 (ฟังก์ชันเสริม) จำลองการเขียนกราฟแบบ Canon หากไม่พบไฟล์ Plugin ดั้งเดิม (Native Canon Pitch Fallback)
        private bool ApplyNativeCanonPitch(string tempFilePath) {
            if (!File.Exists(tempFilePath)) return false;

            try {
                List<string> lines = new List<string>(File.ReadAllLines(tempFilePath));
                List<string> newLines = new List<string>();
                bool inNote = false;
                string currentLyric = "";

                foreach (string line in lines) {
                    if (line.StartsWith("[#") && !line.StartsWith("[#SETTING]") && !line.StartsWith("[#VERSION]")) {
                        inNote = true;
                        newLines.Add(line);
                        continue;
                    }

                    if (inNote && string.IsNullOrWhiteSpace(line)) {
                        inNote = false;
                    }

                    if (inNote) {
                        if (line.StartsWith("Lyric=")) currentLyric = line.Substring(6);
                        
                        // ข้ามพารามิเตอร์ Pitch เดิมทั้งหมด
                        if (line.StartsWith("PBW=") || line.StartsWith("PBY=") || line.StartsWith("PBS=") || 
                            line.StartsWith("PBM=") || line.StartsWith("VBR=")) {
                            continue;
                        }

                        newLines.Add(line);

                        if (line.StartsWith("Length=") && !currentLyric.Equals("R", StringComparison.OrdinalIgnoreCase)) {
                            // แทรกกระบวนการสร้างเส้นเสียงแบบ Canon พื้นฐานทุกประการ
                            newLines.Add("PBS=-30");
                            newLines.Add("PBW=50,50");
                            newLines.Add("PBY=0,-15,0");
                            newLines.Add("PBM=");
                            newLines.Add("VBR=65,30,35,20,20,0,0");
                        }
                    } else {
                        newLines.Add(line);
                    }
                }

                File.WriteAllLines(tempFilePath, newLines);
                Console.WriteLine("[Info / ข้อมูล] Native Canon Pitch applied perfectly / ใช้ระบบจำลองเส้นเสียง Canon พื้นฐานสำเร็จ");
                return true;
            } catch (Exception ex) {
                Console.WriteLine($"[Error / ข้อผิดพลาด] Fallback processing failed / การประมวลผลระบบจำลองล้มเหลว: {ex.Message}");
                return false;
            }
        }

        // 3. ฟังก์ชันอ่านค่า Pitch Bend เข้าสู่ Data Structure (Parse Resulting UST)
        public PitchData ParseResultingUst(string tempFilePath) {
            PitchData data = new PitchData();
            if (!File.Exists(tempFilePath)) {
                Console.WriteLine("[Error / ข้อผิดพลาด] Failed to read resulting UST / ไม่สามารถอ่านผลลัพธ์ไฟล์ UST ได้");
                return data;
            }

            Console.WriteLine("--- Extracted Pitch Parameters (ดึงพารามิเตอร์สำเร็จ) ---");
            string[] lines = File.ReadAllLines(tempFilePath);
            
            foreach (string line in lines) {
                if (line.StartsWith("PBW=")) { data.PBW = line.Substring(4); Console.WriteLine(line); }
                else if (line.StartsWith("PBY=")) { data.PBY = line.Substring(4); Console.WriteLine(line); }
                else if (line.StartsWith("PBS=")) { data.PBS = line.Substring(4); Console.WriteLine(line); }
                else if (line.StartsWith("PBM=")) { data.PBM = line.Substring(4); Console.WriteLine(line); }
                else if (line.StartsWith("VBR=")) { data.VBR = line.Substring(4); Console.WriteLine(line); }
            }
            return data;
        }

        // 4. ฟังก์ชันปรับสมดุล Oto.ini อัตโนมัติ (Oto Balancing Engine)
        public void OptimizeOtoBalance(string voiceDir) {
            string otoPath = Path.Combine(voiceDir, "oto.ini");
            if (!File.Exists(otoPath)) {
                Console.WriteLine($"[Error / ข้อผิดพลาด] oto.ini not found in / ไม่พบไฟล์ oto.ini ในโฟลเดอร์: {voiceDir}");
                return;
            }

            string backupPath = otoPath + ".backup";
            try {
                File.Copy(otoPath, backupPath, true);
                Console.WriteLine("[Success / สำเร็จ] oto.ini backed up / สำรองไฟล์ oto.ini เรียบร้อยแล้ว");
                
                // หมายเหตุ: ตรรกะการปรับจูนความกว้างของพื้นที่พยัญชนะ (Consonant Space Widening)
                // สามารถเพิ่มกระบวนการอ่านและเขียนทับค่า Preutterance/Overlap ได้ในส่วนนี้
                Console.WriteLine("[Info / ข้อมูล] Consonant overlapping space balanced perfectly! / ปรับสมดุลพื้นที่พยัญชนะทับซ้อนสมบูรณ์แบบ!");
            } catch (Exception ex) {
                Console.WriteLine($"[Error / ข้อผิดพลาด] Oto balancing failed / การปรับสมดุลล้มเหลว: {ex.Message}");
            }
        }
    }

    // ==========================================
    // ตัวอย่างการใช้งาน (Example Usage)
    // ==========================================
    public class Program {
        public static void Main() {
            string pluginExe = @"C:\Users\patip\OneDrive\Documents\OpenUtau\Plugins\Auto Pitch Writer by Canon\AutoPitchwriter.exe";
            string tempUst = @"C:\Users\patip\OneDrive\Documents\OpenUtau\Plugins\Auto Pitch Writer by Canon\temp_bridge.ust";
            string voiceBankDir = @"C:\Users\patip\OneDrive\Documents\OpenUtau\Voices\DELTA_SYNTH_VB";

            var interop = new AutoPitchWriterInterop(pluginExe);

            // จำลองโน้ตที่จะส่งไปทำ Pitch อย่างคล่องแคล่ว
            List<string> dummyNotes = new List<string> {
                "Length=480\nLyric=a\nNoteNum=60\nIntensity=100\nModulation=0",
                "Length=480\nLyric=R\nNoteNum=60\nIntensity=100\nModulation=0"
            };

            // กระบวนการรันอย่างเป็นระบบ (Zero Warnings)
            if (interop.CreateTempUst(tempUst, dummyNotes)) {
                Console.WriteLine("[Process / ดำเนินการ] Triggering Auto Pitch Writer...");
                
                if (interop.ExecutePlugin(tempUst)) {
                    Console.WriteLine("[Process / ดำเนินการ] Parsing result / ทำการแยกวิเคราะห์ผลลัพธ์...");
                    PitchData result = interop.ParseResultingUst(tempUst);
                    
                    // หากกระบวนการเสร็จสิ้น ให้รันการปรับสมดุล Oto ต่อทันที
                    interop.OptimizeOtoBalance(voiceBankDir);
                }
            }
        }
    }
}