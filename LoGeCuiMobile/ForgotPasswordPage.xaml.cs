using LoGeCuiShared.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;

namespace LoGeCuiMobile
{
    // Optionnel : permet de pré-remplir l'email si tu navigues avec ?email=...
    [QueryProperty(nameof(PrefillEmail), "email")]
    public partial class ForgotPasswordPage : ContentPage
    {
        private readonly SupabaseService _supabaseService;

        // Valeur passée via Shell : ?email=...
        public string PrefillEmail
        {
            set
            {
                // Evite crash si EmailEntry n'est pas encore prêt
                try
                {
                    var decoded = Uri.UnescapeDataString(value ?? "");
                    if (!string.IsNullOrWhiteSpace(decoded))
                        EmailEntry.Text = decoded;
                }
                catch { /* ignore */ }
            }
        }

        public ForgotPasswordPage()
        {
            InitializeComponent();

            var url = ConfigurationHelper.GetSupabaseUrl();
            var key = ConfigurationHelper.GetSupabaseKey();

            _supabaseService = new SupabaseService(url, key);

            StatusLabel.Text = "";
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            try
            {
                // Sécurité si un contrôle est null (mauvais x:Name / x:Class)
                if (EmailEntry == null || SendButton == null || StatusLabel == null)
                {
                    await DisplayAlert("Erreur", "Interface non initialisée (XAML). Vérifie x:Class et x:Name.", "OK");
                    return;
                }

                var email = (EmailEntry.Text ?? "").Trim();

                // ✅ Validation obligatoire
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                {
                    StatusLabel.TextColor = Colors.Red;
                    StatusLabel.Text = "Veuillez saisir une adresse email valide.";
                    return;
                }

                SendButton.IsEnabled = false;
                StatusLabel.TextColor = Colors.Black;
                StatusLabel.Text = "Envoi en cours…";

                var result = await _supabaseService.SendPasswordResetAsync(email);

                if (result.success)
                {
                    StatusLabel.TextColor = Colors.Green;
                    StatusLabel.Text = "Email envoyé ✅ Vérifiez votre boîte mail (et les spams).";
                }
                else
                {
                    StatusLabel.TextColor = Colors.Red;
                    StatusLabel.Text = result.error ?? "Impossible d'envoyer l'email.";
                }
            }
            catch (Exception ex)
            {
                // ✅ évite le crash et donne une erreur lisible
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.Text = "Erreur : " + ex.Message;
            }
            finally
            {
                if (SendButton != null)
                    SendButton.IsEnabled = true;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch
            {
                // au cas où
                await Shell.Current?.GoToAsync("..");
            }
        }
    }
}
