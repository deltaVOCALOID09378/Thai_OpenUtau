// ============================================================================
// Date         : 06/11/2026 (B.E. 2569 / A.D. 2026)
// File Name    : PhoneticAssistantViewModel.cs
// Version      : 1.1
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors ]
// File Location: OpenUtau/ViewModels/PhoneticAssistantViewModel.cs
// Description  : กรองการแสดงผลหน่วยเสียงของภาษาใดๆ ให้แสดงเฉพาะส่วนที่คลังเสียงรองรับจริง (SupportsPhoneme)
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenUtau.Core;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class PhoneticAssistantViewModel : ViewModelBase {
        public class G2pOption {
            public string name;
            public Type klass;
            public G2pOption(Type klass) {
                name = klass.Name;
                this.klass = klass;
            }
            public override string ToString() => name;
        }
        public List<G2pOption> G2ps => g2ps;

        [Reactive] public G2pOption? G2p { get; set; }
        [Reactive] public string? Grapheme { get; set; }
        [Reactive] public string Phonemes { get; set; }
        [Reactive] public string ActiveSingerInfo { get; set; }
        [Reactive] public bool HasActiveSinger { get; set; }

        private readonly List<G2pOption> g2ps = new List<G2pOption>() {
            new G2pOption(typeof(ArpabetG2p)),
            new G2pOption(typeof(ArpabetPlusG2p)),
            new G2pOption(typeof(BrapaG2p)),
            new G2pOption(typeof(FrenchG2p)),
            new G2pOption(typeof(FrenchMillefeuilleG2p)),
            new G2pOption(typeof(GermanG2p)),
            new G2pOption(typeof(GermanMarzipanG2p)),
            new G2pOption(typeof(ItalianG2p)),
            new G2pOption(typeof(PortugueseG2p)),
            new G2pOption(typeof(RussianG2p)),
            new G2pOption(typeof(SpanishG2p)),
            new G2pOption(typeof(ThaiG2p)),
            new G2pOption(typeof(KoreanG2p)),
            new G2pOption(typeof(FilipinoG2p)),
            new G2pOption(typeof(UkrainianG2p)),
        };

        private Api.G2pPack? g2p;

        public PhoneticAssistantViewModel() {
            G2p = g2ps.FirstOrDefault(x=>x.name == Preferences.Default.PhoneticAssistant) ?? g2ps.First();
            Grapheme = string.Empty;
            Phonemes = string.Empty;
            ActiveSingerInfo = string.Empty;
            HasActiveSinger = false;

            UpdateSingerInfo();

            this.WhenAnyValue(x => x.G2p)
                .Subscribe(option => {
                    g2p = null;
                    if (option != null) {
                        g2p = Activator.CreateInstance(option.klass) as Api.G2pPack;
                        Preferences.Default.PhoneticAssistant = option.name;
                        Preferences.Save();
                        Refresh();
                    }
                });
            this.WhenAnyValue(x => x.Grapheme)
                .Subscribe(_ => Refresh());
        }

        private USinger? GetActiveSinger() {
            var project = DocManager.Inst.Project;
            if (project?.tracks == null || project.tracks.Count == 0) return null;
            return project.tracks.FirstOrDefault(t => t.Singer != null && t.Singer.Found && t.Singer.Loaded)?.Singer;
        }

        private void UpdateSingerInfo() {
            var activeSinger = GetActiveSinger();
            if (activeSinger != null && activeSinger.Found && activeSinger.Loaded) {
                HasActiveSinger = true;
                ActiveSingerInfo = $"คลังเสียง: {activeSinger.LocalizedName} (แสดงเฉพาะหน่วยเสียงที่รองรับ)";
            } else {
                HasActiveSinger = false;
                ActiveSingerInfo = string.Empty;
            }
        }

        private void Refresh() {
            UpdateSingerInfo();
            if (Grapheme == null || g2p == null) {
                Phonemes = string.Empty;
                return;
            }
            string[] phonemes = g2p.Query(Grapheme);
            if (phonemes == null || phonemes.Length == 0) {
                Phonemes = string.Empty;
                return;
            }

            var activeSinger = GetActiveSinger();
            if (activeSinger != null && activeSinger.Found && activeSinger.Loaded) {
                // แสดงหน่วยเสียงของภาษาใดๆ เพียงส่วนที่คลังเสียงสามารถรองรับเท่านั้น
                var supported = phonemes.Where(p => activeSinger.SupportsPhoneme(p)).ToArray();
                if (supported.Length > 0) {
                    Phonemes = string.Join(' ', supported);
                } else {
                    Phonemes = $"(คลังเสียง {activeSinger.LocalizedName} ไม่รองรับหน่วยเสียงนี้)";
                }
            } else {
                Phonemes = string.Join(' ', phonemes);
            }
        }
    }
}
