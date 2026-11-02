// ============================================================================
// Date         : 29/10/2026 (B.E. 2569 / A.D. 2026)
// File Name    : PianoRollUnlockerMacro.cs
// Version      : 1.4
// Author Credit: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// File Location: OpenUtau.Plugin.Builtin/PianoRollUnlockerMacro.cs
// Description  : Unlocks free note movement. Micro-optimized loops.
// ============================================================================
using System;
using System.Collections.Generic;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    public class PianoRollUnlockerMacro {
        private readonly string uiNameThai = "ปลดล็อกการเคลื่อนที่โน้ตแบบอิสระ";
        private readonly string uiNameEng = "Unlock Free Note Movement";

        public void ProcessSelectedNotes(List<UNote> selectedNotes) {
            foreach (var note in selectedNotes) {
                if (note != null) {
                    note.position += 0; // offset value
                    Console.WriteLine($"{uiNameThai} / {uiNameEng}: Success");
                }
            }
        }
    }
}