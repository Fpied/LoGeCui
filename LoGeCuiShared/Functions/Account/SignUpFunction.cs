using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace LoGeCuiShared.Functions.Account
{
    internal static class SignUpFunction
    {
        public static async Task<(bool success, string? userId, string? error)> SignUpAsync(HttpClient httpClient, string supabaseUrl, string supabaseKey, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, null, "Adresse email invalide");

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, null, "Le mot de passe doit contenir au moins 6 caractères");

                var request = new { email = email.Trim(), password };

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", supabaseKey);

                var response = await httpClient.PostAsJsonAsync($"{supabaseUrl}/auth/v1/signup", request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, SupabaseErrorHelper.ExtractSupabaseError(content, response.StatusCode));

                // Supabase renvoie souvent { user: { id: ... } }, parfois { id: ... }
                try
                {
                    var json = JsonDocument.Parse(content);

                    if (json.RootElement.TryGetProperty("user", out var userEl) &&
                        userEl.TryGetProperty("id", out var uidEl))
                    {
                        var uid = uidEl.GetString();
                        if (!string.IsNullOrWhiteSpace(uid))
                            return (true, uid, null);
                    }

                    if (json.RootElement.TryGetProperty("id", out var idEl))
                    {
                        var uid = idEl.GetString();
                        if (!string.IsNullOrWhiteSpace(uid))
                            return (true, uid, null);
                    }

                    return (true, "unknown", null);
                }
                catch
                {
                    return (true, "unknown", null);
                }
            }
            catch (HttpRequestException)
            {
                return (false, null, "Erreur de connexion au serveur. Vérifiez votre connexion internet.");
            }
            catch (TaskCanceledException)
            {
                return (false, null, "La requête a expiré. Vérifiez votre connexion internet.");
            }
            catch (Exception ex)
            {
                return (false, null, $"Erreur inattendue : {ex.Message}");
            }
        }
    }
}
