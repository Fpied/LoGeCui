using System;
using LoGeCuiMobile.Resources.Lang;
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
            LocalizationResourceManager.Instance["Settings_DeleteConfirmTitle"],
            LocalizationResourceManager.Instance["Settings_DeleteConfirmBody"],
            LocalizationResourceManager.Instance["Settings_DeleteConfirmYes"],
            LocalizationResourceManager.Instance["Dialog_Cancel"]);

        if (!confirm) return;

        var app = (App)Application.Current;

        if (app.Supabase == null)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                LocalizationResourceManager.Instance["Settings_SupabaseNull"],
                LocalizationResourceManager.Instance["Dialog_Ok"]);

            app.ShowLogin();
            return;
        }

        // 1) Lire la session stockée
        var token = await SecureStorage.GetAsync("sb_access_token");
        var userId = await SecureStorage.GetAsync("sb_user_id");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["SessionTitle"],
                LocalizationResourceManager.Instance["SessionExpired"],
                LocalizationResourceManager.Instance["Dialog_Ok"]);

            app.ShowLogin();
            return;
        }

        // 2) Réinjecter la session AVANT delete
        app.Supabase.SetSession(token, userId);

        // 3) Appel Edge Function
        var (ok, err) = await app.Supabase.DeleteAccountAsync();

        if (!ok)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                string.Format(LocalizationResourceManager.Instance["Settings_DeleteFailed"], err),
                LocalizationResourceManager.Instance["Dialog_Ok"]);

            if (!string.IsNullOrWhiteSpace(err) &&
                err.Contains("Invalid JWT", StringComparison.OrdinalIgnoreCase))
            {
                app.ShowLogin();
            }

            return;
        }

        // Nettoyage local
        SecureStorage.Remove("sb_access_token");
        SecureStorage.Remove("sb_user_id");

        await DisplayAlert(
            LocalizationResourceManager.Instance["Dialog_Ok"],
            LocalizationResourceManager.Instance["Settings_DeleteSuccess"],
            LocalizationResourceManager.Instance["Dialog_Ok"]);

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
