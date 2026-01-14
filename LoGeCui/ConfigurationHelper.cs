using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace LoGeCui
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
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

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
