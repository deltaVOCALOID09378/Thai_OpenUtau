// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.0 | Date: 2026-07-28
// Description: ยืนยันความเข้ากันได้ของ PhonemizerAttribute แบบ 4 และ 5 พารามิเตอร์
// ============================================================================

using Xunit;

namespace OpenUtau.Api {
    public class PhonemizerAttributeTest {
        [Fact]
        public void FourParameterConstructorPreservesMetadata() {
            var attribute = new PhonemizerAttribute(
                "Thai VCCV",
                "TH VCCV",
                "DELTA SYNTH",
                "TH");

            Assert.Equal("Thai VCCV", attribute.Name);
            Assert.Equal("TH VCCV", attribute.Tag);
            Assert.Equal("DELTA SYNTH", attribute.Author);
            Assert.Equal("TH", attribute.Language);
        }

        [Fact]
        public void FiveParameterCompatibilityConstructorPreservesSupportedMetadata() {
            var attribute = new PhonemizerAttribute(
                "Thai VCCV",
                "TH VCCV",
                "DELTA SYNTH",
                "TH",
                null);

            Assert.Equal("Thai VCCV", attribute.Name);
            Assert.Equal("TH VCCV", attribute.Tag);
            Assert.Equal("DELTA SYNTH", attribute.Author);
            Assert.Equal("TH", attribute.Language);
        }
    }
}
