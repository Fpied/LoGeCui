using LoGeCuiMobile.Pages;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiShared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace LoGeCuiMobile
{
    public partial class App : Application
    {
        public RecetteIngredientsService? RecetteIngredientsService { get; private set; }

        public SupabaseService? Supabase { get; private set; }
        public SupabaseRestClient? RestClient { get; private set; }

        public RecipesService? RecipesService { get; private set; }
        public IngredientsService? IngredientsService { get; private set; }
        public ListeCoursesSupabaseService? ListeCoursesSupabaseService { get; private set; }

        public Guid? CurrentUserId { get; private set; }
        public string OcrApiKey { get; private set; } = "";

        public bool IsConnected =>
            CurrentUserId != null &&
            RestClient != null &&
            RecipesService != null &&
            IngredientsService != null &&
            ListeCoursesSupabaseService != null;

        private IConfigurationRoot? _config;

        public App()
        {
            InitializeComponent();

            // ✅ 1) Appliquer la langue sauvegardée AVANT de charger l'UI
            ApplySavedLanguage();

            ApplySavedTheme(); // 👈 AJOUTE CETTE LIGNE

            // ✅ (Optionnel) Suivre le thème système (ou forcer Dark/Light ailleurs)
            Current.UserAppTheme = AppTheme.Unspecified;

            // ✅ 2) Init des services de base (anon + OCR)
            InitBaseServices();

            // ✅ 3) Page par défaut (avant auto-login)
            MainPage = RootPage.CreateLoginRoot();

            // ✅ 4) Lancement async (config + auto-login)
            _ = InitAsync();
        }

        private void ApplySavedLanguage()
        {
            try
            {
                var lang = Preferences.Get("app_language", "fr");

                // CultureInfo supporte "fr", "en", "pt-BR", etc.
                var culture = new CultureInfo(lang);

                // ✅ Centralise tout (culture + refresh UI si TranslateExtension)
                LocalizationResourceManager.Instance.SetCulture(culture);
            }
            catch
            {
                // Fallback FR si la culture est invalide
                LocalizationResourceManager.Instance.SetCulture(new CultureInfo("fr"));
            }
        }

        private void InitBaseServices()
        {
            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();
            Supabase = new SupabaseService(url, key);

            try
            {
                OcrApiKey = ConfigurationHelper.GetOcrApiKey();
                System.Diagnostics.Debug.WriteLine($"✅ OCR Key chargée: {OcrApiKey.Substring(0, 5)}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR OCR: {ex.Message}");
                OcrApiKey = "K86867725288957"; // ⚠️ debug only
            }

            // ❌ Ne pas supprimer sb_access_token/sb_user_id ici
        }

        private async Task InitAsync()
        {
            try
            {
                await InitConfigurationAsync();
                await TryAutoLoginAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ InitAsync: {ex}");
                ShowLogin();
            }
        }

        private async Task InitConfigurationAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonStream(stream);

            _config = builder.Build();
        }

        private async Task TryAutoLoginAsync()
        {
            var rememberMe = await SecureStorage.GetAsync("remember_me");
            if (!string.Equals(rememberMe, "true", StringComparison.OrdinalIgnoreCase))
            {
                ShowLogin();
                return;
            }

            var accessToken = await SecureStorage.GetAsync("sb_access_token");
            var userId = await SecureStorage.GetAsync("sb_user_id");

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
            {
                ShowLogin();
                return;
            }

            // Recharge la session (au cas où)
            Supabase ??= new SupabaseService(
                ConfigurationHelper.GetSupabaseUrl(),
                ConfigurationHelper.GetSupabaseKey()
            );

            Supabase.SetSession(accessToken, userId);

            if (!Guid.TryParse(userId, out var guidUserId))
            {
                ShowLogin();
                return;
            }

            CurrentUserId = guidUserId;

            InitRestServices(accessToken);
            ShowAppShell();
        }

        public void OnLoginSuccess(string accessToken, string userId)
        {
            // Sécurité : Supabase peut être null si jamais InitBaseServices a été modifié ailleurs
            Supabase ??= new SupabaseService(
                ConfigurationHelper.GetSupabaseUrl(),
                ConfigurationHelper.GetSupabaseKey()
            );

            Supabase.SetSession(accessToken, userId);

            if (!Guid.TryParse(userId, out var guidUserId))
            {
                ShowLogin();
                return;
            }

            CurrentUserId = guidUserId;

            InitRestServices(accessToken);
            ShowAppShell();
        }

        private void InitRestServicesInternal(string accessToken)
        {
            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();

            var client = new SupabaseRestClient(url, key);
            client.SetBearerToken(accessToken);

            RestClient = client;
            RecipesService = new RecipesService(client);
            IngredientsService = new IngredientsService(client);
            ListeCoursesSupabaseService = new ListeCoursesSupabaseService(client);
            RecetteIngredientsService = new RecetteIngredientsService(client);
        }

        public void ShowAppShell()
        {
            if (!IsConnected)
            {
                ShowLogin();
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new AppShell();
            });
        }

        public void ShowLogin()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = RootPage.CreateLoginRoot();
            });
        }

        public void Logout()
        {
            SecureStorage.Remove("remember_me");
            SecureStorage.Remove("sb_access_token");
            SecureStorage.Remove("sb_user_id");

            Supabase?.ClearSession();
            CurrentUserId = null;
            RestClient = null;
            RecipesService = null;
            IngredientsService = null;
            ListeCoursesSupabaseService = null;
            RecetteIngredientsService = null;

            ShowLogin();
        }

        public Task HandleDeepLinkAsync(Uri uri)
        {
            return Task.CompletedTask;
        }

        public void SetSupabase(SupabaseService supabase) => Supabase = supabase;
        public void SetCurrentUserId(Guid userId) => CurrentUserId = userId;

        public void InitRestServices(string accessToken)
        {
            InitRestServicesInternal(accessToken);
        }

        private void ApplySavedTheme()
        {
            var theme = Preferences.Get("app_theme", "system");

            Current.UserAppTheme = theme switch
            {
                "light" => AppTheme.Light,
                "dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }
}
