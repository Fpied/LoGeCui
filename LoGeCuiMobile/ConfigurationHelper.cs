using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;
using System;
using System.IO;

namespace LoGeCuiMobile
{
    public static class ConfigurationHelper
    {
        private static IConfiguration? _configuration;
        private static readonly object _lock = new();

        private static IConfiguration Configuration
        {
            get
            {
                if (_configuration != null)
                    return _configuration;

                lock (_lock)
                {
                    if (_configuration != null)
                        return _configuration;

                    try
                    {
                        // Lire le fichier depuis le package MAUI
                        using var streamTask = FileSystem.OpenAppPackageFileAsync("appsettings.json");
                        streamTask.Wait();
                        using var stream = streamTask.Result;

                        // Copie en mémoire pour éviter tout souci de stream non seekable / lecture partielle
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        ms.Position = 0;

                        var builder = new ConfigurationBuilder()
                            .AddJsonStream(ms);

                        _configuration = builder.Build();

#if DEBUG
                        System.Diagnostics.Debug.WriteLine("[CONFIG] appsettings.json chargé");
                        System.Diagnostics.Debug.WriteLine("[CONFIG] Supabase:Url=" + (_configuration["Supabase:Url"] ?? "<null>"));
                        System.Diagnostics.Debug.WriteLine("[CONFIG] OCR:ApiKey present=" + (!string.IsNullOrWhiteSpace(_configuration["OCR:ApiKey"])));
#endif

                        return _configuration;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            "Impossible de charger appsettings.json depuis le package MAUI. " +
                            "Vérifie qu'il est bien dans LoGeCuiMobile, Build Action = MauiAsset, et que tu as fait Clean/Rebuild. " +
                            $"Détail: {ex.Message}", ex);
                    }
                }
            }
        }


        // =========================
        // SUPABASE
        // =========================

        public static string GetSupabaseUrl()
            => Configuration["Supabase:Url"]
               ?? throw new Exception("Supabase:Url manquant dans appsettings.json");

        public static string GetSupabaseKey()
            => Configuration["Supabase:Key"]
               ?? throw new Exception("Supabase:Key manquant dans appsettings.json");

        // =========================
        // OCR
        // =========================

        public static string GetOcrApiKey()
        {
            var key = Configuration["OCR:ApiKey"];

            if (string.IsNullOrWhiteSpace(key))
                throw new Exception("OCR:ApiKey manquant dans appsettings.json");

            return key;
        }
    }
}




