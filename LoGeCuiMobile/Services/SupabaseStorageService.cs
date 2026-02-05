using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LoGeCuiShared.Services;

namespace LoGeCuiMobile.Services
{
    public class SupabaseStorageService
    {
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private const string Bucket = "recipe-photos";

        // ✅ réutiliser HttpClient (évite sockets leak)
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        public SupabaseStorageService()
        {
            _supabaseUrl = ConfigurationHelper.GetSupabaseUrl().TrimEnd('/');
            _supabaseKey = ConfigurationHelper.GetSupabaseKey();
        }

        /// <summary>
        /// Upload une photo dans Supabase Storage et retourne l'URL publique.
        /// Bucket = recipe-photos (public).
        /// </summary>
        public async Task<string> UploadRecipePhotoAndGetPublicUrlAsync(
            string accessToken,
            Guid userId,
            Guid recetteId,
            string localPath)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Token manquant. Reconnecte-toi.");

            if (!File.Exists(localPath))
                throw new FileNotFoundException("Photo introuvable", localPath);

            var ext = Path.GetExtension(localPath);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            // Range tes fichiers par user -> plus simple pour les policies
            var objectPath = $"{userId}/recipes/{recetteId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";

            var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{Bucket}/{objectPath}";
            var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{Bucket}/{objectPath}";

            await using var stream = File.OpenRead(localPath);

            using var req = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            req.Headers.Add("apikey", _supabaseKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Permet d'écraser si le fichier existe déjà (selon comportement Storage)
            req.Headers.Add("x-upsert", "true");

            req.Content = new StreamContent(stream);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(ext));

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Upload Storage failed ({(int)res.StatusCode}): {body}");

            return publicUrl;
        }

        private static string GetMimeType(string ext)
        {
            ext = (ext ?? "").ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }
    }
}
