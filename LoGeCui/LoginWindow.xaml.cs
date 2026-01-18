using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LoGeCuiShared.Services;

namespace LoGeCui
{
    public partial class LoginWindow : Window
    {
        // SUPPRIMÉ : private readonly SupabaseService _supabase;

        public LoginWindow()
        {
            InitializeComponent();

            // SUPPRIMÉ : Création d'une nouvelle instance
            // _supabase = new SupabaseService(url, key);
        }

        private async void BtnConnexion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtMessage.Text = "Connexion en cours...";
                TxtMessage.Foreground = System.Windows.Media.Brushes.Blue;

                string email = TxtEmail.Text.Trim();
                string password = TxtPassword.Password;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    TxtMessage.Text = "Veuillez remplir tous les champs.";
                    TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // MODIFIÉ : Utiliser le singleton App.SupabaseService
                var (success, accessToken, userId, error) = await App.SupabaseService.SignInAsync(email, password);

                if (success)
                {
                    // Vérifier les mises à jour
                    await CheckForUpdatesAsync();

                    // Connexion réussie !
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    TxtMessage.Text = error ?? "Erreur de connexion";
                    TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                TxtMessage.Text = $"Erreur : {ex.Message}";
                TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private async void BtnInscription_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtMessage.Text = "Inscription en cours...";
                TxtMessage.Foreground = System.Windows.Media.Brushes.Blue;

                string email = TxtEmail.Text.Trim();
                string password = TxtPassword.Password;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    TxtMessage.Text = "Veuillez remplir tous les champs.";
                    TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (password.Length < 6)
                {
                    TxtMessage.Text = "Le mot de passe doit contenir au moins 6 caractères.";
                    TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // MODIFIÉ : Utiliser le singleton App.SupabaseService
                var (success, userId, error) = await App.SupabaseService.SignUpAsync(email, password);

                if (success)
                {
                    MessageBox.Show(
                        "Compte créé avec succès !\n\nVous pouvez maintenant vous connecter.",
                        "Inscription réussie",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    TxtPassword.Password = "";
                    TxtMessage.Text = "Compte créé ! Connectez-vous.";
                    TxtMessage.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    TxtMessage.Text = error ?? "Erreur lors de l'inscription";
                    TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                TxtMessage.Text = $"Erreur : {ex.Message}";
                TxtMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
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
    }
}
