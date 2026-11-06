// ============================================================================
// Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : DiffSingerSinger.cs
// Version      : 1.1
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau.Core/DiffSinger/DiffSingerSinger.cs
// Description  : DiffSinger singer loader with defensive portrait check, suggestion provider, and language/phoneme support detection (TryGetOto/GetSuggestions/SupportsPhoneme/SupportsLanguage).
// ============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using K4os.Hash.xxHash;
using OpenUtau.Classic;
using OpenUtau.Core.SingerHub;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;
using Microsoft.ML.OnnxRuntime;

namespace OpenUtau.Core.DiffSinger {
    class DiffSingerSinger : USinger {
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
        public override string Sample => voicebank.Sample == null ? null : Path.Combine(Location, voicebank.Sample);
        public override string DefaultPhonemizer => voicebank.DefaultPhonemizer;
        public override Encoding TextFileEncoding => voicebank.TextFileEncoding;
        public override IList<USubbank> Subbanks => subbanks;
        public override IList<UOto> Otos => otos;

        Voicebank voicebank;
        List<string> errors = new List<string>();
        Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        public byte[] avatarData;
        private bool avatarLoaded;
        List<USubbank> subbanks = new List<USubbank>();
        List<UOto> otos = new List<UOto>();
        Dictionary<string, UOto> otoMap = new Dictionary<string, UOto>();
        public List<string> phonemes = new List<string>();
        Dictionary<string, int> phonemeTokens;
        public Dictionary<string, int> languageIds = new Dictionary<string, int>();
        internal HashSet<string> unvoicedPhonemes = new HashSet<string>(StringComparer.Ordinal);
        public DsConfig dsConfig;
        public ulong acousticHash;
        public InferenceSession acousticSession = null;
        public DsVocoder vocoder = null;
        public DsPitch pitchPredictor = null;
        public DiffSingerSpeakerEmbedManager speakerEmbedManager = null;
        public DsVariance variancePredictor = null;
        public bool HasPitchPredictor => File.Exists(Path.Join(Location, "dspitch", "dsconfig.yaml"));
        public bool HasVariancePredictor => File.Exists(Path.Join(Location,"dsvariance", "dsconfig.yaml"));

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
                Log.Debug("Avatar not found for {Name}", Name);
            }
            return avatarData;
        }

        public DiffSingerSinger(Voicebank voicebank) {
            this.voicebank = voicebank;

            subbanks.Clear();
            subbanks.AddRange(voicebank.Subbanks
                .Select(subbank => new USubbank(subbank)));

            //Load diffsinger config of a voicebank
            string configPath = Path.Combine(Location, "dsconfig.yaml");
            bool dsConfigLoaded = false;
            if(configPath != null && File.Exists(configPath)){
                try {
                    dsConfig = Core.Yaml.DefaultDeserializer.Deserialize<DsConfig>(
                        File.ReadAllText(configPath, Encoding.UTF8));
                    dsConfigLoaded = true;
                } catch (Exception e) {
                    Log.Warning(e, $"Failed to load dsconfig.yaml for {Name} from {configPath}");
                    errors.Add($"Failed to load dsconfig.yaml: {e.Message}");
                    dsConfig = new DsConfig();
                }
            } else {
                Log.Warning($"dsconfig.yaml not found for {Name} at {configPath}");
                errors.Add($"dsconfig.yaml not found at {configPath}");
                dsConfig = new DsConfig();
            }

            if(dsConfigLoaded) {
                //Load phoneme tokens for acoustic model (render-time tokenization)
                string phonemesPath = Path.Combine(Location, dsConfig.phonemes);
                if(phonemesPath != null && File.Exists(phonemesPath)){
                    try {
                        phonemeTokens = DiffSingerUtils.LoadPhonemes(phonemesPath);
                        phonemes = phonemeTokens.Keys.ToList();
                    } catch (Exception e){
                        Log.Error(e, $"Failed to load phoneme tokens for {Name} from {phonemesPath}");
                        errors.Add($"Failed to load phoneme tokens: {e.Message}");
                        phonemeTokens = new Dictionary<string, int>();
                    }
                } else {
                    Log.Error($"phonemes file not found for {Name} at {phonemesPath}");
                    errors.Add($"Phonemes file not found at {phonemesPath}");
                    phonemeTokens = new Dictionary<string, int>();
                }

                //Load language Id if needed
                if(dsConfig.use_lang_id){
                    if(dsConfig.languages == null){
                        Log.Error("\"languages\" field is not specified in dsconfig.yaml");
                        errors.Add("\"languages\" field is not specified in dsconfig.yaml but use_lang_id is true");
                    } else {
                        var langIdPath = Path.Join(Location, dsConfig.languages);
                        try {
                            languageIds = DiffSingerUtils.LoadLanguageIds(langIdPath);
                        } catch (Exception e) {
                            Log.Error(e, $"failed to load language id from {langIdPath}");
                            errors.Add($"Failed to load language IDs: {e.Message}");
                        }
                    }
                }

                LoadUnvoicedPhonemes();
            }

            var dummyOtoSet = new UOtoSet(new OtoSet(), Location);
            foreach (var phone in phonemes) {
                var uOto = UOto.OfDummy(phone);
                if (!otoMap.ContainsKey(uOto.Alias)) {
                    otos.Add(uOto);
                    otoMap.Add(uOto.Alias, uOto);
                } else {
                    //Errors.Add($"oto conflict {Otos[oto.Alias].Set}/{oto.Alias} and {otoSet.Name}/{oto.Alias}");
                }
            }

            found = true;
            loaded = true;
        }

        public override bool TryGetOto(string phoneme, out UOto oto) {
            // We always return true here just not to let OTO get in our way.
            // Phonemizer and acoustic model work independently and both can report missing phonemes by their own,
            // so do other submodules.
            oto = UOto.OfDummy(phoneme);
            return true;
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return table.Keys
                .Where(key => all || key.Contains(text))
                .Select(key => UOto.OfDummy(key));
        }

        public override byte[] LoadPortrait() {
            return string.IsNullOrEmpty(Portrait) || !File.Exists(Portrait)
                ? null
                : File.ReadAllBytes(Portrait);
        }

        void LoadUnvoicedPhonemes() {
            unvoicedPhonemes.Clear();
            string relativePath = string.IsNullOrWhiteSpace(dsConfig.unvoiced_phonemes)
                ? "dsunvoiced.yaml"
                : dsConfig.unvoiced_phonemes.Trim();
            string path = Path.Combine(Location, relativePath);
            if (File.Exists(path)) {
                try {
                    LoadUnvoicedPhonemesFromYaml(File.ReadAllText(path, Encoding.UTF8), path);
                } catch (Exception e) {
                    Log.Error(e, "Failed to load unvoiced phoneme list from {Path}", path);
                    errors.Add($"Failed to load unvoiced phoneme list: {e.Message}");
                }
            }
            if (unvoicedPhonemes.Count == 0 && SingerHubClient.IsLunaiSinger(Location)) {
                if (LunaiDsUnvoicedDefaults.TryLoadPhonemes(unvoicedPhonemes)) {
                    Log.Information(
                        "Loaded {Count} bundled Lunai unvoiced phonemes (fallback for {File})",
                        unvoicedPhonemes.Count, relativePath);
                }
            } else if (unvoicedPhonemes.Count == 0 && !File.Exists(path)) {
                Log.Debug("Unvoiced phoneme list not found at {Path}", path);
            }
        }

        void LoadUnvoicedPhonemesFromYaml(string yamlText, string sourceLabel) {
            var config = Core.Yaml.DefaultDeserializer.Deserialize<DsUnvoicedConfig>(yamlText);
            if (config?.phonemes == null) {
                return;
            }
            foreach (var phoneme in config.phonemes) {
                if (string.IsNullOrWhiteSpace(phoneme)) {
                    continue;
                }
                unvoicedPhonemes.Add(phoneme.Trim());
            }
            Log.Information("Loaded {Count} unvoiced phonemes from {Path}", unvoicedPhonemes.Count, sourceLabel);
        }

        public InferenceSession getAcousticSession() {
            if (acousticSession is null) {
                var acousticPath = Path.Combine(Location, dsConfig.acoustic);
                var acousticBytes = File.ReadAllBytes(acousticPath);
                acousticHash = XXH64.DigestOf(acousticBytes);
                acousticSession = Onnx.getInferenceSession(acousticBytes, OnnxRunnerChoice.Default);
            }
            return acousticSession;
        }

        public DsVocoder getVocoder() {
            if(vocoder is null) {
                if(File.Exists(Path.Join(Location, "dsvocoder", "vocoder.yaml"))) {
                    vocoder = new DsVocoder(Path.Join(Location, "dsvocoder"));
                    return vocoder;
                }
                string vocoderPath = Path.Combine(PathManager.Inst.DependencyPath, dsConfig.vocoder);
                if (!File.Exists(Path.Combine(vocoderPath, "vocoder.yaml"))) {
                    const string defaultPkg = "pc-nsf-hifigan";
                    string installedPath = PackageManager.Inst.GetInstalledPath(defaultPkg) ?? "";
                    if (!string.IsNullOrEmpty(installedPath) && File.Exists(Path.Combine(installedPath, "vocoder.yaml"))) {
                        vocoderPath = installedPath;
                    }
                }
                vocoder = new DsVocoder(vocoderPath);
            }
            return vocoder;
        }

        public DsPitch? getPitchPredictor(){
            if(pitchPredictor is null) {
                if(HasPitchPredictor){
                    pitchPredictor = new DsPitch(Path.Join(Location, "dspitch"));
                }
            }
            return pitchPredictor;
        }
       
        public DiffSingerSpeakerEmbedManager getSpeakerEmbedManager(){
            if(speakerEmbedManager is null) {
                speakerEmbedManager = new DiffSingerSpeakerEmbedManager(dsConfig, Location);
            }
            return speakerEmbedManager;
        }

        public DsVariance? getVariancePredictor(){
            if(variancePredictor is null) {
                if(HasVariancePredictor){
                    variancePredictor = new DsVariance(Path.Join(Location, "dsvariance"));
                }
            }
            return variancePredictor;
        }

        public override bool TryGetOto(string phoneme, out UOto oto) {
            if (otoMap.TryGetValue(phoneme, out oto)) {
                return true;
            }
            if (phoneme.Contains('/')) {
                var sub = phoneme.Substring(phoneme.IndexOf('/') + 1);
                if (otoMap.TryGetValue(sub, out oto)) {
                    return true;
                }
            }
            oto = default;
            return false;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            return TryGetOto(phoneme, out oto);
        }

        public override bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            return TryGetOto(phoneme, out oto);
        }

        public override IEnumerable<UOto> GetSuggestions(string text) {
            if (text != null) {
                text = text.ToLowerInvariant().Replace(" ", "");
            }
            bool all = string.IsNullOrEmpty(text);
            return otoMap.Values
                .Where(oto => all || oto.Alias.ToLowerInvariant().Replace(" ", "").Contains(text));
        }

        public override bool SupportsPhoneme(string phoneme) {
            if (string.IsNullOrWhiteSpace(phoneme)) {
                return false;
            }
            phoneme = phoneme.Trim();
            if (TryGetOto(phoneme, out _)) {
                return true;
            }
            string clean = phoneme.Trim('[', ']');
            if (clean != phoneme && TryGetOto(clean, out _)) {
                return true;
            }
            return false;
        }

        private HashSet<string>? detectedLanguages;

        public override bool SupportsLanguage(string language) {
            if (string.IsNullOrEmpty(language) || language.Equals("DiffSinger", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            if (detectedLanguages == null) {
                detectedLanguages = DetectSupportedLanguages();
            }
            return detectedLanguages.Contains(language.ToUpperInvariant());
        }

        private HashSet<string> DetectSupportedLanguages() {
            var langs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DIFFSINGER" };

            // 1. Check dictionary files in voicebank folder (e.g. dsdict-th.yaml, dsdict-ja.yaml)
            if (!string.IsNullOrEmpty(Location) && Directory.Exists(Location)) {
                try {
                    var dictFiles = Directory.GetFiles(Location, "dsdict-*.yaml");
                    foreach (var file in dictFiles) {
                        var filename = Path.GetFileNameWithoutExtension(file); // e.g. "dsdict-th"
                        var langCode = filename.Replace("dsdict-", "").ToUpperInvariant(); // "TH"
                        langs.Add(langCode);
                    }
                } catch { }
            }

            // 2. Check language IDs in dsconfig.yaml
            if (languageIds != null && languageIds.Count > 0) {
                foreach (var langId in languageIds.Keys) {
                    langs.Add(langId.ToUpperInvariant());
                }
            }

            // 3. Check phoneme tokens for language-specific prefixes (e.g. "th/a", "ja/a", "zh/a")
            if (phonemes != null && phonemes.Count > 0) {
                foreach (var ph in phonemes) {
                    if (ph.Contains('/')) {
                        var prefix = ph.Substring(0, ph.IndexOf('/')).ToUpperInvariant();
                        if (prefix.Length <= 4) {
                            langs.Add(prefix);
                        }
                    }
                }
            }

            // 4. Check DELTA SYNTH / Thai indicators
            if (IsDeltaSynthSinger || 
                (!string.IsNullOrEmpty(Location) && (Location.Contains("Thai", StringComparison.OrdinalIgnoreCase) || Location.Contains("DELTA SYNTH", StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(Name) && Name.Contains("Thai", StringComparison.OrdinalIgnoreCase))) {
                langs.Add("TH");
            }

            return langs;
        }

        public int PhonemeTokenize(string phoneme){
            if(phonemeTokens == null || phonemeTokens.Count == 0){
                throw new Exception(
                    $"Phoneme vocabulary is not loaded for singer \"{Name}\". " +
                    "Please check that dsconfig.yaml and the phonemes file are valid.");
            }
            bool success = phonemeTokens.TryGetValue(phoneme, out int token);
            if(!success){
                throw new Exception($"Phoneme \"{phoneme}\" isn't supported by acoustic model. Please check {Path.Combine(Location, dsConfig.phonemes)}");
            }
            return token;
        }

        public override void FreeMemory(){
            Log.Information($"Freeing memory for singer {Id}");
            if(acousticSession != null) {
                lock(acousticSession) {
                    acousticSession?.Dispose();
                }
                acousticSession = null;
            }
            if(vocoder != null) {
                lock(vocoder) {
                    vocoder?.Dispose();
                }
                vocoder = null;
            }
            if(pitchPredictor != null) {
                lock(pitchPredictor) {
                    pitchPredictor?.Dispose();
                }
                pitchPredictor = null;
            }
            if(variancePredictor != null){
                lock(variancePredictor) {
                    variancePredictor?.Dispose();
                }
                variancePredictor = null;
            }
        }
    }
}
