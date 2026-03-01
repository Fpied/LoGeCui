using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Supabase;

namespace LoGeCuiShared.Functions
{
    internal static class IngredientFunction
    {
        public static async Task<List<Ingredient>> GetIngredientsAsync(Supabase.Postgrest.Client client, Guid userGuid)
        {

            var response = await client
                .Table<IngredientDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Get();

            return response.Models.Select(db => new Ingredient
            {
                Id = db.Id,
                // Si Ingredient.UserId est Guid (non nullable), on protège :
                UserId = db.UserId ?? Guid.Empty,
                Nom = db.Nom ?? "",
                Quantite = db.Quantite ?? "",
                Unite = db.Unite ?? "",
                EstDisponible = db.EstDisponible
            }).ToList();
        }

        public static async Task<Ingredient?> AddIngredientAsync(Supabase.Postgrest.Client client, Guid userGuid, Ingredient ingredient)
        {

            var db = new IngredientDb
            {
                Nom = ingredient.Nom,
                Quantite = ingredient.Quantite,
                Unite = ingredient.Unite,
                EstDisponible = ingredient.EstDisponible,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            var response = await client.Table<IngredientDb>().Insert(db);
            var inserted = response.Models.FirstOrDefault();
            if (inserted == null) return null;

            return new Ingredient
            {
                Id = inserted.Id,
                UserId = inserted.UserId ?? Guid.Empty,
                Nom = inserted.Nom ?? "",
                Quantite = inserted.Quantite ?? "",
                Unite = inserted.Unite ?? "",
                EstDisponible = inserted.EstDisponible
            };
        }

        public static async Task<bool> UpdateIngredientAsync(Supabase.Postgrest.Client client, Guid userGuid, Ingredient ingredient)
        {

            if (ingredient.Id == Guid.Empty)
                throw new InvalidOperationException("Id ingrédient manquant.");

            var db = new IngredientDb
            {
                Id = ingredient.Id,
                Nom = ingredient.Nom,
                Quantite = ingredient.Quantite,
                Unite = ingredient.Unite,
                EstDisponible = ingredient.EstDisponible,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            await client.Table<IngredientDb>().Update(db);
            return true;
        }

        public static async Task<bool> DeleteIngredientAsync(Supabase.Postgrest.Client client, Guid userGuid, Guid id)
        {

            if (id == Guid.Empty)
                throw new InvalidOperationException("Id ingrédient manquant.");

            await client.Table<IngredientDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            return true;
        }
    }
}
