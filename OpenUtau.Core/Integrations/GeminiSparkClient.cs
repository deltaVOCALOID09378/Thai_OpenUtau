// ========================================================================
// Date: 05/10/2569 (BE) / 05/10/2026 (CE)
// File: GeminiSparkClient.cs
// Version: 1.0.0
// Made And Checked By DELTA SYNTH & All Code Agentic AI Engine
// Original by OpenUtau Contributors
// Location: OpenUtau.Core/Integrations/GeminiSparkClient.cs
// Description: Basic framework for connecting OpenUtau data with Gemini Spark.
// ========================================================================

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace OpenUtau.Core.Integrations {
    /// <summary>
    /// คลาสสำหรับเชื่อมต่อข้อมูลกับ Gemini Spark
    /// ใช้เป็น Framework เริ่มต้นสำหรับการส่งคำขอและรับข้อมูลกลับมาวิเคราะห์
    /// </summary>
    public class GeminiSparkClient {
        private static readonly string API_ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
        private readonly HttpClient _httpClient;
        private string _apiKey;

        public GeminiSparkClient(string apiKey) {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }

        /// <summary>
        /// ตั้งค่า API Key ใหม่
        /// </summary>
        public void SetApiKey(string apiKey) {
            _apiKey = apiKey;
        }

        /// <summary>
        /// ฟังก์ชันทดสอบการเชื่อมต่อและส่งข้อความไปยัง Gemini Spark
        /// </summary>
        /// <param name="prompt">ข้อความคำสั่งที่ต้องการส่ง</param>
        /// <returns>ข้อความตอบกลับจาก Gemini Spark</returns>
        public async Task<string> GenerateContentAsync(string prompt) {
            if (string.IsNullOrWhiteSpace(_apiKey)) {
                Log.Error("GeminiSparkClient: API Key is not set.");
                throw new InvalidOperationException("กรุณากำหนด API Key ก่อนเรียกใช้งาน Gemini Spark");
            }

            try {
                var requestBody = new {
                    contents = new[] {
                        new {
                            parts = new[] {
                                new { text = prompt }
                            }
                        }
                    }
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                string requestUrl = $"{API_ENDPOINT}?key={_apiKey}";
                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);

                response.EnsureSuccessStatusCode();

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic responseData = JsonConvert.DeserializeObject(responseJson);

                if (responseData?.candidates != null && responseData.candidates.Count > 0) {
                    string replyText = responseData.candidates[0].content.parts[0].text;
                    return replyText;
                }

                return string.Empty;
            }
            catch (Exception ex) {
                Log.Error(ex, "GeminiSparkClient: Failed to generate content from Gemini Spark.");
                throw new Exception($"เกิดข้อผิดพลาดในการเชื่อมต่อ Gemini Spark: {ex.Message}", ex);
            }
        }
    }
}
