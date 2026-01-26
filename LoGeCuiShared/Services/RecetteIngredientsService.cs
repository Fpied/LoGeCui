using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supabase;

namespace LoGeCuiShared.Services
{
    public sealed class RecetteIngredientsService
    {
        private readonly SupabaseRestClient _client;
        private const string Table = "recette_ingredients";

        public RecetteIngredientsService(SupabaseRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        // DTO conforme au schéma DB
        private sealed class Row
        {
            public string? ingredient_nom { get; set; }
            public string? quantite { get; set; }
            public string? unite { get; set; }
        }

        public async Task<List<(string nom, string? quantite, string? unite)>> GetForRecetteAsync(Guid recetteId)
        {
            var q = $"{Table}?select=ingredient_nom,quantite,unite&recette_id=eq.{recetteId}&order=created_at.asc";
            var rows = await _client.GetAsync<List<Row>>(q) ?? new List<Row>();

            return rows
                .Where(r => !string.IsNullOrWhiteSpace(r.ingredient_nom))
                .Select(r => (r.ingredient_nom!.Trim(), r.quantite, r.unite))
                .ToList();
        }

        // Remplace la liste complète (simple et robuste)
        public async Task ReplaceForRecetteAsync(Guid recetteId, IEnumerable<(string nom, string? quantite, string? unite)> items)
        {
            // 1) delete
            await _client.DeleteAsync($"{Table}?recette_id=eq.{recetteId}");

            // 2) insert
            var payload = items
                .Where(i => !string.IsNullOrWhiteSpace(i.nom))
                .Select(i => new
                {
                    recette_id = recetteId,
                    ingredient_nom = i.nom.Trim(),
                    quantite = string.IsNullOrWhiteSpace(i.quantite) ? null : i.quantite.Trim(),
                    unite = string.IsNullOrWhiteSpace(i.unite) ? null : i.unite.Trim()
                })
                .ToArray();

            if (payload.Length == 0)
                return;

            await _client.PostAsync<object>(Table, payload, returnRepresentation: false);
        }
    }
}
