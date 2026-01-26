using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LoGeCuiShared.Services
{
    public sealed class SupabaseRestClient
    {
        private readonly HttpClient _http;
        private readonly string _baseRestUrl;

        public SupabaseRestClient(string supabaseUrl, string supabaseAnonKey)
        {
            if (string.IsNullOrWhiteSpace(supabaseUrl)) throw new ArgumentException("supabaseUrl is required");
            if (string.IsNullOrWhiteSpace(supabaseAnonKey)) throw new ArgumentException("supabaseAnonKey is required");

            _baseRestUrl = supabaseUrl.TrimEnd('/') + "/rest/v1/";

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetBearerToken(string accessToken)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<T?> GetAsync<T>(string pathAndQuery)
        {
            var url = _baseRestUrl + pathAndQuery;

            using var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Supabase GET failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            return JsonSerializer.Deserialize<T>(body, JsonOptions());
        }

        public async Task<T?> PostAsync<T>(string pathAndQuery, object payload, bool returnRepresentation = false)
        {
            var url = _baseRestUrl + pathAndQuery;
            var json = JsonSerializer.Serialize(payload, JsonOptions());

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (returnRepresentation)
                req.Headers.Add("Prefer", "return=representation");

            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Supabase POST failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            if (string.IsNullOrWhiteSpace(body))
                return default;

            return JsonSerializer.Deserialize<T>(body, JsonOptions());
        }

        public async Task PatchAsync(string pathAndQuery, object payload)
        {
            var url = _baseRestUrl + pathAndQuery;
            var json = JsonSerializer.Serialize(payload, JsonOptions());

            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Supabase PATCH failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        public async Task DeleteAsync(string pathAndQuery)
        {
            var url = _baseRestUrl + pathAndQuery;

            using var resp = await _http.DeleteAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Supabase DELETE failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<T?> PostAsync<T>(
        string pathAndQuery,
        object payload,
        bool returnRepresentation = false,
        bool mergeDuplicates = false)
        {
            var url = _baseRestUrl + pathAndQuery;
            var json = JsonSerializer.Serialize(payload, JsonOptions());

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // PostgREST Prefer headers
            if (returnRepresentation)
                req.Headers.Add("Prefer", "return=representation");

            if (mergeDuplicates)
                req.Headers.Add("Prefer", "resolution=merge-duplicates");

            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Supabase POST failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

            if (string.IsNullOrWhiteSpace(body))
                return default;

            return JsonSerializer.Deserialize<T>(body, JsonOptions());
        }

    }
}

