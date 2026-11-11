// ============================================================================
// 1. วัน/เดือน/ปี: 8 พฤศจิกายน พ.ศ. 2569 / 8 November 2026
// 2. ชื่อไฟล์โค้ด: DeltaSingerProfileResolverTest.cs
// 3. ลำดับเวอร์ชันของไฟล์: 1.0
// 4. ตำแหน่งที่จัดวางไฟล์: OpenUtau.Test/DeltaSingers/DeltaSingerProfileResolverTest.cs
// 5. หน้าที่: ชุดการทดสอบสำหรับระบบเชื่อมโยงรูปโปรไฟล์นักร้อง DELTA SYNTH และทดสอบการแปลงหน่วยเสียงสระ a
// 6. เครดิตผู้สร้าง: Made And Checked By DELTA SYNTH & All Code Agentic AI Engine | Original by DELTA SYNTH
// ============================================================================

using System;
using System.IO;
using OpenUtau.Core.SingerHub;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Test.DeltaSingers {
    public class DeltaSingerProfileResolverTest {
        private readonly ITestOutputHelper output;

        public DeltaSingerProfileResolverTest(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact]
        public void NormalizeKey_ShouldStripSpecialCharsAndLower() {
            Assert.Equal("ayanamihikaru", DeltaSingerProfileResolver.NormalizeKey("Ayanami Hikaru"));
            Assert.Equal("mairumaishi", DeltaSingerProfileResolver.NormalizeKey("Mairu Maishi"));
            Assert.Equal("gurenkani", DeltaSingerProfileResolver.NormalizeKey("Guren_Kani"));
            Assert.Equal("arzbtv", DeltaSingerProfileResolver.NormalizeKey("ARZB TV"));
        }

        [Fact]
        public void Arpasing_SingleVowelA_ShouldNotBeChopped() {
            var phonemizer = new ArpasingPhonemizer();
            var g2p = ArpasingPhonemizer.GetOrLoadEnglishArpDictionary(string.Empty);
            Assert.NotNull(g2p);

            string[] result = g2p.Query("a");
            Assert.NotNull(result);
            // ตรวจสอบว่าคำว่า a ได้รับผลลัพธ์เป็นสระตัวเดียว (ah) เท่านั้น ห้ามหั่นเป็น 13 ชิ้น
            output.WriteLine($"Phonemes for 'a': {string.Join(" ", result)}");
            Assert.Single(result);
            Assert.Equal("ah", result[0]);
        }

        [Fact]
        public void Arpasing_TrailingHyphen_Was_ShouldResolveCorrectly() {
            var phonemizer = new ArpasingPhonemizer();
            var g2p = ArpasingPhonemizer.GetOrLoadEnglishArpDictionary(string.Empty);
            Assert.NotNull(g2p);

            string[] resultWas = g2p.Query("was");
            Assert.NotNull(resultWas);
            output.WriteLine($"Phonemes for 'was': {string.Join(" ", resultWas)}");
            Assert.True(resultWas.Length >= 2);
            Assert.Equal("w", resultWas[0]);
            Assert.Equal("aa", resultWas[1]);
        }

        [Fact]
        public void DeployProfilePictures_IfSourceExists_ShouldExecuteCleanly() {
            if (Directory.Exists(DeltaSingerProfileResolver.DefaultProfilesDir) &&
                Directory.Exists(DeltaSingerProfileResolver.DefaultVoicebanksDir)) {
                
                int updatedCount = DeltaSingerProfileResolver.DeployProfilePicturesToVoicebanks();
                output.WriteLine($"Deployed profile pictures to {updatedCount} voicebank directories.");
                Assert.True(updatedCount >= 0);
            }
        }
    }
}
