// ============================================================================
// 1. วัน/เดือน/ปี พ.ศ. กับ ปี ค.ศ.: 11 พฤศจิกายน 2569 / 11 November 2026
// 2. ชื่อไฟล์: AutoPitchContextualEngine.cs
// 3. ลำดับเวอร์ชัน: 1.4
// 4. รายชื่อเครดิต: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH & Gemini AI ]
// 5. ตำแหน่งที่จัดวาง: OpenUtau.Core/AutoPitch/AutoPitchContextualEngine.cs
// 6. รายละเอียดการแก้ไขงาน: Smart Melodic AI AutoPitch Engine & Calibrated Canon Shapes with Zero-Defect Tuning Balance
// ============================================================================

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
        Thunderbolt,    // 1. ซิกแซกสายฟ้า สำหรับโน้ตกระโดดขึ้น (Accent)
        PitchDropRise,  // 2. ช้อนเสียงขึ้น / รูดเสียงลง (Attack/Release)
        Slope,          // 3. สโลปนุ่มนวลแบบ Legato
        WShape,         // 4. คลื่นดับเบิ้ลดิพ 2 ลอน (Micro-wave)
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
            ApplyContextualAutoPitch(part, notes, project, null, null, null, true, true);
        }

        public static void ApplyContextualAutoPitch(
            UPart part,
            List<UNote> notes,
            UProject? project,
            ISet<PitchShapeType>? allowedShapes,
            float? customPortamentoStartMs = null,
            float? customPortamentoLengthMs = null)
        {
            ApplyContextualAutoPitch(part, notes, project, allowedShapes, customPortamentoStartMs, customPortamentoLengthMs, true, true);
        }

        public static void ApplyContextualAutoPitch(
            UPart part,
            List<UNote> notes,
            UProject? project,
            ISet<PitchShapeType>? allowedShapes,
            float? customPortamentoStartMs,
            float? customPortamentoLengthMs,
            bool smartMelodicMode,
            bool applyNaturalVibrato = true)
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

                if (smartMelodicMode)
                {
                    // 2A. โหมดอัจฉริยะ (Smart Melodic AI Tuning): เกาะคีย์ 100% ไร้เสียงเพี้ยน ร้องละมุนเป็นธรรมชาติ
                    ApplySmartMelodicTuning(current, context, customPortamentoStartMs, customPortamentoLengthMs, applyNaturalVibrato);
                }
                else
                {
                    // 2B. โหมดเลือก 6 รูปทรง Canon ที่ปรับสมดุลพิกัดไมโครเซนต์แล้ว
                    var chosenShape = SelectShapeByContext(context, allowedShapes);
                    GenerateCalibratedCanonPitchCurve(current, chosenShape, context, customPortamentoStartMs, customPortamentoLengthMs, applyNaturalVibrato);
                }
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
        /// การจูนอัจฉริยะระดับสูง (Smart Melodic AI Tuning):
        /// - รอยต่อ Legato อัจฉริยะตามขั้นคู่เสียง (Interval-Adaptive S-Curve)
        /// - ช้อนเสียงหัวประโยค (Attack Scoop) นุ่มนวลเพียง 8-10 cents เฉพาะโน้ตที่ไม่สั้นเกินไป
        /// - ผ่อนเสียงท้ายประโยค (Release Fall) นุ่มนวลเพียง 10-12 cents
        /// - แกนเสียงคงที่มั่นคง (Rock-Solid Pitch Center) ไม่วอกแวก ไม่หลุดคีย์
        /// - ลูกคอชีวกลศาสตร์เสียงร้อง (Natural Vocal Vibrato) 6.0 Hz บนโน้ตยาว
        /// </summary>
        private static void ApplySmartMelodicTuning(
            UNote note,
            NoteContext ctx,
            float? customPortamentoStartMs,
            float? customPortamentoLengthMs,
            bool applyNaturalVibrato)
        {
            note.pitch.data.Clear();
            note.pitch.snapFirst = true;

            float durMs = (float)ctx.DurationMs;
            int absDelta = Math.Abs(ctx.PitchDeltaFromPrev);

            // 1. Legato Portamento (สำหรับโน้ตที่ต่อเนื่องกัน)
            if (!ctx.IsPhraseStart)
            {
                float portStart;
                float portEnd;

                if (customPortamentoStartMs.HasValue && customPortamentoLengthMs.HasValue)
                {
                    portStart = customPortamentoStartMs.Value;
                    portEnd = portStart + Math.Max(10f, customPortamentoLengthMs.Value);
                }
                else
                {
                    // ปรับความกว้างตามระยะขั้นคู่เสียง (Interval Adaptive)
                    if (absDelta >= 6)
                    {
                        portStart = -50f;
                        portEnd = 30f;
                    }
                    else if (absDelta >= 3)
                    {
                        portStart = -38f;
                        portEnd = 22f;
                    }
                    else
                    {
                        portStart = -28f;
                        portEnd = 16f;
                    }
                }

                // การ์ดป้องกันจุดพิตช์ล้นโน้ตสั้น
                if (durMs < 120f)
                {
                    portEnd = Math.Min(portEnd, durMs * 0.28f);
                }

                note.pitch.AddPoint(new PitchPoint(portStart, 0, PitchPointShape.io));
                note.pitch.AddPoint(new PitchPoint(portEnd, 0, PitchPointShape.io));
            }
            else
            {
                // 2. Phrase Start: ช้อนเสียงนุ่มนวลจากด้านล่าง (8-10 cents) เฉพาะโน้ตที่มีความยาวเหมาะสม
                if (durMs >= 150f)
                {
                    note.pitch.AddPoint(new PitchPoint(-25f, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(0f, -0.9f, PitchPointShape.io)); // Scoop -9 cents
                    note.pitch.AddPoint(new PitchPoint(35f, 0, PitchPointShape.io));    // คืนสู่คีย์หลัก
                }
                else
                {
                    // โน้ตสั้นขึ้นต้นประโยค วางพิตช์สะอาดเพื่อความกระชับของจังหวะ
                    note.pitch.AddPoint(new PitchPoint(-20f, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(15f, 0, PitchPointShape.io));
                }
            }

            // 3. Phrase End / Gap After: ผ่อนแรงท้ายประโยคอย่างเป็นธรรมชาติ (10-12 cents)
            if ((ctx.IsPhraseEnd || ctx.HasGapAfter) && durMs >= 220f)
            {
                float dropStart = Math.Max(60f, durMs - 55f);
                note.pitch.AddPoint(new PitchPoint(dropStart, 0, PitchPointShape.io));
                note.pitch.AddPoint(new PitchPoint(durMs, -1.1f, PitchPointShape.io)); // Release -11 cents
            }

            // 4. Natural Vocal Vibrato (ลูกคอชีวกลศาสตร์เสียงร้อง)
            if (applyNaturalVibrato)
            {
                if (note.duration >= 480 && durMs >= 350f)
                {
                    note.vibrato = new UVibrato
                    {
                        length = 65f,
                        period = 17.0f, // ~5.9 - 6.0 Hz อัตราการสั่นของสายเสียงมนุษย์
                        depth = 25f,    // 25 cents ลึกพอดี ไม่เพี้ยน ไม่แกว่ง
                        @in = 30f,      // ค่อยๆ เข้าอย่างนุ่มนวลหลังเสียงเริ่ม
                        @out = 15f,     // ผ่อนออกเบาๆ
                        shift = 0f,
                        drift = 0f
                    };
                }
                else
                {
                    note.vibrato.length = 0f;
                }
            }
        }

        /// <summary>
        /// เลือกลักษณะคอร์ฟ Pitch ตามความน่าจะเป็นที่เข้ากับบริบทเพลง (สำหรับโหมด 6 ทรง)
        /// </summary>
        private static PitchShapeType SelectShapeByContext(NoteContext ctx, ISet<PitchShapeType>? allowedShapes = null)
        {
            var weights = new Dictionary<PitchShapeType, int>
            {
                { PitchShapeType.Thunderbolt, 10 },
                { PitchShapeType.PitchDropRise, 20 },
                { PitchShapeType.Slope, 45 },
                { PitchShapeType.WShape, 10 },
                { PitchShapeType.UPitchDrop, 10 },
                { PitchShapeType.PitchDent, 5 }
            };

            if (Math.Abs(ctx.PitchDeltaToNext) >= 3 || Math.Abs(ctx.PitchDeltaFromPrev) >= 3)
            {
                weights[PitchShapeType.Slope] += 50;
            }
            else if (ctx.IsPhraseEnd || ctx.HasGapAfter)
            {
                weights[PitchShapeType.UPitchDrop] += 30;
                weights[PitchShapeType.PitchDropRise] += 25;
            }
            else if (ctx.IsPhraseStart)
            {
                weights[PitchShapeType.PitchDropRise] += 50;
                weights[PitchShapeType.Slope] += 25;
            }
            else if (ctx.DurationMs >= 350)
            {
                weights[PitchShapeType.WShape] += 25;
                weights[PitchShapeType.Slope] += 35;
            }

            if (allowedShapes != null && allowedShapes.Count > 0)
            {
                foreach (var key in weights.Keys.ToList())
                {
                    if (!allowedShapes.Contains(key))
                    {
                        weights.Remove(key);
                    }
                }
                if (weights.Count == 0)
                {
                    return allowedShapes.First();
                }
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
        /// สร้างรูปทรงพิตช์ 6 รูปแบบของ Canon โดยปรับค่าเซนต์ให้อยู่ในเกณฑ์ Micro-intonation ที่ปลอดภัย (ไม่เพี้ยน)
        /// </summary>
        private static void GenerateCalibratedCanonPitchCurve(
            UNote note,
            PitchShapeType shape,
            NoteContext ctx,
            float? customPortamentoStartMs,
            float? customPortamentoLengthMs,
            bool applyNaturalVibrato)
        {
            note.pitch.data.Clear();
            note.pitch.snapFirst = true;

            float durMs = (float)ctx.DurationMs;
            float baseStart = customPortamentoStartMs ?? -30f;
            float baseEnd = customPortamentoStartMs.HasValue && customPortamentoLengthMs.HasValue
                ? customPortamentoStartMs.Value + Math.Max(10f, customPortamentoLengthMs.Value)
                : 20f;

            // การ์ดป้องกันจุดพิตช์ล้นโน้ตสั้น
            if (durMs < 120f)
            {
                baseEnd = Math.Min(baseEnd, durMs * 0.3f);
            }

            switch (shape)
            {
                case PitchShapeType.Thunderbolt:
                    // 1. Thunderbolt: ปรับสมดุล Accent (+5 cents, -3 cents) เฉพาะโน้ตที่ไม่สั้นเกินไป
                    if (durMs >= 160f)
                    {
                        note.pitch.AddPoint(new PitchPoint(-35f, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(0f, 0.5f, PitchPointShape.io));     // +5 cents accent
                        note.pitch.AddPoint(new PitchPoint(25f, -0.3f, PitchPointShape.io));  // -3 cents micro-dip
                        note.pitch.AddPoint(new PitchPoint(50f, 0, PitchPointShape.io));       // เสถียรบนคีย์
                    }
                    else
                    {
                        note.pitch.AddPoint(new PitchPoint(baseStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(baseEnd, 0, PitchPointShape.io));
                    }
                    break;

                case PitchShapeType.PitchDropRise:
                    // 2. Pitch Drop/Rise: ช้อนเสียงหัวโน้ตนุ่มนวล (-8 cents)
                    if (ctx.IsPhraseStart && durMs >= 150f)
                    {
                        note.pitch.AddPoint(new PitchPoint(-25f, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(0f, -0.8f, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(35f, 0, PitchPointShape.io));
                    }
                    else
                    {
                        note.pitch.AddPoint(new PitchPoint(baseStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(baseEnd, 0, PitchPointShape.io));
                    }

                    if ((ctx.IsPhraseEnd || ctx.HasGapAfter) && durMs >= 200f)
                    {
                        float dropStart = Math.Max(50f, durMs - 50f);
                        note.pitch.AddPoint(new PitchPoint(dropStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(durMs, -1.0f, PitchPointShape.io));
                    }
                    break;

                case PitchShapeType.Slope:
                default:
                    // 3. Slope: S-Curve เชื่อมต่อโน้ตนุ่มนวลแบบ Legato
                    int absDelta = Math.Abs(ctx.PitchDeltaFromPrev);
                    float portStart = absDelta >= 5 ? -50f : (absDelta >= 3 ? -38f : -28f);
                    float portEnd = absDelta >= 5 ? 30f : (absDelta >= 3 ? 22f : 16f);
                    if (durMs < 120f) portEnd = Math.Min(portEnd, durMs * 0.28f);

                    note.pitch.AddPoint(new PitchPoint(portStart, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(portEnd, 0, PitchPointShape.io));
                    break;

                case PitchShapeType.WShape:
                    // 4. W-Shape: คลื่นละมุนเฉพาะช่วงท้ายของโน้ตยาว (±4-5 cents)
                    note.pitch.AddPoint(new PitchPoint(baseStart, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(baseEnd, 0, PitchPointShape.io));

                    if (durMs >= 280f)
                    {
                        float wStart = durMs - 120f;
                        note.pitch.AddPoint(new PitchPoint(wStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(wStart + 30f, -0.5f, PitchPointShape.io)); // -5 cents
                        note.pitch.AddPoint(new PitchPoint(wStart + 60f, 0.3f, PitchPointShape.io));  // +3 cents
                        note.pitch.AddPoint(new PitchPoint(wStart + 90f, -0.4f, PitchPointShape.io)); // -4 cents
                        note.pitch.AddPoint(new PitchPoint(durMs, 0, PitchPointShape.io));
                    }
                    break;

                case PitchShapeType.UPitchDrop:
                    // 5. U-Pitch Drop: ผ่อนคลายท้ายประโยค (-12 cents)
                    note.pitch.AddPoint(new PitchPoint(baseStart, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(baseEnd, 0, PitchPointShape.io));

                    if (durMs >= 200f)
                    {
                        float uStart = Math.Max(30f, durMs - 75f);
                        note.pitch.AddPoint(new PitchPoint(uStart, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(durMs - 30f, -1.2f, PitchPointShape.io)); // -12 cents
                        note.pitch.AddPoint(new PitchPoint(durMs, -0.6f, PitchPointShape.io));
                    }
                    break;

                case PitchShapeType.PitchDent:
                    // 6. Pitch Dent: รอยเว้าเน้นคำช่วงกลางโน้ตอย่างละมุน (-6 cents)
                    note.pitch.AddPoint(new PitchPoint(baseStart, 0, PitchPointShape.io));
                    note.pitch.AddPoint(new PitchPoint(baseEnd, 0, PitchPointShape.io));

                    if (durMs >= 240f)
                    {
                        float dentMid = durMs * 0.45f;
                        note.pitch.AddPoint(new PitchPoint(dentMid - 20f, 0, PitchPointShape.io));
                        note.pitch.AddPoint(new PitchPoint(dentMid, -0.6f, PitchPointShape.io)); // -6 cents
                        note.pitch.AddPoint(new PitchPoint(dentMid + 20f, 0, PitchPointShape.io));
                    }
                    break;
            }

            if (applyNaturalVibrato)
            {
                if (note.duration >= 480 && durMs >= 350f)
                {
                    note.vibrato = new UVibrato
                    {
                        length = 65f,
                        period = 17.0f,
                        depth = 25f,
                        @in = 30f,
                        @out = 15f,
                        shift = 0f,
                        drift = 0f
                    };
                }
                else
                {
                    note.vibrato.length = 0f;
                }
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
                    docManager.ExecuteCmd(new SetVibratoCommand(part, note, note.vibrato));
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
