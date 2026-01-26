using System.Collections.ObjectModel;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;

namespace LoGeCuiMobile.Pages;

public partial class ResetPasswordPage : ContentPage
{
    private readonly string _accessToken;
    private readonly SupabaseService _supabase;

    public ResetPasswordPage(string accessToken)
    {
        InitializeComponent();
        _accessToken = accessToken;

        string url = ConfigurationHelper.GetSupabaseUrl();
        string key = ConfigurationHelper.GetSupabaseKey();
        _supabase = new SupabaseService(url, key);
    }

    private async void OnSubmit(object sender, EventArgs e)
    {
        var p1 = Pwd1.Text ?? "";
        var p2 = Pwd2.Text ?? "";

        if (string.IsNullOrWhiteSpace(p1) || p1.Length < 6)
        {
            Msg.Text = "Le mot de passe doit contenir au moins 6 caractères.";
            Msg.TextColor = Colors.Red;
            return;
        }

        if (p1 != p2)
        {
            Msg.Text = "Les mots de passe ne correspondent pas.";
            Msg.TextColor = Colors.Red;
            return;
        }

        Msg.Text = "Mise à jour en cours...";
        Msg.TextColor = Colors.Blue;

        var (success, error) = await _supabase.UpdatePasswordAsync(_accessToken, p1);

        if (success)
        {
            await DisplayAlert("✅ Terminé", "Votre mot de passe a été modifié. Vous pouvez vous connecter.", "OK");
            await Navigation.PopToRootAsync();
        }
        else
        {
            Msg.Text = error ?? "Impossible de modifier le mot de passe.";
            Msg.TextColor = Colors.Red;
        }
    }
}
