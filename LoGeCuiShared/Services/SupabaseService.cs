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
        private string? _currentUserId; // AJOUTÉ
        private readonly Client _client;

        public SupabaseService(string url, string key)
        {
            _supabaseUrl = url;
            _supabaseKey = key;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

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
                // Validation de base
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    return (false, null, "Adresse email invalide");
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    return (false, null, "Le mot de passe doit contenir au moins 6 caractères");
                }

                var request = new
                {
                    email = email.Trim(),
                    password = password
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);

                System.Diagnostics.Debug.WriteLine($"[SignUp] Tentative d'inscription pour: {email}");

                var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/signup", request);
                var content = await response.Content.ReadAsStringAsync();

                // Logs détaillés
                System.Diagnostics.Debug.WriteLine($"[SignUp] Status Code: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[SignUp] Response Content: {content}");
                System.Diagnostics.Debug.WriteLine($"[SignUp] Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var json = JsonDocument.Parse(content);
                        string? userId = null;
                        bool needsConfirmation = false;

                        // Dans la vraie réponse Supabase, l'utilisateur est à la racine
                        // et l'ID est directement accessible
                        if (json.RootElement.TryGetProperty("id", out var idElement))
                        {
                            userId = idElement.GetString();
                        }

                        // Vérifier si une confirmation est nécessaire
                        if (json.RootElement.TryGetProperty("confirmation_sent_at", out var confirmElement) &&
                            confirmElement.ValueKind != JsonValueKind.Null)
                        {
                            needsConfirmation = true;
                        }

                        if (!string.IsNullOrEmpty(userId))
                        {
                            // AJOUTÉ : Stocker le userId après inscription
                            _currentUserId = userId;

                            if (needsConfirmation)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SignUp] Inscription réussie ! UserId: {userId} (confirmation email requise)");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[SignUp] Inscription réussie ! UserId: {userId}");
                            }
                            return (true, userId, null);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[SignUp] Inscription réussie mais ID non trouvé");
                            return (true, "unknown", null);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SignUp] Erreur de parsing: {parseEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[SignUp] Stack trace: {parseEx.StackTrace}");
                        System.Diagnostics.Debug.WriteLine($"[SignUp] Contenu reçu: {content}");
                        // Si on a un succès HTTP mais qu'on ne peut pas parser, on considère quand même comme réussi
                        return (true, "unknown", null);
                    }
                }
                else
                {
                    // Extraction du message d'erreur de Supabase
                    string errorMessage = "Erreur lors de l'inscription";

                    try
                    {
                        var json = JsonDocument.Parse(content);

                        // Supabase peut retourner différents formats d'erreur
                        if (json.RootElement.TryGetProperty("msg", out var msgProperty))
                        {
                            errorMessage = msgProperty.GetString() ?? errorMessage;
                        }
                        else if (json.RootElement.TryGetProperty("error_description", out var errorDesc))
                        {
                            errorMessage = errorDesc.GetString() ?? errorMessage;
                        }
                        else if (json.RootElement.TryGetProperty("message", out var message))
                        {
                            errorMessage = message.GetString() ?? errorMessage;
                        }
                        else if (json.RootElement.TryGetProperty("error", out var error))
                        {
                            errorMessage = error.GetString() ?? errorMessage;
                        }
                    }
                    catch
                    {
                        // Si on ne peut pas parser, on utilise le contenu brut
                        if (!string.IsNullOrEmpty(content) && content.Length < 200)
                        {
                            errorMessage = content;
                        }
                        else
                        {
                            errorMessage = $"Erreur {response.StatusCode}";
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[SignUp] Échec: {errorMessage}");
                    return (false, null, errorMessage);
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SignUp] Erreur réseau: {httpEx.Message}");
                return (false, null, "Erreur de connexion au serveur. Vérifiez votre connexion internet.");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[SignUp] Timeout");
                return (false, null, "La requête a expiré. Vérifiez votre connexion internet.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignUp] Exception: {ex}");
                return (false, null, $"Erreur inattendue : {ex.Message}");
            }
        }

        public async Task<(bool success, string? accessToken, string? userId, string? error)> SignInAsync(string email, string password)
        {
            try
            {
                // Validation de base
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    return (false, null, null, "Adresse email invalide");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return (false, null, null, "Le mot de passe est requis");
                }

                var request = new
                {
                    email = email.Trim(),
                    password = password
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);

                System.Diagnostics.Debug.WriteLine($"[SignIn] Tentative de connexion pour: {email}");

                var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/token?grant_type=password", request);
                var content = await response.Content.ReadAsStringAsync();

                // Logs détaillés
                System.Diagnostics.Debug.WriteLine($"[SignIn] Status Code: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[SignIn] Response Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var json = JsonDocument.Parse(content);
                        _accessToken = json.RootElement.GetProperty("access_token").GetString();
                        var userId = json.RootElement.GetProperty("user").GetProperty("id").GetString();

                        // AJOUTÉ : Stocker le userId
                        _currentUserId = userId;

                        // Mettre à jour les headers du client Postgrest
                        _client.Options.Headers["Authorization"] = $"Bearer {_accessToken}";

                        System.Diagnostics.Debug.WriteLine($"[SignIn] Connexion réussie ! UserId: {userId}");
                        return (true, _accessToken, userId, null);
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SignIn] Erreur de parsing: {parseEx.Message}");
                        return (false, null, null, "Erreur lors du traitement de la réponse");
                    }
                }
                else
                {
                    // Extraction du message d'erreur
                    string errorMessage = "Email ou mot de passe incorrect";

                    try
                    {
                        var json = JsonDocument.Parse(content);

                        if (json.RootElement.TryGetProperty("error_description", out var errorDesc))
                        {
                            errorMessage = errorDesc.GetString() ?? errorMessage;
                        }
                        else if (json.RootElement.TryGetProperty("message", out var message))
                        {
                            errorMessage = message.GetString() ?? errorMessage;
                        }
                        else if (json.RootElement.TryGetProperty("msg", out var msg))
                        {
                            errorMessage = msg.GetString() ?? errorMessage;
                        }
                    }
                    catch
                    {
                        // Utiliser le message par défaut
                    }

                    System.Diagnostics.Debug.WriteLine($"[SignIn] Échec: {errorMessage}");
                    return (false, null, null, errorMessage);
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SignIn] Erreur réseau: {httpEx.Message}");
                return (false, null, null, "Erreur de connexion au serveur. Vérifiez votre connexion internet.");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[SignIn] Timeout");
                return (false, null, null, "La requête a expiré. Vérifiez votre connexion internet.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignIn] Exception: {ex}");
                return (false, null, null, $"Erreur inattendue : {ex.Message}");
            }
        }

        public string? GetCurrentUserId()
        {
            // MODIFIÉ : Retourner le userId stocké
            return _currentUserId;
        }

        // Récupérer tous les articles
        public async Task<List<ArticleCourse>> GetArticlesAsync()
        {
            try
            {
                var response = await _client
                    .Table<ArticleCourseDb>()
                    .Get();

                return response.Models.Select(db => new ArticleCourse
                {
                    Id = db.Id,
                    Nom = db.Nom ?? "",
                    Quantite = db.Quantite ?? "",
                    Unite = db.Unite ?? "",
                    EstAchete = db.EstAchete
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetArticles] Erreur: {ex.Message}");
                throw;
            }
        }

        // Ajouter un article
        public async Task<ArticleCourse?> AddArticleAsync(ArticleCourse article)
        {
            try
            {
                // AJOUTÉ : Vérifier que l'utilisateur est connecté
                if (string.IsNullOrEmpty(_currentUserId))
                {
                    throw new InvalidOperationException("Utilisateur non connecté. Veuillez vous connecter avant d'ajouter un article.");
                }

                var dbArticle = new ArticleCourseDb
                {
                    Nom = article.Nom,
                    Quantite = article.Quantite,
                    Unite = article.Unite,
                    EstAchete = article.EstAchete,
                    UserId = _currentUserId  // AJOUTÉ : Inclure le user_id
                };

                var response = await _client
                    .Table<ArticleCourseDb>()
                    .Insert(dbArticle);

                var inserted = response.Models.FirstOrDefault();
                if (inserted != null)
                {
                    return new ArticleCourse
                    {
                        Id = inserted.Id,
                        Nom = inserted.Nom ?? "",
                        Quantite = inserted.Quantite ?? "",
                        Unite = inserted.Unite ?? "",
                        EstAchete = inserted.EstAchete
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddArticle] Erreur: {ex.Message}");
                throw;
            }
        }

        // Mettre à jour un article
        public async Task<bool> UpdateArticleAsync(int id, ArticleCourse article)
        {
            try
            {
                var dbArticle = new ArticleCourseDb
                {
                    Id = id,
                    Nom = article.Nom,
                    Quantite = article.Quantite,
                    Unite = article.Unite,
                    EstAchete = article.EstAchete
                };

                await _client
                    .Table<ArticleCourseDb>()
                    .Update(dbArticle);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateArticle] Erreur: {ex.Message}");
                throw;
            }
        }

        // Supprimer un article par ID
        public async Task<bool> DeleteArticleAsync(int id)
        {
            try
            {
                await _client
                    .Table<ArticleCourseDb>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeleteArticle] Erreur: {ex.Message}");
                throw;
            }
        }

        // Supprimer tous les articles achetés
        public async Task<bool> DeleteAchetesAsync()
        {
            try
            {
                await _client
                    .Table<ArticleCourseDb>()
                    .Filter("est_achete", Supabase.Postgrest.Constants.Operator.Equals, true)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeleteAchetes] Erreur: {ex.Message}");
                throw;
            }
        }
    }

    // Modèle pour Supabase (avec attributs Postgrest)
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

        [Column("user_id")]  // AJOUTÉ
        public string? UserId { get; set; }  // AJOUTÉ

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}