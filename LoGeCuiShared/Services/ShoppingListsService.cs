using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supabase;

namespace LoGeCuiShared.Services
{
    public sealed class ShoppingListsService
    {
        private readonly SupabaseRestClient _client;

        public ShoppingListsService(SupabaseRestClient client)
        {
            _client = client;
        }

        private sealed class ShoppingListRow
        {
            public Guid id { get; set; }
            public Guid owner_user_id { get; set; }
            public string? name { get; set; }
            public string? share_code { get; set; }
        }

        public async Task<Guid> EnsurePersonalListIdAsync(Guid userId)
        {
            // 1) Chercher une liste existante
            var q = "shopping_lists?select=id,owner_user_id,name,share_code" +
                    $"&owner_user_id=eq.{userId}" +
                    "&order=created_at.asc&limit=1";

            var existing = await _client.GetAsync<List<ShoppingListRow>>(q);
            var row = existing?.FirstOrDefault();
            if (row != null)
                return row.id;

            // 2) Sinon créer une liste perso
            var payload = new[]
            {
                new
                {
                    owner_user_id = userId,
                    name = "Ma liste"
                    // share_code généré automatiquement par le trigger SQL
                }
            };

            var created = await _client.PostAsync<List<ShoppingListRow>>(
                "shopping_lists?select=id,owner_user_id,name,share_code",
                payload,
                returnRepresentation: true);

            return created!.First().id;
        }
    }
}
