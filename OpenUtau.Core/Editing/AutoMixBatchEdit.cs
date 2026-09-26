// Made And Checked By DELTA SYNTH & Gemini AI
// Original by DELTA SYNTH
// Version: v.1.1
// History/Summary: Fixed CS1061 IsRest error and updated expression API calls to use UTrack.
// Summary: AutoMixBatchEdit — auto-adjusts note velocity/dynamics based on pitch contour
//          for more natural, expressive, and melodic vocal output.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Editing {
    /// <summary>
    /// Applies automatic dynamic mixing to selected notes.
    /// Higher-pitched notes get a slight velocity boost; lower-pitched notes get a slight reduction.
    /// Result: natural-sounding dynamics that match pitch contour — like a real singer.
    /// </summary>
    public class AutoMixBatchEdit : BatchEdit {
        // Velocity adjustment constants
        private const int BoostAmount       = 8;   // vel += 8 for notes above average
        private const int CutAmount         = 5;   // vel -= 5 for notes below average
        private const int SemitoneThreshold = 2;   // ±2 semitones from avg to trigger adjustment

        // Velocity clamping bounds
        private const int MinVelocity = 20;
        private const int MaxVelocity = 127;

        // Expression key for velocity/loudness (standard OpenUtau expression)
        private const string VelKey = "vel";

        public string Name => "AutoMix (Auto Dynamics)";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            if (selectedNotes == null || selectedNotes.Count == 0) return;

            // Calculate average pitch of selected notes
            var pitchedNotes = selectedNotes.ToList();
            if (pitchedNotes.Count == 0) return;

            double avgTone = pitchedNotes.Average(n => (double)n.tone);

            docManager.StartUndoGroup();
            try {
                UTrack track = project.tracks[part.trackNo];
                foreach (var note in pitchedNotes) {
                    // Get current velocity expression value (fallback 100)
                    int currentVel = 100;
                    var exprVals = note.GetExpressionNoteHas(project, track, VelKey);
                    if (exprVals != null && exprVals.Length > 0 && exprVals[0].HasValue) {
                        currentVel = (int)exprVals[0].Value;
                    }

                    // Determine pitch delta from average
                    int semitonesDelta = note.tone - (int)Math.Round(avgTone);
                    int newVel = currentVel;

                    if (semitonesDelta >= SemitoneThreshold) {
                        // Note is higher than average -> boost
                        newVel = Math.Min(currentVel + BoostAmount, MaxVelocity);
                    } else if (semitonesDelta <= -SemitoneThreshold) {
                        // Note is lower than average -> cut
                        newVel = Math.Max(currentVel - CutAmount, MinVelocity);
                    }
                    // Notes within threshold: unchanged

                    if (newVel != currentVel) {
                        docManager.ExecuteCmd(new SetNoteExpressionCommand(project, track, part, note, VelKey, new float?[] { newVel }));
                    }
                }
            } finally {
                docManager.EndUndoGroup();
            }
        }
    }
}
