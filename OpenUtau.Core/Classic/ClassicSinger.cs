// ============================================================================
// Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : ClassicSinger.cs
// Version      : 1.1
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/Classic/ClassicSinger.cs
// Description  : Classic UTAU singer loader with defensive portrait check and language/phoneme support detection (SupportsPhoneme/SupportsLanguage).
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenUtau.Core.Ustx;
using Serilog;
using WanaKanaNet;

namespace OpenUtau.Classic {
    public class ClassicSinger : USinger {
        public override string Id => voicebank.Id;
        public override string Name => voicebank.Name;
        public override Dictionary<string, string> LocalizedNames => voicebank.LocalizedNames;
        public override USingerType SingerType => voicebank.SingerType;
        public override string BasePath => voicebank.BasePath;
        public override string Author => voicebank.Author;
        public override string Voice => voicebank.Voice;
        public override string Location => Path.GetDirectoryName(voicebank.File);
        public override string Web => voicebank.Web;
        public override string Version => voicebank.Version;
        public override string OtherInfo => voicebank.OtherInfo;
        public override IList<string> Errors => errors;
        public override string Avatar => voicebank.Image == null ? null : Path.Combine(Location, voicebank.Image);
        public override byte[] AvatarData => LoadAvatarDataLazy();
        public override string Portrait => voicebank.Portrait == null ? null : Path.Combine(Location, voicebank.Portrait);
        public override float PortraitOpacity => voicebank.PortraitOpacity;
        public override int PortraitHeight => voicebank.PortraitHeight;
        public override string DefaultPhonemizer => voicebank.DefaultPhonemizer;
        public override string Sample => voicebank.Sample == null ? null : Path.Combine(Location, voicebank.Sample);
        public override Encoding TextFileEncoding => voicebank.TextFileEncoding;
        public override IList<USubbank> Subbanks => subbanks;
        public override IList<UOto> Otos => otos;

        Voicebank voicebank;
        List<string> errors = new List<string>();
        byte[] avatarData;
        private bool avatarLoaded;
        List<UOtoSet> otoSets = new List<UOtoSet>();
        List<USubbank> subbanks = new List<USubbank>();
        List<UOto> otos = new List<UOto>();
        Dictionary<string, UOto> otoMap = new Dictionary<string, UOto>();
        OtoWatcher otoWatcher;

        public bool? UseFilenameAsAlias { get => voicebank.UseFilenameAsAlias; set => voicebank.UseFilenameAsAlias = value; }
        public Dictionary<string, IFrqFiles> Frqs { get; set; } = new Dictionary<string, IFrqFiles>();

        public ClassicSinger(Voicebank voicebank) {
            this.voicebank = voicebank;
            found = true;
        }

        private byte[] LoadAvatarDataLazy() {
            if (avatarLoaded) {
                return avatarData;
            }
            avatarLoaded = true;
            if (Avatar != null && File.Exists(Avatar)) {
                try {
                    avatarData = File.ReadAllBytes(Avatar);
                } catch (Exception e) {
                    avatarData = null;
                    Log.Error(e, "Failed to load avatar data.");
                }
            } else {
                avatarData = null;
                Log.Debug("Avatar can't be found for {Name}", Name);
            }
            return avatarData;
        }

        public override void EnsureLoaded() {
            if (Loaded) {
                return;
            }
            Reload();
        }

        public override void Reload() {
            if (!Found) {
                return;
            }
            try {
                voicebank.Reload();
                Load();
                loaded = true;
                if (otoWatcher == null) {
                    otoWatcher = new OtoWatcher(this, Location);
                }
                OtoDirty = false;
            } catch (Exception e) {
                Log.Error(e, $"Failed to load {voicebank.File}");
            }
        }

        void Load() {
            avatarLoaded = false;
            avatarData = null;

            var newSubbanks = new List<USubbank>();
            newSubbanks.AddRange(voicebank.Subbanks
                .OrderByDescending(subbank => subbank.Prefix.Length + subbank.Suffix.Length)
                .Select(subbank => new USubbank(subbank)));
            var groups = newSubbanks.GroupBy(subbank => $"^{Regex.Escape(subbank.Prefix)}(.*){Regex.Escape(subbank.Suffix)}$")
                .Select(group => new KeyValuePair<Regex, USubbank[]>(new Regex(group.Key), group.ToArray()));

            var dummy = new USubbank[] { new USubbank(new Subbank()) };
            var newOtoSets = new List<UOtoSet>();
            var newOtos = new List<UOto>();
            var newOtoMap = new Dictionary<string, UOto>();
            var newErrors = new List<string>();
            
            foreach (var otoSet in voicebank.OtoSets) {
                var uSet = new UOtoSet(otoSet, voicebank.BasePath);
                newOtoSets.Add(uSet);
                foreach (var oto in otoSet.Otos) {
                    if (!oto.IsValid) {
                        if (!string.IsNullOrEmpty(oto.Error)) {
                            newErrors.Add(oto.Error);
                        }
                        continue;
                    }
                    UOto? uOto = null;
                    foreach (var group in groups) {
                        var m = group.Key.Match(oto.Alias);
                        if (m.Success) {
                            oto.Phonetic = m.Groups[1].Value;
                            uOto = new UOto(oto, uSet, group.Value);
                            break;
                        }
                    }
                    if (uOto == null) {
                        uOto = new UOto(oto, uSet, dummy);
                    }
                    newOtos.Add(uOto);
                    if (!newOtoMap.ContainsKey(oto.Alias)) {
                        newOtoMap.Add(oto.Alias, uOto);
                    } else {
                        //Errors.Add($"oto conflict {Otos[oto.Alias].Set}/{oto.Alias} and {otoSet.Name}/{oto.Alias}");
                    }
                }
            }

            subbanks = newSubbanks;
            otoSets = newOtoSets;
            otos = newOtos;
            otoMap = newOtoMap;
            errors = newErrors;

            Task.Run(() => {
                otoMap.Values
                    .ToList()
                    .ForEach(oto => {
                        oto.SearchTerms.Add(oto.Alias.ToLowerInvariant().Replace(" ", ""));
                        try {
                            oto.SearchTerms.Add(WanaKana.ToRomaji(oto.Alias).ToLowerInvariant().Replace(" ", ""));
                        } catch { }
                    });
            });
        }

        public override void Save() {
            try {
                otoWatcher.Paused = true;
                foreach (var oto in Otos) {
                    oto.WriteBack();
                }
                VoicebankLoader.WriteOtoSets(voicebank);
            } finally {
                otoWatcher.Paused = false;
            }
        }

        public override bool TryGetOto(string phoneme, out UOto oto) {
            if (otoMap.TryGetValue(phoneme, out oto)) {
                return true;
            }
            return false;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            oto = default;
            var subbank = subbanks.Find(subbank => string.IsNullOrEmpty(subbank.Color) && subbank.toneSet.Contains(tone));
            if (subbank != null && otoMap.TryGetValue($"{subbank.Prefix}{phoneme}{subbank.Suffix}", out oto)) {
                return true;
            }
            return TryGetOto(phoneme, out oto);
        }

        public override bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            oto = default;
            var subbank = subbanks.Find(subbank => subbank.Color == color && subbank.toneSet.Contains(tone));
            if (subbank != null && otoMap.TryGetValue($"{subbank.Prefix}{phoneme}{subbank.Suffix}", out oto)) {
                return true;
            }
            return TryGetMappedOto(phoneme, tone, out oto);
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return otoMap.Values
                .Where(oto => all || oto.SearchTerms.Exists(term => term.Contains(text)));
        }

        private HashSet<string>? detectedLanguages;

        public override bool SupportsPhoneme(string phoneme) {
            if (string.IsNullOrWhiteSpace(phoneme)) {
                return false;
            }
            phoneme = phoneme.Trim();
            if (TryGetOto(phoneme, out _)) {
                return true;
            }
            if (TryGetMappedOto(phoneme, 60, out _)) {
                return true;
            }
            if (otoMap.ContainsKey(phoneme)) {
                return true;
            }
            string clean = phoneme.Trim('[', ']');
            if (clean != phoneme && (TryGetOto(clean, out _) || TryGetMappedOto(clean, 60, out _) || otoMap.ContainsKey(clean))) {
                return true;
            }
            return false;
        }

        public override bool SupportsLanguage(string language) {
            if (string.IsNullOrEmpty(language) || language.Equals("UTAU", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            if (detectedLanguages == null) {
                detectedLanguages = DetectSupportedLanguages();
            }
            return detectedLanguages.Contains(language.ToUpperInvariant());
        }

        private HashSet<string> DetectSupportedLanguages() {
            var langs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UTAU" };

            if (!string.IsNullOrEmpty(DefaultPhonemizer)) {
                var defaultFactory = OpenUtau.Api.PhonemizerFactory.Get(DefaultPhonemizer);
                if (defaultFactory != null && !string.IsNullOrEmpty(defaultFactory.language)) {
                    langs.Add(defaultFactory.language.ToUpperInvariant());
                }
            }

            if (IsDeltaSynthSinger || 
                (!string.IsNullOrEmpty(Location) && (Location.Contains("Thai", StringComparison.OrdinalIgnoreCase) || Location.Contains("DELTA SYNTH", StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(Name) && Name.Contains("Thai", StringComparison.OrdinalIgnoreCase))) {
                langs.Add("TH");
            }

            if (otoMap == null || otoMap.Count == 0) {
                return langs;
            }

            bool hasJa = false;
            bool hasTh = false;
            bool hasEn = false;
            bool hasZh = false;
            bool hasKo = false;
            bool hasRu = false;

            foreach (var alias in otoMap.Keys) {
                if (string.IsNullOrEmpty(alias)) continue;

                if (!hasTh) {
                    if (alias.Any(c => c >= 0x0E00 && c <= 0x0E7F) ||
                        alias.Contains("k a") || alias.Contains("t a") || alias.Contains("p a") ||
                        alias.Contains("a-") || alias.Contains("_a") || alias.EndsWith(" 6") ||
                        alias.EndsWith(" @") || alias.EndsWith(" 1") || alias.EndsWith(" 3") || alias.EndsWith(" Q")) {
                        hasTh = true;
                        langs.Add("TH");
                    }
                }

                if (!hasJa) {
                    if (alias.Any(c => (c >= 0x3040 && c <= 0x309F) || (c >= 0x30A0 && c <= 0x30FF)) ||
                        alias == "a" || alias == "i" || alias == "u" || alias == "e" || alias == "o" ||
                        alias == "ka" || alias == "sa" || alias == "ta" || alias == "na" || alias == "ha" || alias == "ma" || alias == "ya" || alias == "ra" || alias == "wa" ||
                        alias.EndsWith(" a") || alias.EndsWith(" i") || alias.EndsWith(" u") || alias.EndsWith(" e") || alias.EndsWith(" o") ||
                        alias.StartsWith("- ") || alias.StartsWith("_")) {
                        hasJa = true;
                        langs.Add("JA");
                    }
                }

                if (!hasEn) {
                    if ((alias.StartsWith("[- ") && alias.EndsWith("]")) ||
                        alias.Contains("aa") || alias.Contains("ae") || alias.Contains("ah") ||
                        alias.Contains("ao") || alias.Contains("aw") || alias.Contains("ay") ||
                        alias.Contains("eh") || alias.Contains("er") || alias.Contains("ey") ||
                        alias.Contains("ih") || alias.Contains("iy") || alias.Contains("ow") ||
                        alias.Contains("oy") || alias.Contains("uh") || alias.Contains("uw")) {
                        hasEn = true;
                        langs.Add("EN");
                    }
                }

                if (!hasZh) {
                    if (alias.Any(c => c >= 0x4E00 && c <= 0x9FFF)) {
                        hasZh = true;
                        langs.Add("ZH");
                    }
                }

                if (!hasKo) {
                    if (alias.Any(c => c >= 0xAC00 && c <= 0xD7AF)) {
                        hasKo = true;
                        langs.Add("KO");
                    }
                }

                if (!hasRu) {
                    if (alias.Any(c => c >= 0x0400 && c <= 0x04FF)) {
                        hasRu = true;
                        langs.Add("RU");
                    }
                }
            }

            if (!hasJa && !hasTh && !hasEn && !hasZh && !hasKo && !hasRu) {
                langs.Add("JA");
            }

            return langs;
        }

        public override byte[] LoadPortrait() {
            return string.IsNullOrEmpty(Portrait) || !File.Exists(Portrait)
                ? null
                : File.ReadAllBytes(Portrait);
        }
    }
}
