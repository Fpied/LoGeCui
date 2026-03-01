using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Json;

namespace LoGeCuiShared.Functions.Account
{
    internal static class UpdatePasswordFunction
    {
        public static async Task<(bool success, string? error)> UpdatePasswordAsync(HttpClient httpClient, string supabaseUrl, string supabaseKey, string accessToken, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return (false, "Le mot de passe doit contenir au moins 6 caractères.");

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.PutAsJsonAsync(
                    $"{supabaseUrl}/auth/v1/user",
                    new { password = newPassword });

                var content = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? (true, null)
                    : (false, SupabaseErrorHelper.ExtractSupabaseError(content, response.StatusCode));
            }
            catch
            {
                return (false, "Erreur réseau.");
            }
        }
    }
}
