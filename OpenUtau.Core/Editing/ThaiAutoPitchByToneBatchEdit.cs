// ==========================================
// วัน/เดือน/ปี: 11/11/2026 (2569)
// ชื่อไฟล์: ThaiAutoPitchByToneBatchEdit.cs
// ลำดับเวอร์ชัน: 1.1
// เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
// [ Original by DELTA SYNTH ] (Edit by DELTA SYNTH)
// ตำแหน่งที่จัดวางไฟล์: OpenUtau.Core/Editing/ThaiAutoPitchByToneBatchEdit.cs
// หน้าที่: วาดเส้นแดง (Pitch) อัตโนมัติอ้างอิงตามวรรณยุกต์ไทย ปรับสมดุล Micro-cents ไม่หลุดคีย์ และใช้เวลา ms จริง
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

                    // คำนวณความยาวเวลาจริงในหน่วยมิลลิวินาที (Real-time Milliseconds)
                    double durationMs = project.timeAxis.MsBetweenTickPos(
                        part.position + note.position,
                        part.position + note.End);

                    // ตรวจจับวรรณยุกต์อย่างง่ายจากเนื้อร้อง
                    int toneType = DetectThaiTone(note.lyric);
                    var newPitch = CreatePitchFromTone(toneType, durationMs, random);

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

        private static UPitch CreatePitchFromTone(int tone, double durationMs, Random random) {
            var pitch = new UPitch { snapFirst = true };
            float jitter() => (float)(random.NextDouble() * 0.2 - 0.1); // Jitter ละมุน 1-2 cents

            switch (tone) {
                case 1: // เสียงเอก (Low) - กดต่ำลงเล็กน้อย (-8 cents)
                    pitch.data.Add(new PitchPoint(-35, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(20, -0.8f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint((float)Math.Min(120, durationMs * 0.5), -0.6f + jitter(), PitchPointShape.io));
                    break;
                case 2: // เสียงโท (Falling) - ยกขึ้นเบาๆ แล้วทอดลง (-10 cents)
                    pitch.data.Add(new PitchPoint(-30, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(25, 0.7f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint((float)Math.Min(140, durationMs * 0.5), -1.0f + jitter(), PitchPointShape.io));
                    break;
                case 3: // เสียงตรี (High) - ยกขึ้นสูงเล็กน้อย (+9 cents)
                    pitch.data.Add(new PitchPoint(-25, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(25, 0.9f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint((float)Math.Min(120, durationMs * 0.5), 0.7f + jitter(), PitchPointShape.io));
                    break;
                case 4: // เสียงจัตวา (Rising) - กดต่ำแล้วช้อนตวัดขึ้น (+10 cents)
                    pitch.data.Add(new PitchPoint(-30, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(30, -0.8f + jitter(), PitchPointShape.io));
                    pitch.data.Add(new PitchPoint((float)Math.Min(140, durationMs * 0.5), 1.0f + jitter(), PitchPointShape.io));
                    break;
                default: // เสียงสามัญ (Mid) - ราบเรียบ เกาะคีย์ 100%
                    pitch.data.Add(new PitchPoint(-30, 0, PitchPointShape.io));
                    pitch.data.Add(new PitchPoint(20, 0, PitchPointShape.io));
                    break;
            }

            // คืนตัวนุ่มนวลท้ายโน้ต
            if (durationMs >= 350) {
                pitch.data.Add(new PitchPoint((float)(durationMs - 60), 0.4f, PitchPointShape.io));
                pitch.data.Add(new PitchPoint((float)durationMs, 0, PitchPointShape.io));
            } else {
                pitch.data.Add(new PitchPoint((float)durationMs, 0, PitchPointShape.io));
            }

            return pitch;
        }
    }
}
