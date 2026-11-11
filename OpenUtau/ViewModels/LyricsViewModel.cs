// ======================================================================
// 1. วัน/เดือน/ปี (Date)        : 11 พฤศจิกายน พ.ศ. 2569 / 11 November 2026 (B.E. 2569 / A.D. 2026)
// 2. ชื่อไฟล์ (File Name)       : LyricsViewModel.cs
// 3. ลำดับเวอร์ชัน (Version)    : v1.2
// 4. รายชื่อเครดิต (Credit)     : Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Contributors ]
// 5. ตำแหน่งที่จัดวาง (Path)    : OpenUtau/ViewModels/LyricsViewModel.cs
// 6. รายละเอียดการแก้ไข (Summary): เพิ่มระบบค้นหาและแทนที่คำแบบ Notepad (Find & Replace All) รองรับการเว้นวรรคหลายคำและแทนที่ยกชุดทั้งโปรเจกต์
// ======================================================================

using System;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    class LyricsViewModel : ViewModelBase {
        [Reactive] public string? Text { get; set; } = string.Empty;
        [Reactive] public int CurrentCount { get; set; }
        [Reactive] public int TotalCount { get; set; }
        [Reactive] public bool LivePreview { get; set; } = Preferences.Default.LyricLivePreview;
        [Reactive] public bool ApplySelection { get; set; } = Preferences.Default.LyricApplySelectionOnly;

        [Reactive] public string TargetWord { get; set; } = string.Empty;
        [Reactive] public string ReplacementWord { get; set; } = string.Empty;

        private UVoicePart? part;
        private UNote[]? notes;
        private UNote[]? selection;
        private string[]? startLyrics;

        public LyricsViewModel() {
            this.WhenAnyValue(x => x.LivePreview,
                x => x.Text)
                .Subscribe(t => {
                    Preview(t.Item1);
                });
            this.WhenAnyValue(x => x.ApplySelection)
                .Subscribe(a => {
                    UpdateTotalCount();
                    Preview(LivePreview);
                });
        }

        public void Start(UVoicePart part, UNote[] notes, UNote[] selection) {
            this.part = part;
            this.notes = notes;
            this.selection = selection;
            if (selection.Length < 1) {
                ApplySelection = false;
            }

            DocManager.Inst.StartUndoGroup("command.note.lyric");
            UpdateTotalCount();
            CurrentCount = TotalCount;
            Text = SplitLyrics.Join(startLyrics!);
        }

        private void Preview(bool update) {
            var notes = ApplySelection ? selection : this.notes;
            if (startLyrics == null || notes == null || part == null) {
                return;
            }
            if (DocManager.Inst.HasOpenUndoGroup) {
                DocManager.Inst.RollBackUndoGroup();
            }
            var lyrics = SplitLyrics.Split(Text);
            CurrentCount = lyrics.Count;
            if (update) {
                for (int i = 0; i < lyrics.Count && i < notes.Length; ++i) {
                    if (notes[i].lyric != lyrics[i]) {
                        DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(part, notes[i], lyrics[i]));
                    }
                }
            }
        }

        private void UpdateTotalCount() {
            if (ApplySelection) {
                TotalCount = selection?.Length ?? 0;
                startLyrics = selection?.Select(n => n.lyric).ToArray();
            } else {
                TotalCount = notes?.Length ?? 0;
                startLyrics = notes?.Select(n => n.lyric).ToArray();
            }
        }

        public void Reset() {
            if (startLyrics == null) {
                return;
            }
            if (DocManager.Inst.HasOpenUndoGroup) {
                DocManager.Inst.RollBackUndoGroup();
            }
            Text = SplitLyrics.Join(startLyrics);
        }

        public void Cancel() {
            if (DocManager.Inst.HasOpenUndoGroup) {
                DocManager.Inst.RollBackUndoGroup();
                DocManager.Inst.EndUndoGroup();
            }
            Preferences.Default.LyricLivePreview = LivePreview;
            Preferences.Default.LyricApplySelectionOnly = ApplySelection;
            Preferences.Save();
        }

        public void Finish() {
            Preview(true);
            if (DocManager.Inst.HasOpenUndoGroup) {
                DocManager.Inst.EndUndoGroup();
            }
            Preferences.Default.LyricLivePreview = LivePreview;
            Preferences.Default.LyricApplySelectionOnly = ApplySelection;
            Preferences.Save();
        }

        /// <summary>
        /// Notepad-style Replace All: Replaces all occurrences of TargetWord with ReplacementWord
        /// in the editor text and across all tracks/parts in the entire project.
        /// </summary>
        public void ReplaceAllWords() {
            if (string.IsNullOrEmpty(TargetWord)) {
                return;
            }

            var target = TargetWord;
            var replacement = ReplacementWord ?? string.Empty;

            // 1. Replace All in the current multiline editor (Notepad exact string replacement)
            if (!string.IsNullOrEmpty(Text) && Text.Contains(target)) {
                Text = Text.Replace(target, replacement);
            }

            // 2. Project-wide replacement across all UVoiceParts in the project
            var targetTokens = SplitLyrics.Split(target);
            var replTokens = SplitLyrics.Split(replacement);

            var project = DocManager.Inst.Project;
            if (project?.parts != null) {
                DocManager.Inst.StartUndoGroup("command.note.lyric.replaceall");
                try {
                    foreach (var p in project.parts) {
                        if (p is UVoicePart voicePart && voicePart.notes != null && voicePart.notes.Count > 0) {
                            var partNotes = voicePart.notes.OrderBy(n => n.position).ToArray();
                            if (targetTokens.Count <= 1) {
                                // Single-word replacement
                                foreach (var note in partNotes) {
                                    if (note.lyric == target) {
                                        DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(voicePart, note, replacement));
                                    }
                                }
                            } else {
                                // Multi-word sequence replacement (supporting spaces e.g. "a a" -> "b b")
                                for (int i = 0; i <= partNotes.Length - targetTokens.Count; i++) {
                                    bool matches = true;
                                    for (int j = 0; j < targetTokens.Count; j++) {
                                        if (partNotes[i + j].lyric != targetTokens[j]) {
                                            matches = false;
                                            break;
                                        }
                                    }
                                    if (matches) {
                                        for (int j = 0; j < targetTokens.Count; j++) {
                                            string newLyr = j < replTokens.Count ? replTokens[j] : string.Empty;
                                            if (partNotes[i + j].lyric != newLyr) {
                                                DocManager.Inst.ExecuteCmd(new ChangeNoteLyricCommand(voicePart, partNotes[i + j], newLyr));
                                            }
                                        }
                                        i += targetTokens.Count - 1;
                                    }
                                }
                            }
                        }
                    }
                } finally {
                    DocManager.Inst.EndUndoGroup();
                }
            }

            // 3. Trigger immediate live preview update
            Preview(true);
        }
    }
}
