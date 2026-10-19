/*
VocalTuningMacro.cs
OpenUtau.Core/Editing/VocalTuningMacro.cs
Made And Checked By DELTA SYNTH & Gemini AI
Original by Patiphat Wongyai
Revision 1.3
*/

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Editing {
    public class VocalTuningMacro : BatchEdit {
        public virtual string Name { get; } = "pianoroll.menu.notes.vocaltuning";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            if (notes.Count == 0) return;

            docManager.StartUndoGroup("command.batch.vocaltuning", true);
            try {
                for (int i = 0; i < notes.Count; i++) {
                    var note = notes[i];
                    var prev = (i > 0) ? notes[i - 1] : note.Prev;
                    var next = (i < notes.Count - 1) ? notes[i + 1] : note.Next;

                    bool isPhraseStart = prev == null || (note.position - prev.End) > 60;
                    bool isPhraseEnd = next == null || (next.position - note.End) > 60;

                    // 1. Pitch: Reset to standard clean portamento
                    docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));

                    float durationMs = (float)project.timeAxis.TickPosToMsPos(part.position + note.End) - 
                                       (float)project.timeAxis.TickPosToMsPos(part.position + note.position);

                    // Add subtle attack scoop on phrase starts (15 cents)
                    if (isPhraseStart && durationMs > 120) {
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(0, -1.5f, PitchPointShape.io), 1));
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(40, 0, PitchPointShape.io), 2));
                    }

                    // Add gentle release drop on phrase ends (20 cents) without collision
                    if (isPhraseEnd && durationMs > 250) {
                        float dropStart = Math.Max(60, durationMs - 70);
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(dropStart, 0, PitchPointShape.io), note.pitch.data.Count));
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, new PitchPoint(durationMs, -2.0f, PitchPointShape.io), note.pitch.data.Count));
                    }

                    // 2. Vibrato: enable with smooth ease-in on sustained notes (>480 ticks)
                    if (note.duration >= 480 && durationMs >= 350) {
                        var vbr = new UVibrato {
                            length = 65f,
                            period = 17.5f,
                            depth = 28f,
                            @in = 30f,
                            @out = 15f,
                            shift = 0f,
                            drift = 0f
                        };
                        docManager.ExecuteCmd(new SetVibratoCommand(part, note, vbr));
                    } else {
                        docManager.ExecuteCmd(new VibratoLengthCommand(part, note, 0f));
                    }

                    // 3. Dynamics: natural subtle vocal presence swell via DYN curve
                    string dynAbbr = Format.Ustx.DYN;
                    if (project.expressions.TryGetValue(dynAbbr, out var descriptor)) {
                        int startValue = (int)(descriptor.defaultValue * 0.88f);
                        int endValue = (int)(descriptor.defaultValue * 1.06f);
                        docManager.ExecuteCmd(new SetCurveCommand(project, part, dynAbbr, note.End, endValue, note.position, startValue));
                    }
                }
                docManager.ExecuteCmd(new ShowPitchNotification());
            } finally {
                docManager.EndUndoGroup();
            }
        }
    }
}