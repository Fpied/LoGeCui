using Newtonsoft.Json.Linq;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace LoGeCuiShared.Functions.Account
{
    internal static class SignInFunction
    {
        public static async Task<(bool success, string? accessToken, string? userId, string? error)> SignInAsync(HttpClient httpClient, string supabaseUrl, string supabaseKey, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, null, null, "Adresse email invalide");

                if (string.IsNullOrWhiteSpace(password))
                    return (false, null, null, "Le mot de passe est requis");

                var request = new { email = email.Trim(), password };

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);

                var response = await httpClient.PostAsJsonAsync(
                    $"{supabaseUrl}/auth/v1/token?grant_type=password",
                    request);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, null, SupabaseErrorHelper.ExtractSupabaseError(content, response.StatusCode));

                var json = JsonDocument.Parse(content);

                var token = json.RootElement.GetProperty("access_token").GetString();
                var userId = json.RootElement.GetProperty("user").GetProperty("id").GetString();

                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
                    return (false, null, null, "Réponse Supabase invalide (token/userId manquant).");

                return (true, token, userId, null);
            }
            catch (HttpRequestException)
            {
                return (false, null, null, "Erreur de connexion au serveur. Vérifiez votre connexion internet.");
            }
            catch (TaskCanceledException)
            {
                return (false, null, null, "La requête a expiré. Vérifiez votre connexion internet.");
            }
            catch (Exception ex)
            {
                return (false, null, null, $"Erreur inattendue : {ex.Message}");
            }

        
        
        }

        
    }
}

