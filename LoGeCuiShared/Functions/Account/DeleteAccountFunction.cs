using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LoGeCuiShared.Functions.Account
{
    internal static class DeleteAccountFunction
    {
        public static async Task<(bool success, string? error)> DeleteAccountAsync(HttpClient httpClient, string supabaseUrl, string supabaseKey , string accessToken)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(accessToken))
                    return (false, "Session invalide. Veuillez vous reconnecter.");

                System.Diagnostics.Debug.WriteLine("=== DELETE ACCOUNT ===");

                // ✅ IMPORTANT : Headers corrects pour RPC
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"{supabaseUrl}/rest/v1/rpc/delete_user_account");

                request.Headers.TryAddWithoutValidation("apikey", supabaseKey);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
                request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response: {(int)response.StatusCode} - {content}");

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Erreur : {content}");
                }

                var json = JsonDocument.Parse(content);
                if (json.RootElement.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    return (true, null);
                }

                if (json.RootElement.TryGetProperty("error", out var error))
                {
                    return (false, error.GetString());
                }

                return (false, "Erreur inconnue");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                return (false, $"Erreur : {ex.Message}");
            }
        }
    }
}
