// ============================================================================
// Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : PhonemizerFactory.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Api/PhonemizerFactory.cs
// Description  : เพิ่มการกรอง Phonemizer ใน EnumerateForSinger ให้แสดงเฉพาะภาษาและหน่วยเสียงที่คลังเสียงรองรับจริง (IsPhonemizerSupportedBySinger)
// ============================================================================
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Api {
    public class PhonemizerFactory {
        public const string DiffSingerLanguage = "DiffSinger";
        public Type type;
        public string name;
        public string tag;
        public string author;
        public string language;

        public Phonemizer Create() {
            var phonemizer = Activator.CreateInstance(type) as Phonemizer;
            phonemizer.Name = name;
            phonemizer.Tag = tag;
            phonemizer.Language = language;
            return phonemizer;
        }

        public override string ToString() => string.IsNullOrEmpty(author)
            ? $"[{tag}] {name}"
            : $"[{tag}] {name} (Contributed by {author})";

        private static Dictionary<Type, PhonemizerFactory> factories = new Dictionary<Type, PhonemizerFactory>();
        private static PhonemizerFactory[] orderedFactories = [];
        public static PhonemizerFactory Get(Type type) {
            if (!factories.TryGetValue(type, out var factory)) {
                var attr = type.GetCustomAttribute<PhonemizerAttribute>();
                if (attr == null || string.IsNullOrEmpty(attr.Name) || string.IsNullOrEmpty(attr.Tag)) {
                    return null;
                }
                factory = new PhonemizerFactory() {
                    type = type,
                    name = attr.Name,
                    tag = attr.Tag,
                    author = attr.Author,
                    language = attr.Language,
                };
                factories[type] = factory;
            }
            return factory;
        }

        public static PhonemizerFactory? Get(string typeFullName) {
            foreach (var factory in factories.Values) {
                if (factory.type.FullName == typeFullName) {
                    return factory;
                }
            }
            return null;
        }

        public static void BuildList() {
            orderedFactories = factories.Values.OrderBy(f => f.tag).ToArray();
        }

        public static PhonemizerFactory[] GetAll() => orderedFactories;

        public static bool IsDiffSingerPhonemizer(PhonemizerFactory factory) {
            return string.Equals(factory.language, DiffSingerLanguage, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPhonemizerSupportedBySinger(PhonemizerFactory factory, USinger singer) {
            if (factory == null || singer == null) return false;
            // Universal Default Phonemizer is always supported
            if (factory.type?.FullName == "OpenUtau.Core.DefaultPhonemizer" ||
                string.Equals(factory.name, "Default UTAU Phonemizer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(factory.name, "Default Phonemizer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(factory.tag, "Default UTAU", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(factory.tag, "---", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            // Universal DiffSinger Phonemizer is always supported on DiffSinger
            if (singer.SingerType == USingerType.DiffSinger &&
                (string.Equals(factory.name, "DiffSinger Phonemizer", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(factory.type?.Name, "DiffSingerPhonemizer", StringComparison.OrdinalIgnoreCase))) {
                return true;
            }
            // Singer default phonemizer is always supported
            if (!string.IsNullOrEmpty(singer.DefaultPhonemizer) &&
                (string.Equals(singer.DefaultPhonemizer, factory.name, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(singer.DefaultPhonemizer, factory.type?.FullName, StringComparison.OrdinalIgnoreCase))) {
                return true;
            }

            string lang = factory.language ?? "";
            string tag = factory.tag ?? "";

            // DiffSinger voicebank check
            if (singer.SingerType == USingerType.DiffSinger) {
                var tagParts = tag.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tagParts.Length > 0) {
                    var cleanTag = tagParts[0].Replace("+", "").Replace("/", " ");
                    foreach (var part in cleanTag.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
                        if (singer.SupportsLanguage(part)) {
                            return true;
                        }
                    }
                    string dictName = $"dsdict-{tagParts[0].ToLowerInvariant()}.yaml";
                    if (!string.IsNullOrEmpty(singer.Location) && File.Exists(Path.Combine(singer.Location, dictName))) {
                        return true;
                    }
                }
                return !string.IsNullOrEmpty(lang) && !string.Equals(lang, "UTAU", StringComparison.OrdinalIgnoreCase) && singer.SupportsLanguage(lang);
            }

            // Classic / ENUNU / other singers check
            if (!string.IsNullOrEmpty(lang) && !string.Equals(lang, "UTAU", StringComparison.OrdinalIgnoreCase) && singer.SupportsLanguage(lang)) {
                return true;
            }
            var partsClassic = tag.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partsClassic.Length > 0) {
                var cleanTag = partsClassic[0].Replace("+", "").Replace("/", " ");
                foreach (var part in cleanTag.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
                    if (singer.SupportsLanguage(part)) {
                        return true;
                    }
                }
            }

            return false;
        }

        public static IEnumerable<PhonemizerFactory> EnumerateForSinger(USinger? singer) {
            var all = GetAll();
            if (singer == null || !singer.Found) {
                return all.Where(factory => !IsDiffSingerPhonemizer(factory));
            }
            if (!singer.Loaded) {
                singer.EnsureLoaded();
            }
            if (singer.SingerType == USingerType.DiffSinger) {
                var dsFactories = all.Where(IsDiffSingerPhonemizer);
                var supported = dsFactories.Where(factory => IsPhonemizerSupportedBySinger(factory, singer)).ToList();
                return supported.Count > 0 ? supported : dsFactories;
            }
            var classicFactories = all.Where(factory => !IsDiffSingerPhonemizer(factory));
            var supportedClassic = classicFactories.Where(factory => IsPhonemizerSupportedBySinger(factory, singer)).ToList();
            return supportedClassic.Count > 0 ? supportedClassic : classicFactories;
        }
    }
}
