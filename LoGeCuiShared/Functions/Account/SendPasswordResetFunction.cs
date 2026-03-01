using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;

namespace LoGeCuiShared.Functions.Account
{
    internal static class SendPasswordResetFunction
    {
        public static async Task<(bool success, string? error)> SendPasswordResetAsync(HttpClient httpClient, string supabaseUrl, string supabaseKey, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Adresse email invalide");

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);

                var request = new
                {
                    email = email.Trim(),
                    redirect_to = "https://logecui.fr/logecui-reset-password/index.html"
                };

                var response = await httpClient.PostAsJsonAsync($"{supabaseUrl}/auth/v1/recover", request);
                var content = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? (true, null)
                    : (false, SupabaseErrorHelper.ExtractSupabaseError(content, response.StatusCode));
            }
            catch
            {
                return (false, "Erreur réseau. Réessayez plus tard.");
            }
        }

    }
        
    
}
