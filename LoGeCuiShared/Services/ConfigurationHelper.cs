using System;

namespace LoGeCuiShared.Services
{
    public static class ConfigurationHelper
    {
        private static Func<string, string?>? _get;

        // Appelé 1 fois au démarrage par le projet MAUI
        public static void Configure(Func<string, string?> getValue)
        {
            _get = getValue ?? throw new ArgumentNullException(nameof(getValue));
        }

        private static string Get(string key)
        {
            if (_get == null)
                throw new InvalidOperationException("ConfigurationHelper n'est pas configuré. Appelez ConfigurationHelper.Configure(...) au démarrage.");

            return _get(key) ?? throw new InvalidOperationException($"Clé manquante : {key}");
        }

        // Versions sûres qui renvoient null si la clé est absente (pour permettre un démarrage sans crash)
        public static string? GetSupabaseUrlSafe() => _get?.Invoke("Supabase:Url");
        public static string? GetSupabaseKeySafe() => _get?.Invoke("Supabase:AnonKey");
        public static string? GetOcrApiKeySafe() => _get?.Invoke("OCR:ApiKey");

        // Les anciennes méthodes conservent le comportement strict (throw) — vous pouvez les remplacer par les versions Safe si nécessaire.
        public static string GetSupabaseUrl() => Get("Supabase:Url");
        public static string GetSupabaseKey() => Get("Supabase:AnonKey");
        public static string GetOcrApiKey() => Get("OCR:ApiKey");
    }
}


