using System;
using System.Linq;
using System.Threading.Tasks;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LoGeCuiShared.Services
{
    public class UpdateService
    {
        private readonly Client _client;

        public UpdateService(string url, string key)
        {
            var options = new ClientOptions
            {
                Headers = new Dictionary<string, string>
                {
                    { "apikey", key },
                    { "Authorization", $"Bearer {key}" }
                }
            };

            _client = new Client($"{url}/rest/v1", options);
        }

        public async Task<AppVersionInfo?> CheckForUpdateAsync(string platform, string currentVersion)
        {
            try
            {
                var response = await _client
                    .Table<AppVersionDb>()
                    .Filter("platform", Supabase.Postgrest.Constants.Operator.Equals, platform)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var latestVersion = response.Models.FirstOrDefault();

                if (latestVersion == null)
                    return null;

                // Comparer les versions
                if (IsNewerVersion(latestVersion.Version, currentVersion))
                {
                    return new AppVersionInfo
                    {
                        Version = latestVersion.Version ?? "",
                        DownloadUrl = latestVersion.DownloadUrl ?? "",
                        IsMandatory = latestVersion.IsMandatory,
                        ReleaseNotes = latestVersion.ReleaseNotes ?? ""
                    };
                }

                return null; // Pas de mise à jour disponible
            }
            catch
            {
                return null;
            }
        }

        private bool IsNewerVersion(string? latestVersion, string currentVersion)
        {
            if (string.IsNullOrEmpty(latestVersion))
                return false;

            try
            {
                var latest = new Version(latestVersion);
                var current = new Version(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }
    }

    // Modèle pour la table app_versions
    [Table("app_versions")]
    public class AppVersionDb : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("platform")]
        public string? Platform { get; set; }

        [Column("version")]
        public string? Version { get; set; }

        [Column("download_url")]
        public string? DownloadUrl { get; set; }

        [Column("is_mandatory")]
        public bool IsMandatory { get; set; }

        [Column("release_notes")]
        public string? ReleaseNotes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    // Classe pour retourner les infos de version
    public class AppVersionInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public bool IsMandatory { get; set; }
        public string ReleaseNotes { get; set; } = "";
    }
}
