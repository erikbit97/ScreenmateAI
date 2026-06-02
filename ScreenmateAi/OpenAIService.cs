using ScreenmateAi;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ChatGPTWPF.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey; //Hier deinen OpenAI API-Key eintragen        

        public OpenAIService()
        {
            _httpClient = new HttpClient();
            _apiKey = File.ReadAllText("apikey.txt").Trim();
        }

        public async Task<string> SendMessageAsync(List<ChatMessage> messages)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestBody = new
            {
                model = "gpt-4.1-mini",
                input = messages.Select(m => new
                {
                    role = m.Role,
                    content = new[]
                    {
            new
            {
                type = m.Role == "assistant" ? "output_text" : "input_text",
                text = m.Content
            }
        }
                })
            };

            string json = JsonSerializer.Serialize(requestBody);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/responses",
                content
            );

            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return "Fehler von der API: " + responseText;
            }

            using JsonDocument document = JsonDocument.Parse(responseText);

            // Variante 1: direkte Kurzantwort
            if (document.RootElement.TryGetProperty("output_text", out JsonElement outputText))
            {
                string? text = outputText.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            // Variante 2: Antwort steckt in output -> content -> text
            if (document.RootElement.TryGetProperty("output", out JsonElement outputArray))
            {
                foreach (JsonElement outputItem in outputArray.EnumerateArray())
                {
                    if (outputItem.TryGetProperty("content", out JsonElement contentArray))
                    {
                        foreach (JsonElement contentItem in contentArray.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("text", out JsonElement textElement))
                            {
                                string? text = textElement.GetString();

                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    return text;
                                }
                            }
                        }
                    }
                }
            }

            return "Keine Antwort gefunden. Rohantwort: " + responseText;
        }

        public async Task<string> SendMessageWithImagesAndHistoryAsync(
     List<ChatMessage> messages,
     List<string> imagePaths)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            List<object> inputMessages = new List<object>();

            for (int i = 0; i < messages.Count; i++)
            {
                ChatMessage message = messages[i];

                bool isLastUserMessage =
                    i == messages.Count - 1 && message.Role == "user";

                List<object> contentParts = new List<object>();

                contentParts.Add(new
                {
                    type = message.Role == "assistant" ? "output_text" : "input_text",
                    text =  isLastUserMessage
                    ? message.Content +
                    "\n\nHinweis: Die angehängten Bilder sind Screenshots aus dem Live-Modus." +
                    " Sie sind zeitlich sortiert." +
                    " Das letzte Bild ist der aktuellste Screenshot." +
                    " Wenn sich die Frage auf 'jetzt', 'aktuell', 'hier' oder 'das Bild' bezieht," +
                    " verwende vor allem das letzte Bild."
                    : message.Content
                });

                if (isLastUserMessage)
                {
                    foreach (string imagePath in imagePaths)
                    {
                        byte[] imageBytes = File.ReadAllBytes(imagePath);

                        string base64Image = Convert.ToBase64String(imageBytes);

                        string imageUrl = $"data:image/png;base64,{base64Image}";

                        contentParts.Add(new
                        {
                            type = "input_image",
                            image_url = imageUrl,
                            detail = "low"
                        });
                    }
                }

                inputMessages.Add(new
                {
                    role = message.Role,
                    content = contentParts.ToArray()
                });
            }

            var requestBody = new
            {
                model = "gpt-4.1-mini",
                input = inputMessages.ToArray()
            };

            string json = JsonSerializer.Serialize(requestBody);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/responses",
                content
            );

            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return "Fehler von der API: " + responseText;
            }

            return ExtractAnswer(responseText);
        }

        private string ExtractAnswer(string responseText)
        {
            using JsonDocument document = JsonDocument.Parse(responseText);

            if (document.RootElement.TryGetProperty("output_text", out JsonElement outputText))
            {
                string? text = outputText.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            if (document.RootElement.TryGetProperty("output", out JsonElement outputArray))
            {
                foreach (JsonElement outputItem in outputArray.EnumerateArray())
                {
                    if (outputItem.TryGetProperty("content", out JsonElement contentArray))
                    {
                        foreach (JsonElement contentItem in contentArray.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("text", out JsonElement textElement))
                            {
                                string? text = textElement.GetString();

                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    return text;
                                }
                            }
                        }
                    }
                }
            }

            return "Keine Antwort gefunden. Rohantwort: " + responseText;
        }
    }
}
