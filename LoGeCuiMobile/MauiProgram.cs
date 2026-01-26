using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace LoGeCuiMobile
{
    public static class MauiProgram
    {
        public static IConfiguration Configuration { get; private set; } = new ConfigurationBuilder().Build();

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ✅ Charge appsettings.json (MauiAsset) sans faire crasher l'app si absent
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json")
                                             .GetAwaiter()
                                             .GetResult();

                builder.Configuration.AddJsonStream(stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========== CONFIG ERROR ==========");
                System.Diagnostics.Debug.WriteLine("Impossible de charger appsettings.json depuis le package MAUI.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("==================================");
                // On continue volontairement pour éviter le crash.
            }

            Configuration = builder.Configuration;

            // ✅ IMPORTANT: on force le namespace exact (évite conflits)
            try
            {
                LoGeCuiShared.Services.ConfigurationHelper.Configure(key => Configuration[key]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========== CONFIGHELPER ERROR ==========");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("========================================");
                // On continue; l'erreur réelle ressortira quand une clé sera demandée.
            }

            return builder.Build();
        }
    }
}

