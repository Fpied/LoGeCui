using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.Controls;

namespace LoGeCuiMobile
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

    // Configuration du Deep Link pour la confirmation email
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "logecui",
        DataHost = "confirm",
        AutoVerify = true)]

    // Deep Link - reset password
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "logecui",
        DataHost = "reset-password")]

    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Gérer le deep link si l'app est lancée via un lien
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            if (intent != null)
            {
                Intent = intent;
                HandleIntent(intent);
            }
        }

        private void HandleIntent(Intent? intent)
        {
            var data = intent?.DataString;
            if (string.IsNullOrEmpty(data))
                return;

            var uri = new Uri(data);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Microsoft.Maui.Controls.Application.Current is App app)
                    await app.HandleDeepLinkAsync(uri);
            });
        }
    }
}