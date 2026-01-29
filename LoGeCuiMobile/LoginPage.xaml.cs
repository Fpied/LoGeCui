using LoGeCuiMobile; // ForgotPasswordPage / RootPage
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiShared.Services;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace LoGeCuiMobile.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly SupabaseService _supabase;
        private bool _isPasswordVisible = false;

        // Empêche SelectedIndexChanged pendant l'init
        private bool _isInitializingLanguage = false;

        public LoginPage()
        {
            InitializeComponent();

            _isInitializingLanguage = true;
            InitLanguagePickerSelection();
            _isInitializingLanguage = false;

            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();
            _supabase = new SupabaseService(url, key);

            _ = LoadSavedUserAsync();
        }

        // =========================
        // LANGUE (Picker)
        // =========================
        private void LanguagePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isInitializingLanguage)
                return;

            if (LanguagePicker.SelectedIndex < 0)
                return;

            var langCode = LanguagePicker.SelectedIndex switch
            {
                0 => "fr",
                1 => "en",
                2 => "de",
                3 => "es",
                4 => "it",
                5 => "ar",
                6 => "pl",
                7 => "tr",
                8 => "ro",
                9 => "cs",
                10 => "ko",
                11 => "ja",
                12 => "th",
                13 => "hi",
                _ => "fr"
            };

            var current = Preferences.Get("app_language", "fr");
            if (string.Equals(current, langCode, StringComparison.OrdinalIgnoreCase))
                return;

            Preferences.Set("app_language", langCode);

            var culture = new CultureInfo(langCode);

            // ✅ Applique la culture + notifie l'UI (si tu utilises TranslateExtension)
            LocalizationResourceManager.Instance.SetCulture(culture);

            // ⚠️ Si ton XAML est encore en x:Static et que tu veux quand même voir le changement,
            // dé-commente ce bloc (mais évite de le faire pendant l'init) :
            // MainThread.BeginInvokeOnMainThread(() =>
            // {
            //     Application.Current.MainPage = RootPage.CreateLoginRoot();
            // });
        }

        private void InitLanguagePickerSelection()
        {
            var lang = Preferences.Get("app_language", "fr");

            var index = lang switch
            {
                "fr" => 0,
                "en" => 1,
                "de" => 2,
                "es" => 3,
                "it" => 4,
                "ar" => 5,
                "pl" => 6,
                "tr" => 7,
                "ro" => 8,
                "cs" => 9,
                "ko" => 10,
                "ja" => 11,
                "th" => 12,
                "hi" => 13,
                _ => 0
            };

            LanguagePicker.SelectedIndex = index;
        }

        // =========================
        // LOGIN
        // =========================
        private async void BtnConnexion_Clicked(object sender, EventArgs e)
        {
            try
            {
                SetStatus(AppResources.Connecting, Colors.Blue);

                var email = TxtEmail.Text?.Trim() ?? string.Empty;
                var password = TxtPassword.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetStatus(AppResources.FillAllFields, Colors.Red);
                    return;
                }

                var (success, accessToken, userId, error) =
                    await _supabase.SignInAsync(email, password);

                if (!success)
                {
                    SetStatus(error ?? AppResources.ConnectionError, Colors.Red);
                    await DisplayAlert(AppResources.ErrorTitle, AppResources.BadPassword, "OK");
                    return;
                }

                accessToken ??= string.Empty;
                userId ??= string.Empty;

                if (ChkRememberMe?.IsChecked == true)
                {
                    await SecureStorage.SetAsync("remember_me", "true");
                    await SecureStorage.SetAsync("sb_access_token", accessToken);
                    await SecureStorage.SetAsync("sb_user_id", userId);
                    await SecureStorage.SetAsync("user_email", email);
                }
                else
                {
                    SecureStorage.Remove("remember_me");
                    SecureStorage.Remove("sb_access_token");
                    SecureStorage.Remove("sb_user_id");
                    SecureStorage.Remove("user_email");
                }

                if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(userId))
                    _supabase.SetSession(accessToken, userId);

                var app = (App)Application.Current;
                app.SetSupabase(_supabase);

                if (Guid.TryParse(userId, out var guidUserId))
                    app.SetCurrentUserId(guidUserId);

                app.InitRestServices(accessToken);
                app.ShowAppShell();
            }
            catch (Exception ex)
            {
                SetStatus($"{AppResources.ErrorTitle} : {ex.Message}", Colors.Red);
            }
        }

        private async Task LoadSavedUserAsync()
        {
            try
            {
                var rememberMe = await SecureStorage.GetAsync("remember_me");
                if (!string.Equals(rememberMe, "true", StringComparison.OrdinalIgnoreCase))
                    return;

                var email = await SecureStorage.GetAsync("user_email");
                if (!string.IsNullOrEmpty(email))
                {
                    TxtEmail.Text = email;
                    ChkRememberMe.IsChecked = true;
                }
            }
            catch
            {
                // ignore SecureStorage errors
            }
        }

        private async void BtnInscription_Clicked(object sender, EventArgs e)
        {
            try
            {
                SetStatus(AppResources.CreatingAccount, Colors.Blue);

                var email = TxtEmail.Text?.Trim() ?? string.Empty;
                var password = TxtPassword.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetStatus(AppResources.FillAllFields, Colors.Red);
                    return;
                }

                var (success, accessToken, userId, error) =
                    await _supabase.SignUpThenSignInAsync(email, password);

                if (!success)
                {
                    SetStatus(error ?? AppResources.SignupError, Colors.Red);
                    return;
                }

                accessToken ??= string.Empty;
                userId ??= string.Empty;

                if (ChkRememberMe?.IsChecked == true)
                {
                    await SecureStorage.SetAsync("remember_me", "true");
                    await SecureStorage.SetAsync("sb_access_token", accessToken);
                    await SecureStorage.SetAsync("sb_user_id", userId);
                    await SecureStorage.SetAsync("user_email", email);
                }
                else
                {
                    SecureStorage.Remove("remember_me");
                    SecureStorage.Remove("sb_access_token");
                    SecureStorage.Remove("sb_user_id");
                    SecureStorage.Remove("user_email");
                }

                if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(userId))
                    _supabase.SetSession(accessToken, userId);

                var app = (App)Application.Current;
                app.SetSupabase(_supabase);

                if (Guid.TryParse(userId, out var guidUserId))
                    app.SetCurrentUserId(guidUserId);

                app.InitRestServices(accessToken);
                app.ShowAppShell();
            }
            catch (Exception ex)
            {
                SetStatus($"{AppResources.ErrorTitle} : {ex.Message}", Colors.Red);
            }
        }

        private async void BtnForgotPassword_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new ForgotPasswordPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert(AppResources.ErrorTitle, ex.ToString(), "OK");
            }
        }

        private void TogglePassword(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            TxtPassword.IsPassword = !_isPasswordVisible;

            if (sender is Button btn)
                btn.Text = _isPasswordVisible ? "🙈" : "👁";

            if (!string.IsNullOrEmpty(TxtPassword.Text))
            {
                var t = TxtPassword.Text;
                TxtPassword.Text = string.Empty;
                TxtPassword.Text = t;
            }
        }

        private void SetStatus(string message, Color color)
        {
            TxtMessage.Text = message;
            TxtMessage.TextColor = color;
            TxtMessage.IsVisible = true;
        }
    }
}
