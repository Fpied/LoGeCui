using System;
using System.Threading.Tasks;
using System.Windows;
using LoGeCuiShared.Services;

namespace LoGeCui
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void BtnConnexion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Connexion en cours...", isError: false);

                string email = TxtEmail.Text.Trim();
                string password = TxtPassword.Password;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetStatus("Veuillez remplir tous les champs.", isError: true);
                    return;
                }

                var (success, accessToken, userIdString, error) = await App.SupabaseService.SignInAsync(email, password);


                if (!success)
                {
                    SetStatus(error ?? "Erreur de connexion", isError: true);
                    return;
                }
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    SetStatus("ID utilisateur invalide.", isError: true);
                    return;
                }

                App.InitRestServices(accessToken, userId);


                // ⬇️ ICI : import des recettes locales vers Supabase (une seule fois)
                await ImporterRecettesLocalVersSupabaseSiBesoinAsync(userId);

                // Vérifier les mises à jour (optionnel)
                await CheckForUpdatesAsync();

                // Ouvrir l'app
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur : {ex.Message}", isError: true);
            }
        }

        private async void BtnInscription_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Inscription en cours...", isError: false);

                string email = TxtEmail.Text.Trim();
                string password = TxtPassword.Password;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetStatus("Veuillez remplir tous les champs.", isError: true);
                    return;
                }

                if (password.Length < 6)
                {
                    SetStatus("Le mot de passe doit contenir au moins 6 caractères.", isError: true);
                    return;
                }

                var (success, userId, error) = await App.SupabaseService.SignUpAsync(email, password);

                if (success)
                {
                    MessageBox.Show(
                        "Compte créé avec succès !\n\nVous pouvez maintenant vous connecter.",
                        "Inscription réussie",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    TxtPassword.Password = "";
                    SetStatus("Compte créé ! Connectez-vous.", isError: false);
                }
                else
                {
                    SetStatus(error ?? "Erreur lors de l'inscription", isError: true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur : {ex.Message}", isError: true);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            TxtMessage.Text = message;
            TxtMessage.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Blue;
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                string url = ConfigurationHelper.GetSupabaseUrl();
                string key = ConfigurationHelper.GetSupabaseKey();

                var updateService = new LoGeCuiShared.Services.UpdateService(url, key);
                var updateInfo = await updateService.CheckForUpdateAsync("wpf", "1.0.0");

                if (updateInfo != null)
                {
                    var result = MessageBox.Show(
                        $"Une nouvelle version {updateInfo.Version} est disponible !\n\n" +
                        $"Notes de version :\n{updateInfo.ReleaseNotes}\n\n" +
                        $"Voulez-vous télécharger la mise à jour ?",
                        "Mise à jour disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = updateInfo.DownloadUrl,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch
            {
                // Erreur silencieuse
            }
        }

        private async Task ImporterRecettesLocalVersSupabaseSiBesoinAsync(Guid userId)
        {
            try
            {
                // 📁 Même chemin que ton ancien RecetteService (JSON)
                string dossier = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LoGeCui"
                );

                string cheminJson = System.IO.Path.Combine(dossier, "recettes.json");

                // ❌ Pas de fichier → rien à importer
                if (!System.IO.File.Exists(cheminJson))
                    return;

                // 🏷️ Fichier "flag" pour éviter de réimporter à chaque login
                string flagPath = System.IO.Path.Combine(dossier, "recettes_import_done.txt");
                if (System.IO.File.Exists(flagPath))
                    return;

                // 📖 Lecture du JSON
                var json = await System.IO.File.ReadAllTextAsync(cheminJson);

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var recettes = System.Text.Json.JsonSerializer
                    .Deserialize<List<LoGeCuiShared.Models.Recette>>(json, options)
                    ?? new List<LoGeCuiShared.Models.Recette>();

                if (recettes.Count == 0)
                {
                    // Rien à importer mais on marque comme fait
                    await System.IO.File.WriteAllTextAsync(flagPath, "empty");
                    return;
                }

                // 🔌 Service Supabase déjà initialisé dans App.InitRestServices
                var recipesService = App.RecipesService;
                if (recipesService == null)
                    return;

                // 🚀 Import / upsert
                foreach (var r in recettes)
                {
                    // ExternalId obligatoire pour l'upsert
                    if (string.IsNullOrWhiteSpace(r.ExternalId))
                        r.ExternalId = Guid.NewGuid().ToString("N");

                    // Nettoyage catégorie (important pour mobile)
                    if (!string.IsNullOrWhiteSpace(r.CategorieDb))
                        r.CategorieDb = r.CategorieDb.Trim();

                    await recipesService.UpsertRecetteAsync(userId, r);
                }

                // ✅ Marqueur "import terminé"
                await System.IO.File.WriteAllTextAsync(
                    flagPath,
                    $"done:{DateTime.UtcNow:O}"
                );
            }
            catch (Exception ex)
            {
                // ⚠️ Import non bloquant : on log mais on n'empêche pas le login
                System.Diagnostics.Debug.WriteLine("[ImportRecettes] " + ex);
            }
        }
    }
}

