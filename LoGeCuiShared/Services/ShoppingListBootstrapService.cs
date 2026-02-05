using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoGeCuiShared.Services
{
    public sealed class ShoppingListBootstrapService
    {
        private readonly SupabaseRestClient _client;

        public ShoppingListBootstrapService(SupabaseRestClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        private sealed class Row
        {
            public Guid list_id { get; set; }
        }

        public async Task<Guid?> GetFirstListIdForUserAsync(Guid userId)
        {
            var q =
                "shopping_list_members?select=list_id" +
                $"&user_id=eq.{userId}" +
                "&limit=1";

            var rows = await _client.GetAsync<List<Row>>(q);
            return rows?.FirstOrDefault()?.list_id;
        }
    }
}

