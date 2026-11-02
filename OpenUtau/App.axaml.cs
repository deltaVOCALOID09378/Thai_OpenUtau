// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : App.axaml.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.App/App.axaml.cs
// Description  : Application entry point and global resource manager.
//                Optimized culture and theme initialization to prevent double-painting 
//                and reduce dictionary lookup overhead.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using OpenUtau.App.Views;
using OpenUtau.Colors;
using Serilog;

namespace OpenUtau.App {
    public class App : Application {
        public override void Initialize() {
            Log.Information("Initializing application.");
            AvaloniaXamlLoader.Load(this);
            InitializeCulture();
            InitializeTheme();
            Log.Information("Initialized application.");
        }

        public override void OnFrameworkInitializationCompleted() {
            Log.Information("Framework initialization completed.");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                desktop.MainWindow = new SplashWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }

        public void InitializeCulture() {
            Log.Information("Initializing culture.");
            string sysLang = CultureInfo.InstalledUICulture.Name;
            string prefLang = Core.Util.Preferences.Default.Language;
            var languages = GetLanguages();
            
            if (languages.ContainsKey(prefLang)) {
                SetLanguage(prefLang, languages);
            } else if (languages.ContainsKey(sysLang)) {
                SetLanguage(sysLang, languages);
                Core.Util.Preferences.Default.Language = sysLang;
                Core.Util.Preferences.Save();
            } else {
                SetLanguage("en-US", languages);
            }

            // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Log.Information("Initialized culture.");
        }

        public static Dictionary<string, IResourceProvider> GetLanguages() {
            if (Current == null) return new Dictionary<string, IResourceProvider>();

            var result = new Dictionary<string, IResourceProvider>();
            // [DELTA SYNTH] Optimized dictionary lookup
            foreach (var key in Current.Resources.Keys.OfType<string>()) {
                if (key.StartsWith("strings-") && 
                    Current.Resources.TryGetResource(key, ThemeVariant.Default, out var res) && 
                    res is IResourceProvider rp) {
                    result[key.Substring(8)] = rp; // "strings-".Length == 8
                }
            }
            return result;
        }

        // [DELTA SYNTH] Overload to pass pre-fetched languages and avoid double-fetching
        public static void SetLanguage(string language, Dictionary<string, IResourceProvider>? languages = null) {
            if (Current == null) return;
            languages ??= GetLanguages();

            // Bulk removal to reduce UI layout invalidation checks
            var merged = Current.Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--) {
                if (languages.Values.Contains(merged[i])) {
                    merged.RemoveAt(i);
                }
            }

            // Always add English as base fallback
            if (language != "en-US" && languages.TryGetValue("en-US", out var baseRes)) {
                merged.Add(baseRes);
            }
            
            // Add the selected language
            if (languages.TryGetValue(language, out var selectedRes)) {
                merged.Add(selectedRes);
            }
        }

        static void InitializeTheme() {
            Log.Information("Initializing theme.");
            try {
                CustomTheme.ListThemes();
            } catch (Exception e) {
                Log.Error(e, "Failed to list custom themes.");
            }
            
            // [DELTA SYNTH] Set initial theme immediately
            SetTheme();
            
            // [DELTA SYNTH] Fire and forget package theme loading to prevent UI blocking
            _ = Task.Run(async () => {
                try {
                    await OudepLoaderRegistry.LoadAllAsync();
                    // Update theme on UI thread if packages brought in new themes
                    Avalonia.Threading.Dispatcher.UIThread.Post(SetTheme);
                } catch (Exception e) {
                    Log.Error(e, "Failed to load themes from packages.");
                }
            });
            Log.Information("Initialized theme.");
        }

        public static void SetTheme() {
            if (Current == null) return;

            string prefTheme = Core.Util.Preferences.Default.ThemeName;
            
            if (prefTheme == BuiltInThemeLoader.LightThemeName) {
                ApplyTheme((IResourceDictionary)Current.Resources["themes-light"]!);
                Current.RequestedThemeVariant = ThemeVariant.Light;
            } 
            else if (prefTheme == BuiltInThemeLoader.DarkThemeName) {
                ApplyTheme((IResourceDictionary)Current.Resources["themes-dark"]!);
                Current.RequestedThemeVariant = ThemeVariant.Dark;
            } 
            else {
                if (BuiltInThemeLoader.TryCreateThemeByName(prefTheme, out var builtInTheme)) {
                    ThemeApplicator.Apply(builtInTheme);
                    Current.RequestedThemeVariant = builtInTheme.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
                } else {
                    CustomTheme.ApplyTheme(prefTheme);
                }
            }
            
            ThemeManager.LoadTheme();
        }

        private static void ApplyTheme(IResourceDictionary resDict) { 
            var res = Current?.Resources;
            if (res == null) return;
            
            foreach (var item in resDict) {
                res[item.Key] = item.Value;
            }
        }
    }
}