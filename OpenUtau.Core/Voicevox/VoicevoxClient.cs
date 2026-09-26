using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Serilog;

// ============================================================================
// Made And Checked By DELTA SYNTH & Gemini AI
// Original by Patiphat Wongyai (Delta)
// Version: 1.2 | Date: 2026-07-28
// Description: ใช้ HTTP client ร่วม ลดการเปิดซ็อกเก็ตซ้ำ และเข้ารหัสพารามิเตอร์ URL
// ============================================================================

namespace OpenUtau.Core.Voicevox {
    class VoicevoxClient : Util.SingletonBase<VoicevoxClient> {
        private static readonly HttpClient client = new HttpClient {
            Timeout = TimeSpan.FromSeconds(30),
        };

        internal Tuple<string, byte[]> SendRequest(VoicevoxURL voicevoxURL) {
            try {
                using var request = new HttpRequestMessage(
                    new HttpMethod(voicevoxURL.method.ToUpperInvariant()),
                    RequestURL(voicevoxURL));
                request.Headers.TryAddWithoutValidation("accept", voicevoxURL.accept);
                request.Content = new StringContent(
                    voicevoxURL.body,
                    Encoding.UTF8,
                    "application/json");

                Log.Information("Voicevox request: {Method} {Url}", request.Method, request.RequestUri);
                using var response = client.SendAsync(request)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                byte[] responseBytes = response.Content.ReadAsByteArrayAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                string responseText = Encoding.UTF8.GetString(responseBytes);
                // May not fit JSON format.
                if (!responseText.StartsWith("{") || !responseText.EndsWith("}")) {
                    responseText = "{ \"json\":" + responseText + "}";
                }
                Log.Information("Voicevox response status: {StatusCode}", response.StatusCode);
                return Tuple.Create(responseText, responseBytes);
            } catch (Exception ex) {
                Log.Error(ex, "Voicevox request failed.");
            }
            return Tuple.Create(string.Empty, Array.Empty<byte>());
        }

        public string RequestURL(VoicevoxURL voicevoxURL) {
            StringBuilder queryStringBuilder = new StringBuilder();
            foreach (var parameter in voicevoxURL.query) {
                queryStringBuilder.Append(Uri.EscapeDataString(parameter.Key));
                queryStringBuilder.Append('=');
                queryStringBuilder.Append(Uri.EscapeDataString(parameter.Value));
                queryStringBuilder.Append('&');
            }

            string baseUrl = $"{voicevoxURL.protocol}{voicevoxURL.host}{voicevoxURL.path}";
            if (queryStringBuilder.Length == 0) {
                return baseUrl;
            }

            // Remove the extra "&" at the end.
            return $"{baseUrl}?{queryStringBuilder.ToString().TrimEnd('&')}";
        }
    }
    public class VoicevoxURL {
        public string method = string.Empty;
        public string protocol = "http://";
        //Currently fixed port 50021 to connect to
        public string host = "127.0.0.1:50021";
        public string path = string.Empty;
        public Dictionary<string, string> query = new Dictionary<string, string>();
        public string body = string.Empty;
        public string accept = "application/json";
    }
}
