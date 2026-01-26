using LoGeCuiMobile.Pages;
using LoGeCuiShared.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;


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

        public App()
        {
            InitializeComponent();

            MainPage = RootPage.CreateLoginRoot();
            Init();
            Task.Run(async () =>
            {
                await InitConfigurationAsync();
                MainThread.BeginInvokeOnMainThread(Init);
            });
        }

        private IConfigurationRoot? _config;


        private void Init()
        {
            // SUPABASE (anon)
            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();
            Supabase = new SupabaseService(url, key);

            // OCR (chargée UNE FOIS)
            try
            {
                OcrApiKey = ConfigurationHelper.GetOcrApiKey();
                System.Diagnostics.Debug.WriteLine($"✅ OCR Key chargée: {OcrApiKey.Substring(0, 5)}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR OCR: {ex.Message}");
                // Temporairement, hardcodez pour tester
                OcrApiKey = "K86867725288957"; // ⚠️ METTEZ VOTRE VRAIE CLÉ ICI TEMPORAIREMENT
            }

            // ❌ PAS d’auto-login : nettoyage SYNCHRONE (API correcte)
            SecureStorage.Remove("sb_access_token");
            SecureStorage.Remove("sb_user_id");
        }

        // APPELÉ UNIQUEMENT APRÈS LOGIN
        public void OnLoginSuccess(string accessToken, string userId)
        {
            Supabase!.SetSession(accessToken, userId);
            CurrentUserId = Guid.Parse(userId);

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
            // ✅ AJOUTER CETTE LIGNE ICI
            RecetteIngredientsService = new RecetteIngredientsService(client);
        }

        private async Task InitConfigurationAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonStream(stream);

            _config = builder.Build();

            
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

        // ✅ MÉTHODE RÉTABLIE (utilisée ailleurs)
        public void ShowLogin()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = RootPage.CreateLoginRoot();
            });
        }

        public void Logout()
        {
            SecureStorage.Remove("sb_access_token");
            SecureStorage.Remove("sb_user_id");

            Supabase?.ClearSession();
            CurrentUserId = null;
            RestClient = null;
            RecipesService = null;
            IngredientsService = null;
            ListeCoursesSupabaseService = null;

            ShowLogin();
        }

        // =========================
        // MÉTHODES DE COMPATIBILITÉ
        // (utilisées ailleurs dans l'app)
        // =========================

        // Ancien point d'entrée DeepLink (Android)
        public async Task HandleDeepLinkAsync(Uri uri)
        {
            // Tu peux garder la logique existante si elle est ailleurs.
            // Ici, on empêche juste le crash / erreur de compilation.
            await Task.CompletedTask;
        }

        // Ancien setter Supabase (appelé depuis LoginPage)
        public void SetSupabase(SupabaseService supabase)
        {
            Supabase = supabase;
        }

        // Ancien setter UserId
        public void SetCurrentUserId(Guid userId)
        {
            CurrentUserId = userId;
        }

        // Ancienne méthode publique (appelée depuis LoginPage)
        public void InitRestServices(string accessToken)
        {
            // Appelle la méthode privée existante
            InitRestServicesInternal(accessToken);
        }

        // 🔒 On renomme l'ancienne méthode privée
       

    }
}






