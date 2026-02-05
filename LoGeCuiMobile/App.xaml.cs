using LoGeCuiMobile.Pages;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiShared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using LoGeCuiMobile.Services.Local;
using Microsoft.Maui.Networking;
using Supabase; // ✅ pour SupabaseRestClient

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

        public ShoppingListsService? ShoppingListsService { get; private set; }

        public static LocalDatabase LocalDb { get; private set; } = null!;

        // ✅ Token RAM (upload storage etc.)
        public string? CurrentAccessToken { get; private set; }

        // ✅ Liste de courses active
        public Guid? CurrentShoppingListId { get; private set; }
        public void SetCurrentShoppingListId(Guid listId) => CurrentShoppingListId = listId;

        public Guid? CurrentUserId { get; private set; }
        public string OcrApiKey { get; private set; } = "";

        // ✅ Inclure ShoppingListsService dans la condition “connecté”
        public bool IsConnected =>
            CurrentUserId != null &&
            RestClient != null &&
            RecipesService != null &&
            IngredientsService != null &&
            ListeCoursesSupabaseService != null &&
            RecetteIngredientsService != null &&
            ShoppingListsService != null;

        private IConfigurationRoot? _config;

        public App()
        {
            InitializeComponent();

            // ✅ Langue & thème avant l'UI
            ApplySavedLanguage();
            ApplySavedTheme();

            // ✅ Local DB
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "logecui.db3");
            LocalDb = new LocalDatabase(dbPath);

            // ✅ Init services de base (anon + OCR)
            InitBaseServices();

            // ✅ Page par défaut : Login (avant auto-login)
            MainPage = RootPage.CreateLoginRoot();

            // ✅ Init async (config + auto-login)
            _ = InitAsync();
        }

        // ------------------ JWT EXPIRED HANDLING ------------------

        public static bool IsJwtExpiredException(Exception ex)
        {
            return ex.ToString().Contains("JWT expired", StringComparison.OrdinalIgnoreCase);
        }

        public void HandleJwtExpired()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Current?.MainPage?.DisplayAlert(
                        "Session expirée",
                        "Ta session a expiré. Merci de te reconnecter.",
                        "OK"
                    );
                }
                catch { }

                Logout(silent: true);
            });
        }

        // ------------------ UI SETTINGS ------------------

        private void ApplySavedLanguage()
        {
            try
            {
                var lang = Preferences.Get("app_language", "fr");
                var culture = new CultureInfo(lang);
                LocalizationResourceManager.Instance.SetCulture(culture);
            }
            catch
            {
                LocalizationResourceManager.Instance.SetCulture(new CultureInfo("fr"));
            }
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

        // ------------------ INIT SERVICES ------------------

        private void InitBaseServices()
        {
            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();
            Supabase = new SupabaseService(url, key);

            try
            {
                OcrApiKey = ConfigurationHelper.GetOcrApiKey();
                if (!string.IsNullOrWhiteSpace(OcrApiKey) && OcrApiKey.Length >= 5)
                    System.Diagnostics.Debug.WriteLine($"✅ OCR Key chargée: {OcrApiKey.Substring(0, 5)}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERREUR OCR: {ex.Message}");
                OcrApiKey = "K86867725288957"; // ⚠️ debug only
            }
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
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                var builder = new ConfigurationBuilder().AddJsonStream(stream);
                _config = builder.Build();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ appsettings.json introuvable ou illisible: " + ex.Message);
                _config = null;
            }
        }

        // ------------------ AUTO LOGIN ------------------

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

            // ✅ Init services REST avec bearer + token RAM
            InitRestServicesInternal(accessToken);

            // ✅ Validation si internet : si token expiré -> logout propre
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                var ok = await ValidateSessionAsync();
                if (!ok)
                {
                    HandleJwtExpired();
                    return;
                }
            }

            // ✅ Charger / assurer list_id (liste de courses active)
            await EnsureActiveShoppingListAsync();

            ShowAppShell();
        }

        public async void OnLoginSuccess(string accessToken, string userId)
        {
            // ✅ Toujours garder le token dispo pour Storage (même si remember_me n'est pas activé)
            await SecureStorage.SetAsync("sb_access_token", accessToken);
            await SecureStorage.SetAsync("sb_user_id", userId);

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

            // ✅ Init services REST avec bearer + token RAM
            InitRestServicesInternal(accessToken);

            // ✅ Validation si internet
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                var ok = await ValidateSessionAsync();
                if (!ok)
                {
                    HandleJwtExpired();
                    return;
                }
            }

            // ✅ Charger / assurer list_id (liste de courses active)
            await EnsureActiveShoppingListAsync();

            ShowAppShell();
        }

        private void InitRestServicesInternal(string accessToken)
        {
            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();

            var client = new SupabaseRestClient(url, key);
            client.SetBearerToken(accessToken);

            // ✅ Stocker le token en RAM
            CurrentAccessToken = accessToken;

            RestClient = client;

            // ✅ IMPORTANT : instancier les services AVEC le client non-null
            ShoppingListsService = new ShoppingListsService(client);
            RecipesService = new RecipesService(client);
            IngredientsService = new IngredientsService(client);
            ListeCoursesSupabaseService = new ListeCoursesSupabaseService(client);
            RecetteIngredientsService = new RecetteIngredientsService(client);
        }

        // ✅ Garantit qu'on a toujours une liste active (sinon création/récupération)
        private async Task EnsureActiveShoppingListAsync()
        {
            if (RestClient == null || CurrentUserId == null)
            {
                CurrentShoppingListId = null;
                return;
            }

            try
            {
                var url = $"shopping_lists?select=id&owner_user_id=eq.{CurrentUserId.Value}&limit=1";
                var json = await RestClient.GetAsync<string>(url);

                if (!string.IsNullOrWhiteSpace(json) && json.Contains("\"id\""))
                {
                    var start = json.IndexOf("\"id\":\"") + 6;
                    var end = json.IndexOf("\"", start);
                    if (start > 5 && end > start)
                    {
                        CurrentShoppingListId = Guid.Parse(json.Substring(start, end - start));
                        System.Diagnostics.Debug.WriteLine($"✅ Liste: {CurrentShoppingListId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ {ex.Message}");
                CurrentShoppingListId = null;
            }
        }

        // ✅ Test simple qui provoque JWT expired si token mort
        private async Task<bool> ValidateSessionAsync()
        {
            try
            {
                if (RecipesService == null || CurrentUserId == null)
                    return false;

                _ = await RecipesService.GetRecettesAsync(CurrentUserId.Value);
                return true;
            }
            catch (Exception ex)
            {
                if (IsJwtExpiredException(ex))
                    return false;

                System.Diagnostics.Debug.WriteLine("⚠️ ValidateSessionAsync: " + ex.Message);
                return true;
            }
        }

        // ------------------ NAV ------------------

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

        // ------------------ LOGOUT ------------------

        public void Logout(bool silent = false)
        {
            SecureStorage.Remove("remember_me");
            SecureStorage.Remove("sb_access_token");
            SecureStorage.Remove("sb_user_id");

            Supabase?.ClearSession();
            CurrentUserId = null;
            CurrentAccessToken = null;
            CurrentShoppingListId = null;

            RestClient = null;
            RecipesService = null;
            IngredientsService = null;
            ListeCoursesSupabaseService = null;
            RecetteIngredientsService = null;
            ShoppingListsService = null;

            if (!silent)
                ShowLogin();
            else
                MainThread.BeginInvokeOnMainThread(() => MainPage = RootPage.CreateLoginRoot());
        }

        // ✅ Assure que l'utilisateur est membre de la liste (utile pour bootstrap + RLS)
        private async Task EnsureMembershipAsync(Guid listId, Guid userId)
        {
            if (RestClient == null) return;

            var payload = new[]
            {
                new { list_id = listId, user_id = userId, role = "owner" }
            };

            try
            {
                await RestClient.PostAsync<object>(
                    "shopping_list_members",
                    payload,
                    returnRepresentation: false);
            }
            catch
            {
                // Déjà membre / conflit / policy : on ignore.
            }
        }

        // ------------------ MISC ------------------

        public Task HandleDeepLinkAsync(Uri uri) => Task.CompletedTask;

        public void SetSupabase(SupabaseService supabase) => Supabase = supabase;
        public void SetCurrentUserId(Guid userId) => CurrentUserId = userId;

        public void InitRestServices(string accessToken)
        {
            InitRestServicesInternal(accessToken);
        }
    }
}
