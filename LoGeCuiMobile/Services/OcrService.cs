using System.Net.Http.Headers;
using System.Text.Json;

namespace LoGeCuiMobile.Services
{
    public sealed class OcrService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public OcrService(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task<string> ExtractTextFromImageAsync(byte[] imageBytes, string fileName = "scan.jpg")
        {
            if (imageBytes is null || imageBytes.Length == 0)
                throw new ArgumentException("Image vide.", nameof(imageBytes));

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Clé OCR manquante (apiKey).");

            var url = "https://api.ocr.space/parse/image";

            using var content = new MultipartFormDataContent();

            // Paramètres utiles
            content.Add(new StringContent("fre"), "language");          // fre = français
            content.Add(new StringContent("false"), "isOverlayRequired");
            content.Add(new StringContent("2"), "OCREngine");           // 1 ou 2 (2 souvent meilleur)
            content.Add(new StringContent("true"), "scale");
            content.Add(new StringContent("true"), "detectOrientation");

            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(fileContent, "file", fileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _apiKey); // ✅ endroit le plus fiable
            request.Content = content;

            using var resp = await _http.SendAsync(request);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"OCR HTTP {(int)resp.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);

            // Si l’API retourne une erreur applicative
            if (doc.RootElement.TryGetProperty("IsErroredOnProcessing", out var errored)
                && errored.ValueKind == JsonValueKind.True)
            {
                string msg = "Erreur OCR.";
                if (doc.RootElement.TryGetProperty("ErrorMessage", out var em))
                {
                    // ErrorMessage peut être string ou tableau selon les cas
                    msg = em.ValueKind switch
                    {
                        JsonValueKind.Array => string.Join(" | ", em.EnumerateArray().Select(x => x.ToString())),
                        _ => em.ToString()
                    };
                }
                throw new Exception(msg);
            }

            if (!doc.RootElement.TryGetProperty("ParsedResults", out var parsedResults)
                || parsedResults.ValueKind != JsonValueKind.Array
                || parsedResults.GetArrayLength() == 0)
            {
                return "";
            }

            var first = parsedResults[0];
            if (!first.TryGetProperty("ParsedText", out var parsedTextEl))
                return "";

            return parsedTextEl.GetString() ?? "";
        }
    }
}


