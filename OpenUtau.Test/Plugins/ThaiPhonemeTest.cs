// Made And Checked By DELTA SYNTH & Gemini AI
// Original by OpenUtau

using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {

    // ==========================================
    // 1. ระบบทดสอบสำหรับ THAI VCCV
    // ==========================================
    public class ThaiVCCVComprehensiveTest : PhonemizerTestBase {
        public ThaiVCCVComprehensiveTest(ITestOutputHelper output) : base(output) { }
        
        protected override Phonemizer CreatePhonemizer() {
            return new ThaiVCCVPhonemizer();
        }

        [Theory]
        [InlineData("th_vccv", new string[] { "สวรรค์", "บรรณ", "ธรรม", "พรรค" }, 
            new string[] { "sa", "am", "ba", "an", "tha", "am", "pha", "ak" })]
        [InlineData("th_vccv", new string[] { "จันทรา", "ปรารถนา", "ปราถนา" }, 
            new string[] { "ja", "pr", "ra", "an", "na", "pr", "ra" })]
        [InlineData("th_vccv", new string[] { "ป่วน", "เหลือ", "โสน", "ขวัญ", "หนาม", "อย่า" }, 
            new string[] { "pua", "uan", "l6", "sa", "khua", "uan", "na", "am", "ya" })]
        [InlineData("th_vccv", new string[] { "บ่", "ธ", "ณ" }, 
            new string[] { "bQ", "tha", "an", "na" })]
        [InlineData("th_vccv", new string[] { "คึก", "ฤทธิ์", "ฤกดิ์", "ฤาษี", "พลัง", "บวร" }, 
            new string[] { "kh1", "1k", "ri", "r3", "3k", "r1", "phl", "la", "ag", "ba", "an" })]
        [InlineData("th_vccv", new string[] { "กัน", "นก", "ขวด", "เด็ก", "แข็ง", "เดิน", "เลย", "ก็" }, 
            new string[] { "ka", "an", "no", "ok", "khua", "uad", "de", "ek", "kh@", "@g", "de", "l3", "3y", "kQ" })]
        public void ComplexThaiPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }

    // ==========================================
    // 2. ระบบทดสอบสำหรับ THAI VCV

    // ==========================================
    // 3. ระบบทดสอบสำหรับ THAI VCV
    // ==========================================
    public class ThaiVCVTest : PhonemizerTestBase {
        public ThaiVCVTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new ThaiVCVPhonemizer();
        }

        [Theory]
        [InlineData("th_vcv", new string[] { "บ่น", "วัว", "เบียบ", "เสื่อม", "พราหม์", "พรหม", "สวรรค์", "จันทรา" }, 
            new string[] { "bon", "wua", "be", "ียob", "s6m", "phraห", "พรหม", "swan", "jan", "ra" })]
        public void VCVPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }

    // ==========================================
    // 4. ระบบทดสอบสำหรับ THAI C+V (CV)
    // ==========================================
    public class ThaiCVTest : PhonemizerTestBase {
        public ThaiCVTest(ITestOutputHelper output) : base(output) { }

        protected override Phonemizer CreatePhonemizer() {
            return new ThaiCpVPhonemizer();
        }

        [Theory]
        [InlineData("th_cv", new string[] { "บ่น", "ก้น", "เบย", "เมย", "มวย", "ควัน", "พราหม์", "พรหม" }, 
            new string[] { "b", "Q", "n", "k", "o", "n", "b", "3", "y", "m", "3", "y", "m", "ua", "y", "kh", "w", "a", "n", "ph", "r", "a", "ph", "r", "o", "-" })]
        public void CVPhonemizingTest(string singerName, string[] lyrics, string[] aliases) {
            SameAltsTonesColorsTest(singerName, lyrics, aliases, "", "C4", "");
        }
    }

} // สิ้นสุด Namespace OpenUtau.Plugins
