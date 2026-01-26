using LoGeCuiShared.Services;
using Microsoft.Maui.Storage;

namespace LoGeCuiMobile.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly SupabaseService _supabase;

        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();

            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();

            _supabase = new SupabaseService(url, key);
        }

        private async void BtnConnexion_Clicked(object sender, EventArgs e)
        {
            try
            {
                SetStatus("Connexion en cours...", Colors.Blue);

                var email = TxtEmail.Text?.Trim() ?? "";
                var password = TxtPassword.Text ?? "";

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetStatus("Veuillez remplir tous les champs.", Colors.Red);
                    return;
                }

                var (success, accessToken, userId, error) =
                    await _supabase.SignInAsync(email, password);

                System.Diagnostics.Debug.WriteLine("ACCESS_TOKEN=" + accessToken);


                if (!success)
                {
                    SetStatus(error ?? "Erreur de connexion", Colors.Red);
                    await Application.Current.MainPage.DisplayAlert("DEBUG", "Bouton cliqué", "OK");

                    return;
                }

                // 1) Sauvegarde session pour désinscription / relance app
                await SecureStorage.SetAsync("sb_access_token", accessToken ?? "");
                await SecureStorage.SetAsync("sb_user_id", userId ?? "");

                // 2) Assure que le SupabaseService a bien la session (utile pour DeleteAccountAsync)
                _supabase.SetSession(accessToken!, userId!);

                // 3) Mise à dispo pour le reste de l'app + bascule Shell
                var app = (App)Application.Current;
                app.SetSupabase(_supabase);
                app.SetCurrentUserId(Guid.Parse(userId!));
                app.InitRestServices(accessToken!);
                app.ShowAppShell();
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur : {ex.Message}", Colors.Red);
            }
        }

        private async void BtnInscription_Clicked(object sender, EventArgs e)
        {
            try
            {
                SetStatus("Création du compte...", Colors.Blue);

                var email = TxtEmail.Text?.Trim() ?? "";
                var password = TxtPassword.Text ?? "";

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetStatus("Veuillez remplir tous les champs.", Colors.Red);
                    return;
                }

                var (success, accessToken, userId, error) =
                    await _supabase.SignUpThenSignInAsync(email, password);

                if (!success)
                {
                    SetStatus(error ?? "Erreur lors de l'inscription", Colors.Red);
                    return;
                }

                // Sauvegarde session
                await SecureStorage.SetAsync("sb_access_token", accessToken ?? "");
                await SecureStorage.SetAsync("sb_user_id", userId ?? "");

                _supabase.SetSession(accessToken!, userId!);

                var app = (App)Application.Current;
                app.SetSupabase(_supabase);
                app.SetCurrentUserId(Guid.Parse(userId!));
                app.InitRestServices(accessToken!);
                app.ShowAppShell();
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur : {ex.Message}", Colors.Red);
            }
        }

        private void TogglePassword(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            // Affiche/masque le mot de passe
            TxtPassword.IsPassword = !_isPasswordVisible;

            // Optionnel: change l'icône texte
            if (sender is Button btn)
                btn.Text = _isPasswordVisible ? "🙈" : "👁";

            // Optionnel Android: force refresh / curseur fin
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



