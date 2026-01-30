using LoGeCuiMobile.Resources.Lang;
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
            Msg.Text = LocalizationResourceManager.Instance["Reset_PwdMinLength"];
            Msg.TextColor = Colors.Red;
            return;
        }

        if (p1 != p2)
        {
            Msg.Text = LocalizationResourceManager.Instance["Reset_PwdMismatch"];
            Msg.TextColor = Colors.Red;
            return;
        }

        Msg.Text = LocalizationResourceManager.Instance["Reset_Updating"];
        Msg.TextColor = Colors.Blue;

        var (success, error) = await _supabase.UpdatePasswordAsync(_accessToken, p1);

        if (success)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["Reset_DoneTitle"],
                LocalizationResourceManager.Instance["Reset_DoneBody"],
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );

            await Navigation.PopToRootAsync();
        }
        else
        {
            Msg.Text = string.IsNullOrWhiteSpace(error)
                ? LocalizationResourceManager.Instance["Reset_Failed"]
                : error;

            Msg.TextColor = Colors.Red;
        }
    }
}
