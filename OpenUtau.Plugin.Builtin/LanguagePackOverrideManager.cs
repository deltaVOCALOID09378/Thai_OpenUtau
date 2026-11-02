#pragma warning disable CA2255
#nullable enable
// ============================================================================
// Date         : 28/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : LanguagePackOverrideManager.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & Gemini AI [ Original by DELTA SYNTH ]
// File Location: OpenUtau.Plugin.Builtin/LanguagePackOverrideManager.cs
// Description  : Plug-and-Play Manager that immediately overrides default/built-in
//                phonemizers in OpenUtau with the enhanced editions from
//                'The Language Editor Pack.dll' without background polling loops.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenUtau.Api;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// DELTA SYNTH Standard: Dynamic Plug-and-Play Language Pack Override Manager.
    /// Replaces default built-in phonemizers with The Language Editor Pack equivalents.
    /// </summary>
    public static class LanguagePackOverrideManager {
        private static bool isInitialized = false;
        private static readonly object lockObj = new object();
        private static List<Type>? cachedOurPhonemizerTypes;

        [ModuleInitializer]
        public static void Initialize() {
            try {
                // Apply immediately upon assembly load
                ApplyOverrides();

                // Hook AssemblyLoad to catch late-loaded Core or Builtin assemblies event-driven
                AppDomain.CurrentDomain.AssemblyLoad += (s, e) => {
                    try {
                        var name = e.LoadedAssembly.GetName().Name;
                        if (name != null && (name.StartsWith("OpenUtau", StringComparison.OrdinalIgnoreCase) ||
                                             name.Contains("Language", StringComparison.OrdinalIgnoreCase))) {
                            ApplyOverrides();
                        }
                    } catch { }
                };
            } catch (Exception ex) {
                Log.Error(ex, "[DELTA SYNTH] LanguagePackOverrideManager failed during module initialization.");
            }
        }

        public static void EnsureApplied() {
            ApplyOverrides();
        }

        public static void ApplyOverrides() {
            lock (lockObj) {
                try {
                    var factoriesField = typeof(PhonemizerFactory).GetField("factories", BindingFlags.Static | BindingFlags.NonPublic);
                    if (factoriesField == null) return;

                    var factories = factoriesField.GetValue(null) as Dictionary<Type, PhonemizerFactory>;
                    if (factories == null) return;

                    var ourAssembly = typeof(LanguagePackOverrideManager).Assembly;
                    cachedOurPhonemizerTypes ??= ourAssembly.GetExportedTypes()
                        .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Phonemizer)))
                        .ToList();

                    int replacedCount = 0;

                    foreach (var ourType in cachedOurPhonemizerTypes) {
                        var attr = ourType.GetCustomAttribute<PhonemizerAttribute>();
                        if (attr == null || string.IsNullOrEmpty(attr.Tag)) continue;

                        // Find any existing factory with same FullName or Tag from another assembly
                        var staleKeys = factories.Keys.Where(k => 
                            k.Assembly != ourAssembly && 
                            (k.FullName == ourType.FullName || factories[k].tag == attr.Tag)).ToList();

                        foreach (var key in staleKeys) {
                            factories.Remove(key);
                            replacedCount++;
                        }

                        // Register our enhanced factory if not already registered
                        if (!factories.ContainsKey(ourType)) {
                            factories[ourType] = PhonemizerFactory.Get(ourType);
                        }
                    }

                    // Rebuild the ordered list so OpenUtau UI dropdowns reflect the pack immediately
                    if (replacedCount > 0 || !isInitialized) {
                        PhonemizerFactory.BuildList();
                    }

                    if (!isInitialized && replacedCount > 0) {
                        isInitialized = true;
                        Log.Information($"[DELTA SYNTH] The Language Editor Pack successfully activated: Replaced {replacedCount} built-in phonemizer(s) with plugin edition.");
                    }
                } catch (Exception ex) {
                    Log.Warning(ex, "[DELTA SYNTH] LanguagePackOverrideManager: Notice during override synchronization.");
                }
            }
        }
    }
}
