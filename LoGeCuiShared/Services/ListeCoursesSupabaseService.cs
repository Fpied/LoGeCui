using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supabase;

namespace LoGeCuiShared.Services
{
    public sealed class ListeCoursesSupabaseService
    {
        private readonly SupabaseRestClient _client;
        private const string Table = "articles_courses";

        public ListeCoursesSupabaseService(SupabaseRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        private static bool IsShared(Guid? listId) => listId.HasValue && listId.Value != Guid.Empty;

        // =========================
        // GET
        // =========================
        public async Task<List<ArticleCourseRow>> GetArticlesAsync(Guid userId, Guid? listId = null)
        {
            var q = IsShared(listId)
                ? $"{Table}?select=id,created_at,nom,quantite,unite,est_achete,user_id,list_id" +
                  $"&list_id=eq.{listId!.Value}" +
                  "&order=created_at.desc"
                : $"{Table}?select=id,created_at,nom,quantite,unite,est_achete,user_id,list_id" +
                  $"&user_id=eq.{userId}" +
                  "&order=created_at.desc";

            return await _client.GetAsync<List<ArticleCourseRow>>(q) ?? new List<ArticleCourseRow>();
        }

        // =========================
        // INSERT
        // ✅ IMPORTANT: on envoie user_id explicitement => évite 403 RLS
        // =========================
        public async Task<ArticleCourseRow?> AddArticleAsync(Guid userId, string nom, string? quantite, string? unite, Guid? listId = null)
        {
            if (string.IsNullOrWhiteSpace(nom))
                throw new ArgumentException("nom requis", nameof(nom));

            if (listId == null || listId == Guid.Empty)
                throw new InvalidOperationException("Aucune liste active (list_id requis).");

            var payloadRow = new
            {
                user_id = userId,
                list_id = listId.Value,
                nom = nom.Trim(),
                quantite = quantite,
                unite = unite,
                est_achete = false
            };

            var created = await _client.PostAsync<List<ArticleCourseRow>>(
                $"{Table}?select=id,created_at,nom,quantite,unite,est_achete,user_id,list_id",
                new[] { payloadRow },
                returnRepresentation: true);

            return created?.FirstOrDefault();
        }


        // =========================
        // UPDATE (PATCH)
        // =========================
        public async Task UpdateArticleAsync(long id, bool estAchete)
        {
            var q = $"{Table}?id=eq.{id}";
            var payload = new { est_achete = estAchete };
            await _client.PatchAsync(q, payload);
        }

        // =========================
        // DELETE
        // =========================
        public async Task DeleteArticleAsync(long id)
        {
            await _client.DeleteAsync($"{Table}?id=eq.{id}");
        }

        // =========================
        // ADD MISSING (batch)
        // ✅ Envoie user_id + list_id si partagé
        // =========================
        public async Task AddMissingAsync(Guid userId, IEnumerable<string> missingNames, Guid? listId = null)
        {
            if (listId == null || listId == Guid.Empty)
                throw new InvalidOperationException("Aucune liste active (list_id requis).");

            var names = (missingNames ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count == 0)
                return;

            bool shared = IsShared(listId);

            // 1) Lire ce qui existe déjà (non acheté)
            string existingQuery = shared
                ? $"{Table}?select=nom&list_id=eq.{listId!.Value}&est_achete=eq.false"
                : $"{Table}?select=nom&user_id=eq.{userId}&est_achete=eq.false";

            var existingRows = await _client.GetAsync<List<NomRow>>(existingQuery) ?? new List<NomRow>();

            var existing = existingRows
                .Where(r => !string.IsNullOrWhiteSpace(r.nom))
                .Select(r => r.nom!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2) Construire payload
            var payload = new List<object>();

            foreach (var n in names)
            {
                if (existing.Contains(n))
                    continue;

                if (shared)
                {
                    payload.Add(new
                    {
                        user_id = userId,
                        list_id = listId!.Value,
                        nom = n,
                        quantite = (string?)null,
                        unite = (string?)null,
                        est_achete = false
                    });
                }
                else
                {
                    payload.Add(new
                    {
                        user_id = userId,
                        nom = n,
                        quantite = (string?)null,
                        unite = (string?)null,
                        est_achete = false
                    });
                }
            }

            if (payload.Count == 0)
                return;

            await _client.PostAsync<object>(
                Table,
                payload.ToArray(),
                returnRepresentation: false);
        }

        // =========================
        // RPC: join shared list
        // =========================
        public async Task<(Guid listId, string name, string code)?> JoinByCodeAsync(string shareCode)
        {
            if (string.IsNullOrWhiteSpace(shareCode))
                return null;

            var payload = new { p_share_code = shareCode.Trim() };

            var result = await _client.PostAsync<List<JoinResult>>(
                "rpc/join_shopping_list",
                payload,
                returnRepresentation: true);

            var row = result?.FirstOrDefault();
            if (row == null)
                return null;

            return (row.list_id, row.name ?? "Liste partagée", row.share_code ?? shareCode.Trim());
        }

        // =========================
        // DTOs
        // =========================
        private sealed class NomRow
        {
            public string? nom { get; set; }
        }

        private sealed class JoinResult
        {
            public Guid list_id { get; set; }
            public string? name { get; set; }
            public string? share_code { get; set; }
        }

        public sealed class ArticleCourseRow
        {
            public long id { get; set; }
            public DateTimeOffset created_at { get; set; }
            public string? nom { get; set; }
            public string? quantite { get; set; }
            public string? unite { get; set; }
            public bool est_achete { get; set; }
            public Guid user_id { get; set; }
            public Guid? list_id { get; set; }
        }
    }
}
