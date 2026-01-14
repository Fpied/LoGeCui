using System.Windows;

namespace LoGeCui
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ouvrir la fenêtre de connexion en premier
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
