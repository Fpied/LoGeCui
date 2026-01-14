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
        private readonly SupabaseService _supabase;

        public LoginWindow()
        {
            InitializeComponent();

            // ⚠️ REMPLACE PAR TES VRAIES CLÉS !
            string url = "https://wzctiypsadqktzcnswri.supabase.co";
            string key = "sb_publishable_ZFk8ONON5qMA0vZ3V0nVAg_TZZrO1F1";

            _supabase = new SupabaseService(url, key);
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

                var (success, accessToken, userId, error) = await _supabase.SignInAsync(email, password);

                if (success)
                {
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

                var (success, userId, error) = await _supabase.SignUpAsync(email, password);

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
    }
}
