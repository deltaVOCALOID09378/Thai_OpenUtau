// ============================================================================
// 1. วัน/เดือน/ปี: 6 พฤศจิกายน พ.ศ. 2569 / 6 November 2026
// 2. ชื่อไฟล์โค้ด: LaosVCCVPhonemizer.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.0
// 4. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine [ Original by DELTA SYNTH ]
// 5. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Plugin.Builtin/LaosVCCVPhonemizer.cs
// 6. รายละเอียดการแก้ไข: ลงทะเบียน Lao VCCV Phonemizer (ภาษา LO) สืบทอดคุณสมบัติจาก Thai VCCV ครบถ้วน
// ============================================================================

#pragma warning disable CS0618, CS0649, CS8632, CS0108
using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Lao VCCV Phonemizer", "LO VCCV", "DELTA SYNTH", language: "LO")]
    public class LaoVCCVPhonemizer : ThaiVCCVPhonemizer {
        // Inherits all logic from Thai VCCV but registers as Lao (LO)
    }
}
