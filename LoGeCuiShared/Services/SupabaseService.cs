using LoGeCuiShared.Functions;
using LoGeCuiShared.Functions.Account;
using LoGeCuiShared.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;


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
            return await SignUpFunction.SignUpAsync(_httpClient, _supabaseUrl, _supabaseKey, email, password);
        }

        public async Task<(bool success, string? accessToken, string? userId, string? error)> SignInAsync(string email, string password)
        {
            var result = await SignInFunction.SignInAsync(_httpClient, _supabaseUrl, _supabaseKey, email, password);

            if (result.success && result.accessToken != null && result.userId != null)
            {
                SetSession(result.accessToken, result.userId);
            }
            return result;
        }

        public async Task<(bool success, string? accessToken, string? userId, string? error)> SignUpThenSignInAsync(string email, string password)
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
            return await SendPasswordResetFunction.SendPasswordResetAsync(_httpClient, _supabaseUrl, _supabaseKey, email);
        }

        public async Task<(bool success, string? error)> UpdatePasswordAsync(string accessToken, string newPassword)
        {
            return await UpdatePasswordFunction.UpdatePasswordAsync(_httpClient, _supabaseUrl, _supabaseKey, accessToken, newPassword);
        }

        // =========================
        // EDGE FUNCTION : DELETE ACCOUNT
        // =========================

        public async Task<(bool success, string? error)> DeleteAccountAsync()
        {
            var result = await DeleteAccountFunction.DeleteAccountAsync(_httpClient, _supabaseUrl, _supabaseKey, _accessToken ?? "");
            if (result.success)
            {
                ClearSession();
            }
            return result;
        }



        // =========================
        // ARTICLES (Liste de courses)
        // =========================

        public async Task<List<ArticleCourse>> GetArticlesAsync()
        {
            return await ArticlesCoursesFunction.GetArticlesAsync(_client, RequireCurrentUserGuid());
        }

        public async Task<ArticleCourse?> AddArticleAsync(ArticleCourse article)
        {
            return await ArticlesCoursesFunction.AddArticleAsync(_client, article, RequireCurrentUserGuid());
        }

        public async Task<bool> UpdateArticleAsync(int id, ArticleCourse article)
        {
            return await ArticlesCoursesFunction.UpdateArticleAsync(id, article, _client, RequireCurrentUserGuid());
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            return await ArticlesCoursesFunction.DeleteArticleAsync(_client, id, RequireCurrentUserGuid());
        }

        public async Task<bool> DeleteAchetesAsync()
        {
            return await ArticlesCoursesFunction.DeleteAchetesAsync(_client, RequireCurrentUserGuid());
        }

        // =========================
        // INGREDIENTS
        // =========================

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            return await IngredientFunction.GetIngredientsAsync(_client, RequireCurrentUserGuid());
        }

        public async Task<Ingredient?> AddIngredientAsync(Ingredient ingredient)
        {
            return await IngredientFunction.AddIngredientAsync(_client, RequireCurrentUserGuid(), ingredient);
        }

        public async Task<bool> UpdateIngredientAsync(Ingredient ingredient)
        {
            return await IngredientFunction.UpdateIngredientAsync(_client, RequireCurrentUserGuid(), ingredient);
        }

        public async Task<bool> DeleteIngredientAsync(Guid id)
        {
            return await IngredientFunction.DeleteIngredientAsync(_client, RequireCurrentUserGuid(), id);
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


