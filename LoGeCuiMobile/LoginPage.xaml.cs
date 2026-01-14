using LoGeCuiShared.Services;

namespace LoGeCuiMobile.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly SupabaseService _supabase;

        public LoginPage()
        {
            InitializeComponent();

            // ⚠️ REMPLACE PAR TES VRAIES CLÉS !
            string url = ConfigurationHelper.GetSupabaseUrl();
            string key = ConfigurationHelper.GetSupabaseKey();

            _supabase = new SupabaseService(url, key);
        }

        private async void BtnConnexion_Clicked(object sender, EventArgs e)
        {
            try
            {
                TxtMessage.Text = "Connexion en cours...";
                TxtMessage.TextColor = Colors.Blue;
                TxtMessage.IsVisible = true;

                string email = TxtEmail.Text?.Trim() ?? "";
                string password = TxtPassword.Text ?? "";

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    TxtMessage.Text = "Veuillez remplir tous les champs.";
                    TxtMessage.TextColor = Colors.Red;
                    return;
                }

                var (success, accessToken, userId, error) = await _supabase.SignInAsync(email, password);

                if (success)
                {
                    // Connexion réussie !
                    Application.Current.MainPage = new NavigationPage(new ListeCoursesPage());
                }
                else
                {
                    TxtMessage.Text = error ?? "Erreur de connexion";
                    TxtMessage.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                TxtMessage.Text = $"Erreur : {ex.Message}";
                TxtMessage.TextColor = Colors.Red;
                TxtMessage.IsVisible = true;
            }
        }

        private async void BtnInscription_Clicked(object sender, EventArgs e)
        {
            try
            {
                TxtMessage.Text = "Inscription en cours...";
                TxtMessage.TextColor = Colors.Blue;
                TxtMessage.IsVisible = true;

                string email = TxtEmail.Text?.Trim() ?? "";
                string password = TxtPassword.Text ?? "";

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    TxtMessage.Text = "Veuillez remplir tous les champs.";
                    TxtMessage.TextColor = Colors.Red;
                    return;
                }

                if (password.Length < 6)
                {
                    TxtMessage.Text = "Le mot de passe doit contenir au moins 6 caractères.";
                    TxtMessage.TextColor = Colors.Red;
                    return;
                }

                var (success, userId, error) = await _supabase.SignUpAsync(email, password);

                if (success)
                {
                    await DisplayAlert(
                        "Inscription réussie",
                        "Compte créé avec succès !\n\nVous pouvez maintenant vous connecter.",
                        "OK");

                    TxtPassword.Text = "";
                    TxtMessage.Text = "Compte créé ! Connectez-vous.";
                    TxtMessage.TextColor = Colors.Green;
                }
                else
                {
                    TxtMessage.Text = error ?? "Erreur lors de l'inscription";
                    TxtMessage.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                TxtMessage.Text = $"Erreur : {ex.Message}";
                TxtMessage.TextColor = Colors.Red;
                TxtMessage.IsVisible = true;
            }
        }
    }
}