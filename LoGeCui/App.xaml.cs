using System.Windows;
using LoGeCuiShared.Services;

namespace LoGeCui
{
    public partial class App : Application
    {
        // AJOUTÉ : Instance unique de SupabaseService
        public static SupabaseService SupabaseService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // AJOUTÉ : Initialiser le service Supabase UNE SEULE FOIS
            string url = ConfigurationHelper.GetSupabaseUrl();
            string key = ConfigurationHelper.GetSupabaseKey(); // Remplace par ta clé anon

            // Créer l'instance UNE SEULE FOIS
            SupabaseService = new SupabaseService(url, key);

            // Ouvrir la fenêtre de connexion en premier
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
