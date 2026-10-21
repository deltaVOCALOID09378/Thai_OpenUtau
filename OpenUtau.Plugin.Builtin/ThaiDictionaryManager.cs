#pragma warning disable CS0618, CS0649, CS8632, CS0108
#nullable enable
// ============================================================================
// Date         : 21/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : ThaiDictionaryManager.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by Patiphat Wongyai ]
// File Location: OpenUtau.Plugin.Builtin\ThaiDictionaryManager.cs
// Description  : Unified Thai dictionary manager for OpenUtau phonemizers.
//                Externalizes dictionary loading, prevents redundant file reads,
//                and ensures thread-safe centralized dictionary caching.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
/// 
/// DELTA SYNTH Standard: Unified dictionary manager for Thai phonemizers.
/// Externalizes dictionary loading and prevents redundant file reads.
/// 
public static class ThaiDictionaryManager {
private static readonly Dictionary<string, string> customDictionary = new Dictionary<string, string>();
private static bool isLoaded = false;
private static readonly object lockObj = new object();

    /// <summary>
    /// โหลดข้อมูลพจนานุกรมส่วนกลาง (Load unified dictionary data)
    /// </summary>
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
                
                int totalLoadedEntries = 0;
                foreach (var path in dictPaths) {
                    if (File.Exists(path)) {
                        var lines = File.ReadAllLines(path, Encoding.UTF8);
                        foreach (var line in lines) {
                            // ข้ามบรรทัดคอมเมนต์หรือบรรทัดว่าง (Skip comments or empty lines)
                            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split(new[] { '=', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2) {
                                string key = parts[0].Trim();
                                string val = parts[1].Trim();
                                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val)) {
                                    customDictionary[key] = val;
                                    totalLoadedEntries++;
                                }
                            }
                        }
                    }
                }
                Log.Information($"[DELTA SYNTH] โหลดพจนานุกรมภาษาไทยส่วนกลางสำเร็จ จำนวน {customDictionary.Count} คำ (Total entries parsed: {totalLoadedEntries}).");
            } catch (Exception ex) {
                Log.Error(ex, "เกิดข้อผิดพลาดในการโหลดพจนานุกรมภาษาไทย (Failed to load custom Thai dictionary).");
            }
            isLoaded = true;
        }
    }

    /// <summary>
    /// นำข้อมูลไปใช้กับพจนานุกรมเป้าหมาย (Apply data to target dictionary)
    /// </summary>
    public static void ApplyToDictionary(Dictionary<string, string> targetDict, Func<string, string>? transformer = null) {
        LoadDictionary();
        lock (lockObj) {
            foreach (var kvp in customDictionary) {
                targetDict[kvp.Key] = transformer != null ? transformer(kvp.Value) : kvp.Value;
            }
        }
    }
}


}