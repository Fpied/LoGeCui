using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoGeCuiShared.Models;
using Supabase;

namespace LoGeCuiShared.Services
{
    public sealed class RecipesService
    {
        private readonly SupabaseRestClient _client;

        public RecipesService(SupabaseRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Récupère TOUTES les recettes de l’utilisateur courant
        /// (utilisé notamment pour le menu aléatoire)
        /// </summary>
        public async Task<List<Recette>> GetRecettesAsync(Guid userId)
        {
            var q =
                "recettes?" +
                "select=id,created_at,owner_user_id,nom,categorie,temps_minutes,note,is_favorite,instructions" +
                $"&owner_user_id=eq.{userId}" +
                "&order=created_at.desc";

            return await _client.GetAsync<List<Recette>>(q)
                   ?? new List<Recette>();
        }

        /// <summary>
        /// Récupère les recettes (optionnellement filtrées par catégorie)
        /// </summary>
        public async Task<List<Recette>> GetMyRecettesAsync(string? categorie = null)
        {
            var q =
                "recettes?" +
                "select=id,created_at,owner_user_id,nom,categorie,temps_minutes,note,is_favorite,instructions" +
                "&order=created_at.desc";

            if (!string.IsNullOrWhiteSpace(categorie) &&
                !string.Equals(categorie, "Toutes", StringComparison.OrdinalIgnoreCase))
            {
                q += $"&categorie=eq.{Uri.EscapeDataString(categorie)}";
            }

            return await _client.GetAsync<List<Recette>>(q)
                   ?? new List<Recette>();
        }

        /// <summary>
        /// Crée une recette et retourne la recette créée
        /// </summary>
        public async Task<Recette?> CreateRecetteAsync(Guid ownerUserId, Recette r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            var payload = new
            {
                owner_user_id = ownerUserId,
                nom = r.Nom,
                categorie = r.CategorieDb,
                temps_minutes = r.TempsMinutesDb,
                note = r.NoteDb,
                is_favorite = r.IsFavorite,
                instructions = r.InstructionsDb
            };

            var created = await _client.PostAsync<List<Recette>>(
                "recettes?select=id,created_at,owner_user_id,nom,categorie,temps_minutes,note,is_favorite,instructions",
                payload,
                returnRepresentation: true);

            return created?.FirstOrDefault();
        }

        /// <summary>
        /// Supprime une recette
        /// </summary>
        public async Task DeleteRecetteAsync(Guid recetteId)
        {
            await _client.DeleteAsync($"recettes?id=eq.{recetteId}");
        }

        /// <summary>
        /// Insert ou met à jour une recette via external_id
        /// </summary>
        public async Task UpsertRecetteAsync(Guid ownerUserId, Recette r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            if (string.IsNullOrWhiteSpace(r.ExternalId))
                throw new ArgumentException("ExternalId est requis pour upsert.", nameof(r));

            var payload = new[]
            {
                new
                {
                    owner_user_id = ownerUserId,
                    external_id = r.ExternalId,
                    nom = r.Nom,
                    categorie = r.CategorieDb,
                    temps_minutes = r.TempsMinutesDb,
                    note = r.NoteDb,
                    is_favorite = r.IsFavorite,
                    instructions = r.InstructionsDb
                }
            };

            await _client.PostAsync<object>(
                "recettes?on_conflict=external_id",
                payload,
                returnRepresentation: false,
                mergeDuplicates: true);
        }
        public async Task<Guid?> GetRecetteIdByExternalIdAsync(string externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                throw new ArgumentException("externalId requis", nameof(externalId));

            var q =
                "recettes?" +
                "select=id" +
                $"&external_id=eq.{Uri.EscapeDataString(externalId)}" +
                "&limit=1";

            var rows = await _client.GetAsync<List<RecetteIdRow>>(q);

            return rows?.FirstOrDefault()?.id;
        }

        private sealed class RecetteIdRow
        {
            public Guid id { get; set; }
        }

    }
}

