using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LoGeCuiShared.Models;
using Supabase;

namespace LoGeCuiShared.Services
{
    public sealed class IngredientsService
    {
        private readonly SupabaseRestClient _client;
        private const string Table = "ingredients";

        public IngredientsService(SupabaseRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<List<Ingredient>> GetIngredientsAsync(Guid userId)
        {
            var q =
                $"{Table}?" +
                "select=id,user_id,nom,quantite,unite,est_disponible,est_favori,created_at" +
                $"&user_id=eq.{userId}" +
                "&order=created_at.desc";

            return await _client.GetAsync<List<Ingredient>>(q) ?? new List<Ingredient>();
        }

        public async Task<Ingredient?> CreateIngredientAsync(Guid userId, Ingredient i)
        {
            var payload = new
            {
                user_id = userId,
                nom = i.Nom,
                quantite = string.IsNullOrWhiteSpace(i.Quantite) ? null : i.Quantite,
                unite = string.IsNullOrWhiteSpace(i.Unite) ? null : i.Unite,
                est_disponible = i.EstDisponible,
                est_favori = i.EstFavori
            };

            var created = await _client.PostAsync<List<Ingredient>>(
                $"{Table}?select=id,user_id,nom,quantite,unite,est_disponible,est_favori,created_at",
                payload,
                returnRepresentation: true);

            return created?.Count > 0 ? created[0] : null;
        }


        public async Task UpdateDisponibiliteAsync(Guid ingredientId, bool estDisponible)
        {
            var payload = new { est_disponible = estDisponible };
            await _client.PatchAsync($"{Table}?id=eq.{ingredientId}", payload);
        }

        public async Task DeleteIngredientAsync(Guid ingredientId)
        {
            await _client.DeleteAsync($"{Table}?id=eq.{ingredientId}");
        }

        public async Task UpdateFavoriAsync(Guid ingredientId, bool estFavori)
        {
            var payload = new { est_favori = estFavori };
            await _client.PatchAsync($"{Table}?id=eq.{ingredientId}", payload);
        }

    }
}


