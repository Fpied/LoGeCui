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

        /// <summary>
        /// Ajoute à la liste de courses les ingrédients manquants (sans doublons),
        /// en s'appuyant sur le trigger set_user_id_on_insert (donc on n'envoie pas user_id).
        /// </summary>
        public async Task AddMissingAsync(Guid userId, IEnumerable<string> missingNames)
        {
            var names = missingNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count == 0)
                return;

            // 1) Lire ce qui existe déjà pour cet utilisateur (non acheté)
            // (On filtre par user_id car ta table l'a bien, et le token bearer doit donner accès via RLS/trigger)
            var existingQuery =
                $"{Table}?select=nom&user_id=eq.{userId}&est_achete=eq.false";

            var existingRows = await _client.GetAsync<List<Row>>(existingQuery) ?? new List<Row>();

            var existing = existingRows
                .Where(r => !string.IsNullOrWhiteSpace(r.nom))
                .Select(r => r.nom!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2) Garder uniquement ceux qui n'existent pas
            var toInsert = names
                .Where(n => !existing.Contains(n))
                .Select(n => new
                {
                    nom = n,
                    quantite = (string?)null,
                    unite = (string?)null,
                    est_achete = false
                    // user_id est mis par trigger set_user_id_on_insert
                })
                .ToArray();

            if (toInsert.Length == 0)
                return;

            // 3) Insert batch
            await _client.PostAsync<object>(
                Table,
                toInsert,
                returnRepresentation: false);
        }

        private sealed class Row
        {
            public string? nom { get; set; }
        }
    }
}
