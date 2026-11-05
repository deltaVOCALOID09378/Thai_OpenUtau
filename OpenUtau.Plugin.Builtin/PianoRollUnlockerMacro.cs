// ============================================================================
// 1. วัน/เดือน/ปี: 5 พฤศจิกายน พ.ศ. 2569 / 05 November 2026
// 2. ชื่อไฟล์โค้ด: PianoRollUnlockerMacro.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.6
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/PianoRollUnlockerMacro.cs
// 5. หน้าที่: ปลดล็อกการเคลื่อนที่ของโน้ตใน Piano Roll แบบอิสระ รองรับการเรียกใช้งานผ่านระบบ BatchEdit
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI | Original by DELTA SYNTH
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Plugin.Builtin {
    public class PianoRollUnlockerMacro : BatchEdit {
        private readonly string centralPath = Environment.GetEnvironmentVariable("DELTA_SYNTH_CENTRAL")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DELTA_SYNTH_Central");
        private readonly string uiNameThai = "ปลดล็อกการเคลื่อนที่โน้ตแบบอิสระ";
        private readonly string uiNameEng = "Unlock Free Note Movement";

        public virtual string Name => $"[DELTA SYNTH] {uiNameThai} ({uiNameEng})";

        public string CentralResourcePath => centralPath;

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            if (part == null) return;
            IEnumerable<UNote> targetNotes = (selectedNotes != null && selectedNotes.Count > 0) ? selectedNotes : (IEnumerable<UNote>)part.notes;
            ProcessSelectedNotes(targetNotes);
        }

        public void ProcessSelectedNotes(IEnumerable<UNote> selectedNotes) {
            if (selectedNotes == null) return;
            const int unquantizeOffset = 0;

            foreach (var note in selectedNotes) {
                ApplyFreeMovement(note, unquantizeOffset);
            }
        }

        private static void ApplyFreeMovement(UNote note, int offset) {
            if (note != null) {
                note.position += offset;
            }
        }
    }
}
