using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoGeCuiShared.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace LoGeCuiShared.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private string? _accessToken;
        private string? _currentUserId;
        private readonly Client _client;

        public SupabaseService(string url, string key)
        {
            _supabaseUrl = url;
            _supabaseKey = key;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            var options = new ClientOptions
            {
                Headers = new Dictionary<string, string>
                {
                    { "apikey", key },
                    { "Authorization", $"Bearer {key}" }
                }
            };

            _client = new Client($"{url}/rest/v1", options);
        }

        // === MÉTHODES D'AUTHENTIFICATION REST ===

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

                var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/signup", request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var json = JsonDocument.Parse(content);
                        string? userId = null;

                        if (json.RootElement.TryGetProperty("id", out var idElement))
                            userId = idElement.GetString();

                        if (!string.IsNullOrEmpty(userId))
                        {
                            _currentUserId = userId;
                            return (true, userId, null);
                        }

                        return (true, "unknown", null);
                    }
                    catch
                    {
                        return (true, "unknown", null);
                    }
                }
                else
                {
                    string errorMessage = "Erreur lors de l'inscription";

                    try
                    {
                        var json = JsonDocument.Parse(content);
                        if (json.RootElement.TryGetProperty("msg", out var msg)) errorMessage = msg.GetString() ?? errorMessage;
                        else if (json.RootElement.TryGetProperty("error_description", out var ed)) errorMessage = ed.GetString() ?? errorMessage;
                        else if (json.RootElement.TryGetProperty("message", out var m)) errorMessage = m.GetString() ?? errorMessage;
                        else if (json.RootElement.TryGetProperty("error", out var e)) errorMessage = e.GetString() ?? errorMessage;
                    }
                    catch
                    {
                        if (!string.IsNullOrEmpty(content) && content.Length < 200) errorMessage = content;
                        else errorMessage = $"Erreur {response.StatusCode}";
                    }

                    return (false, null, errorMessage);
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

                var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/token?grant_type=password", request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var json = JsonDocument.Parse(content);
                        _accessToken = json.RootElement.GetProperty("access_token").GetString();
                        var userId = json.RootElement.GetProperty("user").GetProperty("id").GetString();

                        _currentUserId = userId;
                        _client.Options.Headers["Authorization"] = $"Bearer {_accessToken}";

                        return (true, _accessToken, userId, null);
                    }
                    catch
                    {
                        return (false, null, null, "Erreur lors du traitement de la réponse");
                    }
                }
                else
                {
                    string errorMessage = "Email ou mot de passe incorrect";

                    try
                    {
                        var json = JsonDocument.Parse(content);
                        if (json.RootElement.TryGetProperty("error_description", out var ed)) errorMessage = ed.GetString() ?? errorMessage;
                        else if (json.RootElement.TryGetProperty("message", out var m)) errorMessage = m.GetString() ?? errorMessage;
                        else if (json.RootElement.TryGetProperty("msg", out var msg)) errorMessage = msg.GetString() ?? errorMessage;
                    }
                    catch { }

                    return (false, null, null, errorMessage);
                }
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

        public string? GetCurrentUserId() => _currentUserId;

        private Guid RequireCurrentUserGuid()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
                throw new InvalidOperationException("Utilisateur non connecté. Veuillez vous reconnecter.");

            if (!Guid.TryParse(_currentUserId, out var guid))
                throw new InvalidOperationException("Identifiant utilisateur invalide. Veuillez vous reconnecter.");

            return guid;
        }

        // ===== ARTICLES =====

        public async Task<List<ArticleCourse>> GetArticlesAsync()
        {
            var response = await _client.Table<ArticleCourseDb>().Get();

            return response.Models.Select(db => new ArticleCourse
            {
                Id = db.Id,
                Nom = db.Nom ?? "",
                Quantite = db.Quantite ?? "",
                Unite = db.Unite ?? "",
                EstAchete = db.EstAchete
            }).ToList();
        }

        public async Task<ArticleCourse?> AddArticleAsync(ArticleCourse article)
        {
            // IMPORTANT: UserId est Guid? donc conversion obligatoire
            var userGuid = RequireCurrentUserGuid();

            var dbArticle = new ArticleCourseDb
            {
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete,
                UserId = userGuid
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
                EstAchete = inserted.EstAchete
            };
        }

        public async Task<bool> UpdateArticleAsync(int id, ArticleCourse article)
        {
            var dbArticle = new ArticleCourseDb
            {
                Id = id,
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete
            };

            await _client.Table<ArticleCourseDb>().Update(dbArticle);
            return true;
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            await _client.Table<ArticleCourseDb>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();

            return true;
        }

        public async Task<bool> DeleteAchetesAsync()
        {
            await _client.Table<ArticleCourseDb>()
                .Filter("est_achete", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Delete();

            return true;
        }

        // ===== INGREDIENTS =====
        // IMPORTANT : on utilise LoGeCuiShared.Models.Ingredient (pas LoGeCui.Models)

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
                UserId = db.UserId,
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
                UserId = userGuid
            };

            var response = await _client.Table<IngredientDb>().Insert(db);
            var inserted = response.Models.FirstOrDefault();
            if (inserted == null) return null;

            return new Ingredient
            {
                Id = inserted.Id,
                UserId = inserted.UserId,
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
                UserId = userGuid
            };

            await _client.Table<IngredientDb>().Update(db);
            return true;
        }

        public async Task<bool> DeleteIngredientAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new InvalidOperationException("Id ingrédient manquant.");

            await _client.Table<IngredientDb>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            return true;
        }
    }

    // ===== MODELES DB =====

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
        public Guid? UserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
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
        public Guid? UserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
