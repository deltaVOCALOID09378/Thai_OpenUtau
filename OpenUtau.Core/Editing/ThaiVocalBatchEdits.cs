/*
ThaiVocalBatchEdits.cs
OpenUtau.Core/Editing/ThaiVocalBatchEdits.cs
Made And Checked By DELTA SYNTH & Gemini AI
Original by Patiphat Wongyai
Revision 1.2
*/

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Editing {
    
    // 1. กดเพื่อปรับปรุงการจัดพื้นที่ ของพยัญชนะ ให้เข้าที่เข้าทาง
    public class AlignConsonants : BatchEdit {
        public string Name => "pianoroll.menu.notes.alignconsonants";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            if (notes.Count == 0) return;

            docManager.StartUndoGroup("command.batch.alignconsonants", true);
            foreach (var note in notes) {
                bool shouldClear = note.phonemeOverrides.Any(o => o.offset != null || o.preutterDelta != null || o.overlapDelta != null);
                if (shouldClear) {
                    docManager.ExecuteCmd(new ClearPhonemeTimingCommand(part, note));
                }
            }
            docManager.EndUndoGroup();
        }
    }

    // 2. ปุ่มกดเพื่อร่างเว้นเสียงสีแดง โดยการวาดเส้นสีแดง ตามอารมณ์เสียงใน เนื้อเพลง มาให้แบบ อัตโนมัติ หลังจากกดใช้งานไป
    public class DrawVocalMoodCurve : BatchEdit {
        public string Name => "pianoroll.menu.notes.drawvocalmoodcurve";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            if (notes.Count == 0) return;

            docManager.StartUndoGroup("command.batch.drawvocalmoodcurve", true);
            try {
                for (int i = 0; i < notes.Count; i++) {
                    var note = notes[i];
                    var prev = (i > 0) ? notes[i - 1] : note.Prev;
                    var next = (i < notes.Count - 1) ? notes[i + 1] : note.Next;

                    bool isPhraseStart = prev == null || (note.position - prev.End) > 60;
                    bool isPhraseEnd = next == null || (next.position - note.End) > 60;

                    // 1. Reset and draw clean pitch curve
                    docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));

                    float durationMs = (float)project.timeAxis.TickPosToMsPos(part.position + note.End) - 
                                       (float)project.timeAxis.TickPosToMsPos(part.position + note.position);

                    // Add subtle attack scoop on phrase starts (18 cents)
                    if (isPhraseStart && durationMs > 120) {
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(0, -1.8f, PitchPointShape.io), 1));
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(45, 0, PitchPointShape.io), 2));
                    }

                    // Add release drop on phrase ends (25 cents) without double portamento
                    if (isPhraseEnd && durationMs > 250) {
                        float dropStart = Math.Max(50, durationMs - 70);
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(dropStart, 0, PitchPointShape.io), note.pitch.data.Count));
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(durationMs, -2.5f, PitchPointShape.io), note.pitch.data.Count));
                    }

                    // 2. Setup vibrato based on length
                    if (note.duration >= 480 && durationMs >= 350) {
                        var vbr = new UVibrato {
                            length = 68f,
                            period = 18f,
                            depth = 30f,
                            @in = 25f,
                            @out = 15f,
                            shift = 0f,
                            drift = 0f
                        };
                        docManager.ExecuteCmd(new SetVibratoCommand(part, note, vbr));
                    } else {
                        docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0f));
                    }

                    // 3. Draw DYN curves (red/pink expression curve)
                    string dynAbbr = Format.Ustx.DYN;
                    if (project.expressions.TryGetValue(dynAbbr, out var descriptor)) {
                        int startValue = (int)(descriptor.defaultValue * 0.85f);
                        int endValue = (int)(descriptor.defaultValue * 1.05f);
                        docManager.ExecuteCmd(new SetCurveCommand(project, part, dynAbbr, note.End, endValue, note.position, startValue));
                    }
                }
                docManager.ExecuteCmd(new ShowPitchNotification());
            } finally {
                docManager.EndUndoGroup();
            }
        }
    }

    // 3. ปุ่มกดเพื่อสร้างตัวเพิ่มเสียงเอื้อนจังหวะเร็วๆ ตามเนื้อเพลงที่มีความยาว และต้องเอื้อนเสียงทุกครั้ง ให้ลงตัว
    public class AddVocalRuns : BatchEdit {
        public string Name => "pianoroll.menu.notes.addvocalruns";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            if (notes.Count == 0) return;

            // Find notes that are long enough to split (duration > 480)
            // Sort them from last to first so that shifting positions don't affect notes processed earlier
            var targetNotes = notes.Where(n => n.duration > 480 && n.lyric != "-").OrderByDescending(n => n.position).ToList();
            if (targetNotes.Count == 0) return;

            docManager.StartUndoGroup("command.batch.addvocalruns", true);
            
            foreach (var note in targetNotes) {
                int splitDuration = 120; // 16th note length for a quick run
                int newDuration = note.duration - splitDuration;

                // Resize the original note
                docManager.ExecuteCmd(new ResizeNoteCommand(part, note, newDuration - note.duration));

                // Add the melisma note "-" at the end
                var runNote = project.CreateNote(note.tone, note.position + newDuration, splitDuration);
                runNote.lyric = "-";
                
                // Copy expressions from original note
                foreach (var exp in note.phonemeExpressions.OrderBy(e => e.index)) {
                    runNote.SetExpression(project, project.tracks[part.trackNo], exp.abbr, new float?[] { exp.value });
                }

                docManager.ExecuteCmd(new AddNoteCommand(part, runNote));
            }

            docManager.EndUndoGroup();
        }
    }
}
