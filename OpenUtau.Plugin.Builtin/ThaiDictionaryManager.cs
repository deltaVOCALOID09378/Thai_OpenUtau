#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable
// ============================================================================
// Made And Checked By DELTA SYNTH & All Agentic AI
// Original by Patiphat Wongyai
// Version: 1.0
// Description: ตัวจัดการพจนานุกรมภาษาไทยส่วนกลาง (Unified Thai dictionary manager)
// ช่วยลดความซ้ำซ้อนในการอ่านไฟล์สำหรับ Phonemizers (Externalizes dictionary loading)
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// DELTA SYNTH Standard: Unified dictionary manager for Thai phonemizers.
    /// Externalizes dictionary loading and prevents redundant file reads.
    /// </summary>
    public static class ThaiDictionaryManager {
        private static Dictionary<string, string> customDictionary = new Dictionary<string, string>();
        private static bool isLoaded = false;
        private static readonly object lockObj = new object();

        // โหลดข้อมูลพจนานุกรมส่วนกลาง (Load unified dictionary data)
        public static void LoadDictionary() {
            if (isLoaded) return;
            lock (lockObj) {
                if (isLoaded) return;
                try {
                    // รายชื่อเส้นทางไฟล์พจนานุกรมที่รองรับ (Supported dictionary file paths)
                    string[] dictPaths = {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionaries", "dsdict-th.txt"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionaries", "th_vccv_custom_dict.txt"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionaries", "th_custom_dict.txt"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionary", "words_th.txt"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionary", "words_th_dict.txt"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionary", "words_th_vccv.txt"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionary", "TH_VCCV_Dict.txt")
                    };
                    
                    foreach (var path in dictPaths) {
                        if (File.Exists(path)) {
                            var lines = File.ReadAllLines(path, Encoding.UTF8);
                            foreach (var line in lines) {
                                // ข้ามบรรทัดคอมเมนต์หรือบรรทัดว่าง (Skip comments or empty lines)
                                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                                var parts = line.Split(new[] { '=', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2) {
                                    customDictionary[parts[0].Trim()] = parts[1].Trim();
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Log.Error(ex, "เกิดข้อผิดพลาดในการโหลดพจนานุกรมภาษาไทย (Failed to load custom Thai dictionary).");
                }
                isLoaded = true;
            }
        }

        // นำข้อมูลไปใช้กับพจนานุกรมเป้าหมาย (Apply data to target dictionary)
        public static void ApplyToDictionary(Dictionary<string, string> targetDict, Func<string, string>? transformer = null) {
            LoadDictionary();
            foreach (var kvp in customDictionary) {
                targetDict[kvp.Key] = transformer != null ? transformer(kvp.Value) : kvp.Value;
            }
        }
    }
}