// ========================================================================
// Date: 30/10/2569 (BE) / 30/10/2026 (CE)
// File: GeminiSparkClient.cs
// Version: 1.3.0
// Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
// Original by OpenUtau Contributors
// Location: OpenUtau.Core/Integrations/GeminiSparkClient.cs
// Description: Typed framework for connecting OpenUtau data with Gemini Spark.
// Modification: Fixed multiline string literal missing '@' prefix.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.Integrations {
    public class GeminiPart {
        [JsonProperty("text")]
        public string? Text { get; set; }
    }

    public class GeminiContent {
        [JsonProperty("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    public class GeminiCandidate {
        [JsonProperty("content")]
        public GeminiContent? Content { get; set; }
    }

    public class GeminiResponse {
        [JsonProperty("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    /// <summary>
    /// คลาสสำหรับเชื่อมต่อข้อมูลกับ Gemini Spark
    /// ใช้ Static Singleton HttpClient และ Typed Models เพื่อความเสถียรสูงสุด
    /// </summary>
    public class GeminiSparkClient {
        private static readonly string API_ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
        private static readonly HttpClient SharedHttpClient = new HttpClient {
            Timeout = TimeSpan.FromSeconds(30)
        };
        private string _apiKey;

        public GeminiSparkClient(string apiKey) {
            _apiKey = apiKey;
        }

        public void SetApiKey(string apiKey) {
            _apiKey = apiKey;
        }

        public async Task<string> AnalyzeProjectDataAsync(string promptText, string projectContextJson) {
            if (string.IsNullOrWhiteSpace(_apiKey)) {
                Log.Warning("GeminiSparkClient: API Key is missing.");
                return "Error: Missing API Key.";
            }

            try {
                var requestUrl = $"{API_ENDPOINT}?key={_apiKey}";
                var payload = new {
                    contents = new[] {
                        new {
                            parts = new[] {
                                new { text = $@"{promptText}

Context Data:
{projectContextJson}" }
                            }
                        }
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await SharedHttpClient.PostAsync(requestUrl, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsedResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseString);

                string? resultText = parsedResponse?.Candidates != null &&
                                     parsedResponse.Candidates.Count > 0 &&
                                     parsedResponse.Candidates[0].Content?.Parts != null &&
                                     parsedResponse.Candidates[0].Content!.Parts!.Count > 0
                    ? parsedResponse.Candidates[0].Content!.Parts![0].Text
                    : null;

                return resultText ?? "No response text generated.";
            } catch (Exception ex) {
                Log.Error(ex, "GeminiSparkClient: Failed to communicate with Gemini Spark API.");
                return $"Error communicating with Gemini Spark: {ex.Message}";
            }
        }
    }
}