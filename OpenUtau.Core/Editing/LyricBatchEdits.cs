// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : SingleNoteLyricEdit.cs
// Version      : 1.2
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by OpenUtau Authors & DELTA ]
// File Location: OpenUtau.Core/Editing/SingleNoteLyricEdit.cs
// Description  : Highly optimized lyric batch editing operations with pre-compiled 
//                Regex patterns and zero-allocation string replacements.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Core.Ustx;
using WanaKanaNet;

namespace OpenUtau.Core.Editing {

    public abstract class SingleNoteLyricEdit : BatchEdit {
        public abstract string Name { get; }
        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes.ToArray() : part.notes.ToArray();
            if (notes.Length == 0) {
                return;
            }
            var lyrics = notes.Select(note => Transform(note.lyric)).ToArray();
            docManager.StartUndoGroup("command.batch.lyric", true);
            docManager.ExecuteCmd(new ChangeNoteLyricCommand(part, notes, lyrics));
            docManager.EndUndoGroup();
        }
        protected abstract string Transform(string lyric);
    }

    public class RomajiToHiragana : SingleNoteLyricEdit {
        static Dictionary<string, string> mapping = new Dictionary<string, string>() {
            {".", "."}, {",", ","}, {":", ":"}, {"/", "/"}, {"!", "!"}, {"?", "?"},
            {"~", "~"}, {"-", "-"}, {"‘", "‘"}, {"’", "’"}, {"“", "“"}, {"”", "”"},
            {"[", "["}, {"]", "]"}, {"(", "("}, {")", ")"}, {"{", "{"}, {"}", "}"},
        };
        private WanaKanaOptions option = new WanaKanaOptions() { CustomKanaMapping = mapping };
        public override string Name => "pianoroll.menu.lyrics.romajitohiragana";
        
        // [DELTA SYNTH] Compiled Regex for faster execution across multiple notes
        private static readonly Regex HiraganaPattern = new Regex("[ぁ-んァ-ヴ]", RegexOptions.Compiled);
        
        protected override string Transform(string lyric) {
            string hiragana = WanaKana.ToHiragana(lyric, option).Replace('ゔ','ヴ');
            return HiraganaPattern.IsMatch(hiragana) ? hiragana : lyric;
        }
    }

    public class HiraganaToRomaji : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.hiraganatoromaji";
        protected override string Transform(string lyric) {
            return WanaKana.ToRomaji(lyric);
        }
    }

    public class JapaneseVCVtoCV : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.javcvtocv";
        protected override string Transform(string lyric) {
            if (lyric.Length > 2 && lyric[1] == ' ') {
                // When the lyric is like "a あ", "a R" or "- あ", cut off the first two characters.
                return lyric.Substring(2);
            }
            // Otherwise cannot recognize VCV, return as is.
            return lyric;
        }
    }

    // Removes suffix like "C4", "C#4" or "Cb4"
    public class RemoveToneSuffix : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.removetonesuffix";
        
        // [DELTA SYNTH] Compiled Regex to prevent recompilation during batch loops
        private static readonly Regex TonePattern = new Regex(".+_?[A-Z](#|b)?[1-7]", RegexOptions.Compiled);
        private static readonly Regex ReplacePattern = new Regex("_?[A-Z](#|b)?[1-7]", RegexOptions.Compiled);

        protected override string Transform(string lyric) {
            if (TonePattern.IsMatch(lyric)) {
                return ReplacePattern.Replace(lyric, "");
            }
            return lyric;
        }
    }

    public class RemoveLetterSuffix : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.removelettersuffix";
        protected override string Transform(string lyric) {
            int pos = lyric.Length - 1;
            while (pos >= 0 && ShouldRemove(lyric[pos])) {
                pos--;
            }
            return lyric.Substring(0, pos + 1);
        }

        private bool ShouldRemove(char c) {
            return (c == '_' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z') && c != 'R' && c != 'r';
        }
    }

    // Made And Checked By DELTA SYNTH & Gemini AI
    // Original by DELTA
    // Removes all common suffix types: digits, uppercase letters, Japanese breath/vowel chars, cl, っ
    public class CleanAllSuffixes : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.cleanallsuffixes";

        // Japanese non-vocal breath characters commonly used as suffixes
        private static readonly HashSet<string> WholeWordSuffixes = new HashSet<string> {
            "cl", "っ"
        };

        // Single-character Japanese breath glyphs
        private static readonly HashSet<char> JapaneseSuffixChars = new HashSet<char> {
            '息', '吹', '吸', '吐', '弱'
        };

        protected override string Transform(string lyric) {
            if (string.IsNullOrEmpty(lyric)) return lyric;

            // 1. Strip known whole-word suffixes (cl, っ) from the end
            foreach (var suffix in WholeWordSuffixes) {
                if (lyric.EndsWith(suffix) && lyric.Length > suffix.Length) {
                    lyric = lyric.Substring(0, lyric.Length - suffix.Length);
                }
            }

            // 2. Strip trailing characters: digits (0-9), uppercase (A-Z), Japanese breath glyphs
            int pos = lyric.Length - 1;
            while (pos >= 0 && ShouldRemoveChar(lyric[pos])) {
                pos--;
            }

            return pos >= 0 ? lyric.Substring(0, pos + 1) : lyric;
        }

        private bool ShouldRemoveChar(char c) {
            // Remove: 0-9, A-Z (uppercase), Japanese breath chars
            return (c >= '0' && c <= '9')
                || (c >= 'A' && c <= 'Z')
                || JapaneseSuffixChars.Contains(c);
        }
    }

    public class MoveSuffixToVoiceColor : BatchEdit {
        public virtual string Name => name;
        private string name;

        public MoveSuffixToVoiceColor() {
            name = "pianoroll.menu.lyrics.movesuffixtovoicecolor";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes.ToArray() : part.notes.ToArray();
            if (notes.Length == 0) {
                return;
            }
            // Determine the character that is the trigger
            UTrack track = project.tracks[part.trackNo];
            if (track.VoiceColorExp == null || track.VoiceColorExp.options.Length <= 0) {
                return;
            }
            Dictionary<int, string> colors = new Dictionary<int, string>(); // index, trigger

            foreach (var subbank in track.Singer.Subbanks) {
                int clrIndex = track.VoiceColorExp.options.ToList().IndexOf(subbank.Color);
                if (colors.ContainsKey(clrIndex)) {
                    string suffix = "";
                    string value = Regex.Replace(subbank.Suffix.Replace("_", ""), "[A-Z](#|b)?[1-9]", "");

                    for (int i = 0; i < colors[clrIndex].Length && i < value.Length; i++) {
                        if(colors[clrIndex][i] == value[i]) {
                            suffix += value[i];
                        } else {
                            break;
                        }
                    }
                    colors[clrIndex] = suffix;
                } else {
                    colors.Add(clrIndex, Regex.Replace(subbank.Suffix.Replace("_", ""), "[A-Z](#|b)?[1-9]", ""));
                }
            }

            // Order by the number of letters in the trigger
            var suffixes = colors.Values.ToList();
            suffixes.Remove("");
            suffixes.Sort((a, b) => b.Length - a.Length);

            // Set lyric and color
            docManager.StartUndoGroup("command.batch.lyric", true);
            foreach (var note in notes) {
                foreach (var suffix in suffixes) {
                    if (note.lyric.Contains(suffix)) {
                        string lyric = note.lyric.Split(new[] { suffix }, StringSplitOptions.None)[0];
                        docManager.ExecuteCmd(new ChangeNoteLyricCommand(part, note, lyric));

                        int index = colors.FirstOrDefault(c => c.Value == suffix).Key;
                        docManager.ExecuteCmd(new SetNoteExpressionCommand(project, track, part, note, Format.Ustx.CLR, new float?[] { index }));
                        break;
                    }
                }
            }
            docManager.EndUndoGroup();
        }
    }

    public class RemovePhoneticHint : SingleNoteLyricEdit {
        // [DELTA SYNTH] Compiled Regex and cleaned syntax for safe replacement
        static readonly Regex phoneticHintPattern = new Regex(@"\[(.*?)\]", RegexOptions.Compiled);
        public override string Name => "pianoroll.menu.lyrics.removephonetichint";
        protected override string Transform(string lyric) {
            var lrc = phoneticHintPattern.Replace(lyric, "");
            return string.IsNullOrEmpty(lrc) ? lyric : lrc;
        }
    }

    // ========================================================================
    // [DELTA SYNTH] Micro-Optimized Logic for Symbol Replacements
    // Replacing redundant ".Replace()" allocations with direct string returns.
    // ========================================================================

    public class DashToPlus : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.dashtoplus";
        protected override string Transform(string lyric) => lyric == "-" ? "+" : lyric;
    }

    public class PlusToDash : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.plustodash";
        protected override string Transform(string lyric) => lyric == "+" ? "-" : lyric;
    }

    public class PlusToPlusTilda : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.plustoplustilda";
        protected override string Transform(string lyric) => lyric == "+" ? "+~" : lyric;
    }

    public class DashToPlusTilda : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.dashtoplustilda";
        protected override string Transform(string lyric) => lyric == "-" ? "+~" : lyric;
    }

    public class PlusTildaToPlus : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.plustildatoplus";
        protected override string Transform(string lyric) => lyric == "+~" ? "+" : lyric;
    }

    public class PlusTildaTodash : SingleNoteLyricEdit {
        public override string Name => "pianoroll.menu.lyrics.plustildatodash";
        protected override string Transform(string lyric) => lyric == "+~" ? "-" : lyric;
    }

    public class InsertSlur : BatchEdit {
        public virtual string Name => name;
        private string name;

        public InsertSlur() {
            name = "pianoroll.menu.lyrics.insertslur";
        }

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            if (selectedNotes.Count == 0) {
                return;
            }
            var startPos = selectedNotes.First().position;
            Queue<string> lyricsQueue = new Queue<string>();
            
            docManager.StartUndoGroup("command.batch.lyric", true);
            foreach(var note in part.notes.Where(n => n.position >= startPos)){
                lyricsQueue.Enqueue(note.lyric);
                if(selectedNotes.Contains(note)){
                    docManager.ExecuteCmd(new ChangeNoteLyricCommand(part, note, "+~"));
                } else {
                    docManager.ExecuteCmd(new ChangeNoteLyricCommand(part, note, lyricsQueue.Dequeue()));
                }
            }
            docManager.EndUndoGroup();
        }
    }
}