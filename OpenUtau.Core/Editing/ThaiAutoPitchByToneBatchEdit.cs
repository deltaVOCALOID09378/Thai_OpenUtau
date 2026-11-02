// ==========================================
// วัน/เดือน/ปี: 29/10/2026
// ชื่อไฟล์: ThaiAutoPitchByToneBatchEdit.cs
// ลำดับเวอร์ชัน: 1.0
// เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Agentic AI
// [ Original by DELTA SYNTH ] (Edit by DELTA SYNTH)
// ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Editing/ThaiAutoPitchByToneBatchEdit.cs
// หน้าที่: วาดเส้นแดง (Pitch) อัตโนมัติอ้างอิงตามวรรณยุกต์ไทย พร้อมแทรกการเอื้อนธรรมชาติช่วงปลายโน้ต
// ==========================================

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Editing {
    public class ThaiAutoPitchByToneBatchEdit : BatchEdit {
        public string Name => "pianoroll.menu.notes.thaiautopitchbytone";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            if (notes.Count == 0) return;

            var random = new Random();
            docManager.StartUndoGroup("command.batch.note", true);
            
            try {
                int totalNotes = notes.Count;
                int count = 0;

                foreach (var note in notes) {
                    if (note == null || note.pitch == null) continue;

                    // เคลียร์เส้นแดงเดิมอย่างปลอดภัย
                    docManager.ExecuteCmd(new ResetPitchPointsCommand(part, note));

                    // ตรวจจับวรรณยุกต์อย่างง่ายจากเนื้อร้อง
                    int toneType = DetectThaiTone(note.lyric);
                    var newPitch = CreatePitchFromTone(toneType, note.duration, random);

                    int index = 0;
                    foreach (var point in newPitch.data) {
                        docManager.ExecuteCmd(new AddPitchPointCommand(part, note, point, index));
                        index++;
                    }

                    // ลบจุดพิตช์เริ่มต้น 2 จุดที่มาพร้อมคำสั่ง Reset
                    docManager.ExecuteCmd(new DeletePitchPointCommand(part, note, index));
                    docManager.ExecuteCmd(new DeletePitchPointCommand(part, note, index));

                    count++;
                    docManager.ExecuteCmd(new ProgressBarNotification(count * 100.0 / totalNotes, "Generating Thai Tones..."));
                }
                
                docManager.ExecuteCmd(new ProgressBarNotification(0, ""));
                docManager.ExecuteCmd(new ShowPitchNotification());
            } finally {
                docManager.EndUndoGroup();
            }
        }

        private int DetectThaiTone(string lyric) {
            // 0: สามัญ, 1: เอก, 2: โท, 3: ตรี, 4: จัตวา
            if (string.IsNullOrEmpty(lyric)) return 0;
            if (lyric.Contains("่")) return 1;
            if (lyric.Contains("้")) return 2;
            if (lyric.Contains("๊")) return 3;
            if (lyric.Contains("๋")) return 4;
            return 0; // Default สามัญ
        }

        private static UPitch CreatePitchFromTone(int tone, int duration, Random random) {
            var pitch = new UPitch { snapFirst = true };
            float jitter() => (float)(random.NextDouble() * 0.4 - 0.2); // เพิ่มความสมจริงของเสียงมนุษย์

            switch (tone) {
                case 1: // เสียงเอก (Low) - กดต่ำลงแล้วคงที่
                    pitch.data.Add(new PitchPoint(-40, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(20, -2.5f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(duration > 240 ? 120 : duration / 2, -2.0f + jitter(), PitchPointShape.io));
                    break;
                case 2: // เสียงโท (Falling) - ยกขึ้นเล็กน้อยแล้วตกลงลึก
                    pitch.data.Add(new PitchPoint(-30, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(25, 2.0f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(duration > 240 ? 150 : duration / 2, -3.5f + jitter(), PitchPointShape.io));
                    break;
                case 3: // เสียงตรี (High) - ยกขึ้นสูงแล้วคงที่
                    pitch.data.Add(new PitchPoint(-20, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(30, 3.0f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(duration > 240 ? 120 : duration / 2, 2.5f + jitter(), PitchPointShape.io));
                    break;
                case 4: // เสียงจัตวา (Rising) - กดต่ำแล้วตวัดขึ้นสูง (ลูกเอื้อน)
                    pitch.data.Add(new PitchPoint(-30, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(35, -2.5f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(duration > 240 ? 150 : duration / 2, 3.0f + jitter(), PitchPointShape.io));
                    break;
                default: // เสียงสามัญ (Mid) - ราบเรียบ มี Jitter เล็กน้อย
                    pitch.data.Add(new PitchPoint(-40, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(30, 0.5f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(duration > 240 ? 100 : duration / 2, -0.2f + jitter(), PitchPointShape.io));
                    break;
            }

            // แทรกกลไก "เอื้อนแบบจังหวะเร็ว" ในช่วงท้ายของโน้ตหากมีความยาวเพียงพอ
            if (duration >= 480) {
                pitch.data.Add(new PitchPoint(duration - 150, 1.5f, PitchPointShape.io));
                pitch.data.Add(new PitchPoint(duration - 75, -1.0f, PitchPointShape.io));
                pitch.data.Add(new PitchPoint(duration, 0, PitchPointShape.io));
            } else {
                pitch.data.Add(new PitchPoint(duration, 0, PitchPointShape.io));
            }

            return pitch;
        }
    }
}