using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LoGeCuiShared.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Net.Http.Headers;


namespace LoGeCuiShared.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;

        // Session courante (après login)
        private string? _accessToken;    // JWT utilisateur
        private string? _currentUserId;  // GUID string

        private readonly Client _client;

        public SupabaseService(string url, string key)
        {
            _supabaseUrl = url ?? throw new ArgumentNullException(nameof(url));
            _supabaseKey = key ?? throw new ArgumentNullException(nameof(key));

            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            var options = new ClientOptions
            {
                Headers = new Dictionary<string, string>
                {
                    { "apikey", key },
                    // Tant qu'on n'est pas connecté, on reste en "anon"
                    { "Authorization", $"Bearer {key}" }
                }
            };

            _client = new Client($"{url}/rest/v1", options);
        }

        // =========================
        // SESSION
        // =========================

        public void SetSession(string accessToken, string userId)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken manquant", nameof(accessToken));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId manquant", nameof(userId));

            _accessToken = accessToken;
            _currentUserId = userId;

            // IMPORTANT: PostgREST doit utiliser le JWT utilisateur pour RLS
            _client.Options.Headers["Authorization"] = $"Bearer {_accessToken}";
        }

        public void ClearSession()
        {
            _accessToken = null;
            _currentUserId = null;

            // Retour "anon"
            _client.Options.Headers["Authorization"] = $"Bearer {_supabaseKey}";
        }

        public string? GetCurrentUserId() => _currentUserId;

        private Guid RequireCurrentUserGuid()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
                throw new InvalidOperationException("Utilisateur non connecté. Veuillez vous reconnecter.");

            if (!Guid.TryParse(_currentUserId, out var guid))
                throw new InvalidOperationException("Identifiant utilisateur invalide. Veuillez vous reconnecter.");

            return guid;
        }

        private void RequireAccessToken()
        {
            if (string.IsNullOrWhiteSpace(_accessToken))
                throw new InvalidOperationException("Session invalide. Veuillez vous reconnecter.");
        }

        // =========================
        // AUTH REST
        // =========================

        public async Task<(bool success, string? userId, string? error)> SignUpAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, null, "Adresse email invalide");

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, null, "Le mot de passe doit contenir au moins 6 caractères");

                var request = new { email = email.Trim(), password };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _supabaseKey);

                var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/signup", request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, ExtractSupabaseError(content, response.StatusCode));

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

        public async Task<(bool success, string? accessToken, string? userId, string? error)> SignInAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, null, null, "Adresse email invalide");

                if (string.IsNullOrWhiteSpace(password))
                    return (false, null, null, "Le mot de passe est requis");

                var request = new { email = email.Trim(), password };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_supabaseUrl}/auth/v1/token?grant_type=password",
                    request);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, null, ExtractSupabaseError(content, response.StatusCode));

                var json = JsonDocument.Parse(content);

                var token = json.RootElement.GetProperty("access_token").GetString();
                var userId = json.RootElement.GetProperty("user").GetProperty("id").GetString();

                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
                    return (false, null, null, "Réponse Supabase invalide (token/userId manquant).");

                SetSession(token, userId);
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

        public async Task<(bool success, string? accessToken, string? userId, string? error)>
            SignUpThenSignInAsync(string email, string password)
        {
            var (signUpSuccess, _, signUpError) = await SignUpAsync(email, password);
            if (!signUpSuccess)
                return (false, null, null, signUpError);

            var (signInSuccess, accessToken, userId, signInError) = await SignInAsync(email, password);
            if (!signInSuccess)
                return (false, null, null, signInError);

            return (true, accessToken, userId, null);
        }

        public async Task<(bool success, string? error)> SendPasswordResetAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Adresse email invalide");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);

                var request = new
                {
                    email = email.Trim(),
                    redirect_to = "logecui://reset-password"
                };

                var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/recover", request);
                var content = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? (true, null)
                    : (false, ExtractSupabaseError(content, response.StatusCode));
            }
            catch
            {
                return (false, "Erreur réseau. Réessayez plus tard.");
            }
        }

        public async Task<(bool success, string? error)> UpdatePasswordAsync(string accessToken, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return (false, "Le mot de passe doit contenir au moins 6 caractères.");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_supabaseUrl}/auth/v1/user",
                    new { password = newPassword });

                var content = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? (true, null)
                    : (false, ExtractSupabaseError(content, response.StatusCode));
            }
            catch
            {
                return (false, "Erreur réseau.");
            }
        }

        // =========================
        // EDGE FUNCTION : DELETE ACCOUNT
        // =========================

        public async Task<(bool success, string? error)> DeleteAccountAsync()
        {
            try
            {
                RequireAccessToken();

                System.Diagnostics.Debug.WriteLine("=== DELETE ACCOUNT DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"Token: {_accessToken?.Substring(0, 30)}");

                // ✅ IMPORTANT : Créer une nouvelle requête à chaque fois
                using var request = new HttpRequestMessage(HttpMethod.Post,
                    $"{_supabaseUrl}/functions/v1/delete-account");

                // ✅ Ajouter les headers dans le bon ordre
                request.Headers.TryAddWithoutValidation("apikey", _supabaseKey);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_accessToken}");

                System.Diagnostics.Debug.WriteLine($"Headers added: {request.Headers.Count()}");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response: {(int)response.StatusCode} - {content}");

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Erreur : {content}");
                }

                // Nettoyage local
                var userGuid = RequireCurrentUserGuid();
                await _client.Table<ArticleCourseDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                    .Delete();

                await _client.Table<IngredientDb>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                    .Delete();

                ClearSession();
                return (true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                return (false, $"Erreur : {ex.Message}");
            }
        }



        // =========================
        // ARTICLES (Liste de courses)
        // =========================

        public async Task<List<ArticleCourse>> GetArticlesAsync()
        {
            var userGuid = RequireCurrentUserGuid();

            var response = await _client
                .Table<ArticleCourseDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Get();

            return response.Models.Select(db => new ArticleCourse
            {
                Id = db.Id,
                Nom = db.Nom ?? "",
                Quantite = db.Quantite ?? "",
                Unite = db.Unite ?? "",
                EstAchete = db.EstAchete,
                // IMPORTANT: si ton modèle ArticleCourse.UserId est Guid (non nullable),
                // on protège avec ?? Guid.Empty
                UserId = db.UserId ?? Guid.Empty
            }).ToList();
        }

        public async Task<ArticleCourse?> AddArticleAsync(ArticleCourse article)
        {
            var userGuid = RequireCurrentUserGuid();

            var dbArticle = new ArticleCourseDb
            {
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            var response = await _client.Table<ArticleCourseDb>().Insert(dbArticle);
            var inserted = response.Models.FirstOrDefault();
            if (inserted == null) return null;

            return new ArticleCourse
            {
                Id = inserted.Id,
                Nom = inserted.Nom ?? "",
                Quantite = inserted.Quantite ?? "",
                Unite = inserted.Unite ?? "",
                EstAchete = inserted.EstAchete,
                UserId = inserted.UserId ?? Guid.Empty
            };
        }

        public async Task<bool> UpdateArticleAsync(int id, ArticleCourse article)
        {
            var userGuid = RequireCurrentUserGuid();

            var dbArticle = new ArticleCourseDb
            {
                Id = id,
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            await _client.Table<ArticleCourseDb>().Update(dbArticle);
            return true;
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var userGuid = RequireCurrentUserGuid();

            // Sécurise par user_id (sinon RLS peut bloquer ou pire, toucher autre chose si policy permissive)
            await _client.Table<ArticleCourseDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();

            return true;
        }

        public async Task<bool> DeleteAchetesAsync()
        {
            var userGuid = RequireCurrentUserGuid();

            await _client.Table<ArticleCourseDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Filter("est_achete", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Delete();

            return true;
        }

        // =========================
        // INGREDIENTS
        // =========================

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            var userGuid = RequireCurrentUserGuid();

            var response = await _client
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

        public async Task<Ingredient?> AddIngredientAsync(Ingredient ingredient)
        {
            var userGuid = RequireCurrentUserGuid();

            var db = new IngredientDb
            {
                Nom = ingredient.Nom,
                Quantite = ingredient.Quantite,
                Unite = ingredient.Unite,
                EstDisponible = ingredient.EstDisponible,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            var response = await _client.Table<IngredientDb>().Insert(db);
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

        public async Task<bool> UpdateIngredientAsync(Ingredient ingredient)
        {
            var userGuid = RequireCurrentUserGuid();

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

            await _client.Table<IngredientDb>().Update(db);
            return true;
        }

        public async Task<bool> DeleteIngredientAsync(Guid id)
        {
            var userGuid = RequireCurrentUserGuid();

            if (id == Guid.Empty)
                throw new InvalidOperationException("Id ingrédient manquant.");

            await _client.Table<IngredientDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            return true;
        }

        // =========================
        // UTIL
        // =========================

        private static string ExtractSupabaseError(string content, System.Net.HttpStatusCode code)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    var json = JsonDocument.Parse(content);

                    if (json.RootElement.TryGetProperty("msg", out var msg)) return msg.GetString() ?? $"Erreur {code}";
                    if (json.RootElement.TryGetProperty("error_description", out var ed)) return ed.GetString() ?? $"Erreur {code}";
                    if (json.RootElement.TryGetProperty("message", out var m)) return m.GetString() ?? $"Erreur {code}";
                    if (json.RootElement.TryGetProperty("error", out var e)) return e.GetString() ?? $"Erreur {code}";
                }
                catch
                {
                    if (content.Length < 400) return content;
                }
            }

            return $"Erreur {code}";
        }
    }

    // =========================
    // MODELES DB
    // =========================

    [Table("articles_courses")]
    public class ArticleCourseDb : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("nom")]
        public string? Nom { get; set; }

        [Column("quantite")]
        public string? Quantite { get; set; }

        [Column("unite")]
        public string? Unite { get; set; }

        [Column("est_achete")]
        public bool EstAchete { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; } // DB nullable -> mapping protège avec ?? Guid.Empty
    }

    [Table("ingredients")]
    public class IngredientDb : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("nom")]
        public string? Nom { get; set; }

        [Column("quantite")]
        public string? Quantite { get; set; }

        [Column("unite")]
        public string? Unite { get; set; }

        [Column("est_disponible")]
        public bool EstDisponible { get; set; }

        [Column("user_id")]
        public Guid? UserId { get; set; } // DB nullable -> mapping protège avec ?? Guid.Empty
    }
}


