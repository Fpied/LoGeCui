using LoGeCuiMobile.Pages;
using LoGeCuiShared.Services;

namespace LoGeCuiMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new LoginPage();
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            // Gérer le deep link de confirmation email
            if (uri.Host == "confirm" || uri.AbsolutePath.Contains("/confirm"))
            {
                HandleEmailConfirmation(uri);
            }
        }

        private async void HandleEmailConfirmation(Uri uri)
        {
            try
            {
                // Extraire les tokens de l'URL
                // Format: logecui://confirm/*?access_token=xxx&refresh_token=yyy&type=signup

                System.Diagnostics.Debug.WriteLine($"[DeepLink] URI reçu: {uri}");

                string? accessToken = null;
                string? refreshToken = null;

                // Parser manuellement les query parameters
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var queryString = uri.Query.TrimStart('?');
                    var parameters = queryString.Split('&');

                    foreach (var param in parameters)
                    {
                        var keyValue = param.Split('=');
                        if (keyValue.Length == 2)
                        {
                            var key = Uri.UnescapeDataString(keyValue[0]);
                            var value = Uri.UnescapeDataString(keyValue[1]);

                            if (key == "access_token")
                                accessToken = value;
                            else if (key == "refresh_token")
                                refreshToken = value;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DeepLink] Access token trouvé: {!string.IsNullOrEmpty(accessToken)}");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    // Afficher un message de succès
                    await MainPage.DisplayAlert(
                        "✅ Email confirmé !",
                        "Votre email a été confirmé avec succès.\nVous pouvez maintenant vous connecter.",
                        "OK");

                    // Rediriger vers la page de connexion
                    MainPage = new LoginPage();
                }
                else
                {
                    await MainPage.DisplayAlert(
                        "Erreur",
                        "Lien de confirmation invalide.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Erreur: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Stack: {ex.StackTrace}");

                await MainPage.DisplayAlert(
                    "Erreur",
                    "Une erreur est survenue lors de la confirmation de votre email.",
                    "OK");
            }
        }
    }
}