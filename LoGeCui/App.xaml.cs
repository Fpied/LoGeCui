using System;
using System.Windows;
using LoGeCuiShared.Services;

namespace LoGeCui
{
    public partial class App : Application
    {
        // AJOUTÉ : Instance unique de SupabaseService
        public static SupabaseService SupabaseService { get; private set; }
        public static SupabaseRestClient? RestClient { get; private set; }
        public static RecipesService? RecipesService { get; private set; }
        public static Guid? CurrentUserId { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // AJOUTÉ : Initialiser le service Supabase UNE SEULE FOIS
            string url = ConfigurationHelper.GetSupabaseUrl();
            string key = ConfigurationHelper.GetSupabaseKey(); // Remplace par ta clé anon

            // Créer l'instance UNE SEULE FOIS
            SupabaseService = new SupabaseService(url, key);

            // afficher login
            new LoginWindow().Show();

            // Ouvrir la fenêtre de connexion en premier
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        public static void InitRestServices(string accessToken, Guid userId)
        {
            string url = ConfigurationHelper.GetSupabaseUrl();
            string key = ConfigurationHelper.GetSupabaseKey();

            var client = new SupabaseRestClient(url, key);
            client.SetBearerToken(accessToken);

            RestClient = client;
            RecipesService = new RecipesService(client);
            CurrentUserId = userId;
        }
    }
}
