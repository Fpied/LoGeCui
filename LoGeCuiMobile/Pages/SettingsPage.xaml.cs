using System;
using LoGeCuiShared.Services;
using Microsoft.Maui.Storage;

namespace LoGeCuiMobile.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnDesinscriptionClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Désinscription",
            "Es-tu sûr de vouloir supprimer ton compte ? Cette action est irréversible.",
            "Oui, supprimer",
            "Annuler");

        if (!confirm) return;

        var app = (App)Application.Current;

        if (app.Supabase == null)
        {
            await DisplayAlert("Erreur", "Supabase non initialisé. Reconnectez-vous.", "OK");
            app.ShowLogin();
            return;
        }

        // 1) Lire la session stockée
        var token = await SecureStorage.GetAsync("sb_access_token");
        var userId = await SecureStorage.GetAsync("sb_user_id");

        
        System.Diagnostics.Debug.WriteLine($"TOKEN_START={token?.Substring(0, Math.Min(10, token.Length))}");
        System.Diagnostics.Debug.WriteLine($"TOKEN_LEN={token?.Length ?? 0}");
        System.Diagnostics.Debug.WriteLine($"APP_SUPABASE_URL={ConfigurationHelper.GetSupabaseUrl()}");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Session", "Session absente/expirée. Veuillez vous reconnecter.", "OK");
            app.ShowLogin();
            return;
        }

        // 2) CRUCIAL : réinjecter la session dans SupabaseService AVANT delete
        app.Supabase.SetSession(token, userId);

        // 3) Appel Edge Function
        var (ok, err) = await app.Supabase.DeleteAccountAsync();

        if (!ok)
        {
            await DisplayAlert("Erreur", $"Suppression impossible : {err}", "OK");

            // Si JWT invalide → forcer reconnexion
            if (!string.IsNullOrWhiteSpace(err) &&
                err.Contains("Invalid JWT", StringComparison.OrdinalIgnoreCase))
            {
                app.ShowLogin();
            }

            return;
        }

        // Nettoyage local (bonne pratique après suppression)
        SecureStorage.Remove("sb_access_token");
        SecureStorage.Remove("sb_user_id");

        await DisplayAlert("OK", "Votre compte a été supprimé.", "OK");
        app.ShowLogin();
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        var app = (App)Application.Current;

        app.Supabase?.ClearSession();

        SecureStorage.Remove("sb_access_token");
        SecureStorage.Remove("sb_user_id");

        app.ShowLogin();
    }
}




