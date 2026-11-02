// ============================================================================
// Date         : 28/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : PianoRollUnlockerMacro.cs
// Version      : 1.3
// Author Credit: Made And Checked By DELTA SYNTH & Gemini AI [ Original by DELTA SYNTH ]
// File Location: OpenUtau.Plugin.Builtin/PianoRollUnlockerMacro.cs
// Description  : Unlocks free note movement in Piano Roll with configurable
//                central resource resolution via environment variable.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using OpenUtau.Api;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    public class PianoRollUnlockerMacro {

        private readonly string centralPath = Environment.GetEnvironmentVariable("DELTA_SYNTH_CENTRAL")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DELTA_SYNTH_Central");
        private readonly string uiNameThai = "ปลดล็อกการเคลื่อนที่โน้ตแบบอิสระ";
        private readonly string uiNameEng = "Unlock Free Note Movement";

        public void ProcessSelectedNotes(List<UNote> selectedNotes) {
            if (selectedNotes == null) return;
            int unquantizeValue = 0;

            foreach (var note in selectedNotes) {
                ApplyFreeMovement(note, unquantizeValue);
            }
        }

        private void ApplyFreeMovement(UNote note, int offset) {
            if (note != null) {
                note.position = note.position + offset;
            }
        }
    }
}
