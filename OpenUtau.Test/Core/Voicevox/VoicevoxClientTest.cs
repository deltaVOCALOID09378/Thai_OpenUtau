// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.2 | Date: 2026-07-28
// Description: ตรวจการสร้าง URL ของ VOICEVOX ให้ปลอดภัยต่อภาษาไทยและอักขระพิเศษ
// ============================================================================

using System.Collections.Generic;
using Xunit;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxClientTest {
        [Fact]
        public void RequestUrlEncodesQueryParameters() {
            var request = new VoicevoxURL {
                protocol = "http://",
                host = "127.0.0.1:50021",
                path = "/audio_query",
                query = new Dictionary<string, string> {
                    ["text"] = "สวัสดี & hello",
                    ["speaker"] = "1",
                },
            };

            string actual = VoicevoxClient.Inst.RequestURL(request);

            Assert.Equal(
                "http://127.0.0.1:50021/audio_query" +
                "?text=%E0%B8%AA%E0%B8%A7%E0%B8%B1%E0%B8%AA%E0%B8%94%E0%B8%B5%20%26%20hello" +
                "&speaker=1",
                actual);
        }

        [Fact]
        public void RequestUrlOmitsQuestionMarkWithoutParameters() {
            var request = new VoicevoxURL {
                path = "/engine_manifest",
            };

            string actual = VoicevoxClient.Inst.RequestURL(request);

            Assert.Equal("http://127.0.0.1:50021/engine_manifest", actual);
        }
    }
}
