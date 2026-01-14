using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace LoGeCuiMobile
{
    public static class ConfigurationHelper
    {
        private static IConfiguration? _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    using var stream = assembly.GetManifestResourceStream("LoGeCuiMobile.appsettings.json");

                    if (stream == null)
                        throw new Exception("Fichier appsettings.json introuvable");

                    var builder = new ConfigurationBuilder()
                        .AddJsonStream(stream);

                    _configuration = builder.Build();
                }
                return _configuration;
            }
        }

        public static string GetSupabaseUrl()
        {
            return Configuration["Supabase:Url"] ?? throw new Exception("Supabase URL non configurée");
        }

        public static string GetSupabaseKey()
        {
            return Configuration["Supabase:Key"] ?? throw new Exception("Supabase Key non configurée");
        }
    }
}