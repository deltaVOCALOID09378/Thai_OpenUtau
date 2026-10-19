/*
AutoPitchContextualEngine.cs
OpenUtau.Core/Editing/AutoPitchContextualEngine.cs
Made And Checked By DELTA SYNTH & Gemini AI
Original by Patiphat Wongyai (Delta) & Gemini AI
Revision 1.2
*/

using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.AutoPitch
{
    public enum PitchShapeType
    {
        Thunderbolt,    // 1. ซิกแซกสายฟ้า สำหรับโน้ตกระโดดขึ้น
        PitchDropRise,  // 2. ช้อนเสียงขึ้น / รูดเสียงลง
        Slope,          // 3. สโลปนุ่มนวลแบบ Legato
        WShape,         // 4. คลื่นดับเบิ้ลดิพ 2 ลอน
        UPitchDrop,     // 5. ดิ่งลงลึกรูปตัว U ท้ายประโยค
        PitchDent       // 6. รอยบุ๋มกระแทกเน้นสำเนียงกลางคำ
    }

    public class NoteContext
    {
        public bool IsPhraseStart { get; set; }
        public bool IsPhraseEnd { get; set; }
        public int PitchDeltaToNext { get; set; }    // ระยะห่างคีย์โน้ตถัดไป (semitones)
        public int PitchDeltaFromPrev { get; set; }  // ระยะห่างคีย์จากโน้ตก่อนหน้า (semitones)
        public int DurationTicks { get; set; }       // ความยาวโน้ต (ticks)
        public double DurationMs { get; set; }       // ความยาวโน้ตจริง (milliseconds)
        public bool HasGapAfter { get; set; }        // มีช่องว่าง/พักหายใจหลังโน้ตหรือไม่
    }

    public static class AutoPitchContextualEngine
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        /// ฟังก์ชันหลัก: ประมวลผลสร้างคอร์ฟ Pitch ให้กับชุดโน้ตทั้งหมดตามบริบท
        /// </summary>
        public static void ApplyContextualAutoPitch(UPart part, List<UNote> notes, UProject? project = null)
        {
            if (notes == null || notes.Count == 0) return;

            var timeAxis = project?.timeAxis ?? DocManager.Inst.Project?.timeAxis;

            for (int i = 0; i < notes.Count; i++)
            {
                var current = notes[i];
                var prev = (i > 0) ? notes[i - 1] : null;
                var next = (i < notes.Count - 1) ? notes[i + 1] : null;

                // 1. วิเคราะห์บริบทของโน้ตตัวนี้
                var context = AnalyzeContext(current, prev, next, part, timeAxis);

                // 2. เลือกทรงคอร์ฟตามน้ำหนักบริบท (Weighted Selection)
                var chosenShape = SelectShapeByContext(context);

                // 3. สร้างจุด Pitch Points (PBS, PBW, PBY) ให้กับโน้ต
                GeneratePitchCurve(current, chosenShape, context);
            }
        }

        /// <summary>
        /// วิเคราะห์บริบททางดนตรีของโน้ต
        /// </summary>
        private static NoteContext AnalyzeContext(UNote current, UNote prev, UNote next, UPart part, TimeAxis? timeAxis)
        {
            double durationMs;
            if (timeAxis != null)
            {
                durationMs = Math.Max(20.0, timeAxis.MsBetweenTickPos(part.position + current.position, part.position + current.End));
            }
            else
            {
                durationMs = Math.Max(20.0, current.duration * 500.0 / 480.0);
            }

            var ctx = new NoteContext
            {
                DurationTicks = current.duration,
                DurationMs = durationMs,
                IsPhraseStart = (prev == null || (current.position - prev.End) > 60),
                IsPhraseEnd = (next == null || (next.position - current.End) > 60),
                HasGapAfter = (next == null || (next.position - current.End) > 30),
                PitchDeltaFromPrev = (prev != null) ? (current.tone - prev.tone) : 0,
                PitchDeltaToNext = (next != null) ? (next.tone - current.tone) : 0
            };
            return ctx;
        }

        /// <summary>
        /// เลือกลักษณะคอร์ฟ Pitch ตามความน่าจะเป็นที่เข้ากับบริบทเพลง
        /// </summary>
        private static PitchShapeType SelectShapeByContext(NoteContext ctx)
        {
            var weights = new Dictionary<PitchShapeType, int>
            {
                { PitchShapeType.Thunderbolt, 10 },
                { PitchShapeType.PitchDropRise, 15 },
                { PitchShapeType.Slope, 35 },
                { PitchShapeType.WShape, 15 },
                { PitchShapeType.UPitchDrop, 15 },
                { PitchShapeType.PitchDent, 10 }
            };

            // ปรับน้ำหนักตามบริบท:
            if (Math.Abs(ctx.PitchDeltaToNext) >= 3 || Math.Abs(ctx.PitchDeltaFromPrev) >= 3)
            {
                // โน้ตกระโดดขึ้นหรือลงขั้นคู่กว้าง -> ใช้ Slope (Legato) เพื่อความเชื่อมต่อที่นุ่มนวล
                weights[PitchShapeType.Slope] += 50;
                weights[PitchShapeType.Thunderbolt] += 25;
            }
            else if (ctx.IsPhraseEnd || ctx.HasGapAfter)
            {
                // ท้ายประโยค / มีช่องไฟ -> ใช้ U Pitch Drop หรือ Pitch Drop/Rise เพื่อทอดเสียงนุ่มนวล
                weights[PitchShapeType.UPitchDrop] += 50;
                weights[PitchShapeType.PitchDropRise] += 30;
                weights[PitchShapeType.WShape] += 20;
            }
            else if (ctx.IsPhraseStart)
            {
                // ต้นประโยค -> เพิ่มโอกาสเกิด Pitch Scoop (ช้อนเสียงหัวโน้ต)
                weights[PitchShapeType.PitchDropRise] += 60;
                weights[PitchShapeType.Slope] += 20;
            }
            else if (ctx.DurationMs >= 350)
            {
                // โน้ตยาว -> ลูกคอคลื่นเบาๆ หรือ Slope
                weights[PitchShapeType.WShape] += 40;
                weights[PitchShapeType.Slope] += 30;
                weights[PitchShapeType.PitchDent] += 20;
            }

            int totalWeight = weights.Values.Sum();
            int roll = Rng.Next(totalWeight);
            int cumulative = 0;

            foreach (var pair in weights)
            {
                cumulative += pair.Value;
                if (roll < cumulative)
                {
                    return pair.Key;
                }
            }

            return PitchShapeType.Slope;
        }

        /// <summary>
        /// สร้างเส้นกราฟ Pitch Points ใส่ลงในโน้ต
        /// หมายเหตุ: ใน OpenUtau
        /// - PitchPoint.X มีหน่วยเป็น ms สัมพัทธ์กับหัวโน้ต
        /// - PitchPoint.Y มีหน่วยเป็น 0.1 semitones (1.0 = 10 cents, 10.0 = 1 semitone)
        /// - snapFirst = true จะ snap จุดแรกเข้าหาโน้ตก่อนหน้าโดยอัตโนมัติหากโน้ตติดกัน
        /// </summary>
        private static void GeneratePitchCurve(UNote note, PitchShapeType shape, NoteContext ctx)
        {
            note.pitch.data.Clear();
            note.pitch.snapFirst = true;

            float durMs = (float)ctx.DurationMs;

            switch (shape)
            {
                case PitchShapeType.Thunderbolt:
                    // 1. Thunderbolt: เชื่อมต่อด้วย Portamento แล้วมี Accent เล็กน้อย (±15 cents)
                    note.pitch.AddPoint(new PitchPoint(-40, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(0, 1.5f, PitchPointShape.io));     // +15 cents peak
                    note.pitch.AddPoint(new PitchPoint(35, -0.8f, PitchPointShape.io));   // -8 cents micro-dip
                    note.pitch.AddPoint(new PitchPoint(70, 0, PitchPointShape.io));       // เสถียรบนคีย์
                    break;

                case PitchShapeType.PitchDropRise:
                    // 2. Pitch Drop/Rise: ช้อนเสียงหัวโน้ตอย่างเป็นธรรมชาติ (15-20 cents)
                    if (ctx.IsPhraseStart)
                    {
                        // Scoop จากด้านล่าง 18 cents
                        note.pitch.AddPoint(new PitchPoint(-30, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(0, -1.8f, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(45, 0, PitchPointShape.io));
                    }
                    else
                    {
                        note.pitch.AddPoint(new PitchPoint(-35, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(25, 0, PitchPointShape.io));
                    }

                    // หากเป็นท้ายประโยค ผ่อนเสียงท้ายโน้ต 20 cents
                    if ((ctx.IsPhraseEnd || ctx.HasGapAfter) && durMs > 150)
                    {
                        float dropStart = Math.Max(50, durMs - 60);
                        note.pitch.AddPoint(new PitchPoint(dropStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(durMs, -2.0f, PitchPointShape.io));
                    }
                    break;

                case PitchShapeType.Slope:
                default:
                    // 3. Slope: S-Curve เชื่อมต่อโน้ตนุ่มนวลแบบ Legato
                    // คำนวณความกว้างตามขนาดของขั้นคู่เสียง (Interval Adaptive)
                    int absDelta = Math.Abs(ctx.PitchDeltaFromPrev);
                    float portStart = absDelta >= 5 ? -60f : (absDelta >= 3 ? -45f : -35f);
                    float portEnd = absDelta >= 5 ? 45f : (absDelta >= 3 ? 30f : 25f);

                    note.pitch.AddPoint(new PitchPoint(portStart, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(portEnd, 0, PitchPointShape.io));
                    break;

                case PitchShapeType.WShape:
                    // 4. W-Shape: ดับเบิ้ลดิพ 2 ลอน ละมุนนุ่มนวลท้ายโน้ต (±10-12 cents)
                    note.pitch.AddPoint(new PitchPoint(-35, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(25, 0, PitchPointShape.io));

                    if (durMs >= 300)
                    {
                        float wStart = durMs - 140;
                        note.pitch.AddPoint(new PitchPoint(wStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(wStart + 35, -1.2f, PitchPointShape.io)); // Wave 1 (-12 cents)
                        note.pitch.AddPoint(new PitchPoint(wStart + 70, 0.6f, PitchPointShape.io));  // Wave peak (+6 cents)
                        note.pitch.AddPoint(new PitchPoint(wStart + 105, -1.0f, PitchPointShape.io)); // Wave 2 (-10 cents)
                        note.pitch.AddPoint(new PitchPoint(durMs, 0, PitchPointShape.io));
                    }
                    break;

                case PitchShapeType.UPitchDrop:
                    // 5. U-Pitch Drop: ท้ายโน้ตโค้งดิ่งลงอย่างนุ่มนวลแล้วตัดเสียง (-25 cents)
                    note.pitch.AddPoint(new PitchPoint(-35, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(25, 0, PitchPointShape.io));

                    if (durMs >= 200)
                    {
                        float uStart = Math.Max(30, durMs - 90);
                        note.pitch.AddPoint(new PitchPoint(uStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(durMs - 35, -2.5f, PitchPointShape.io)); // จุดต่ำสุด (-25 cents)
                        note.pitch.AddPoint(new PitchPoint(durMs, -1.2f, PitchPointShape.io));      // ผ่อนคลายก่อนหมดเสียง
                    }
                    break;

                case PitchShapeType.PitchDent:
                    // 6. Pitch Dent: รอยบุ๋มกระแทกเน้นสำเนียงช่วงกลางโน้ตอย่างละมุน (-15 cents)
                    note.pitch.AddPoint(new PitchPoint(-35, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(25, 0, PitchPointShape.io));

                    if (durMs >= 250)
                    {
                        float dentMid = durMs * 0.45f;
                        note.pitch.AddPoint(new PitchPoint(dentMid - 25, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(dentMid, -1.5f, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(dentMid + 25, 0, PitchPointShape.io));
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// BatchEdit wrapper สำหรับ Contextual AutoPitch Generator
    /// </summary>
    public class ContextualAutoPitchBatchEdit : BatchEdit
    {
        public string Name => "pianoroll.menu.notes.contextualautopitch";

        public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager)
        {
            var notes = selectedNotes.Count > 0 ? selectedNotes : part.notes.ToList();
            if (notes.Count == 0) return;

            docManager.StartUndoGroup("command.batch.contextualautopitch", true);
            try
            {
                AutoPitchContextualEngine.ApplyContextualAutoPitch(part, notes, project);
                foreach (var note in notes)
                {
                    docManager.ExecuteCmd(new SetPitchPointsCommand(part, note, note.pitch));
                }
                docManager.ExecuteCmd(new ShowPitchNotification());
            }
            finally
            {
                docManager.EndUndoGroup();
            }
        }
    }
}