using System.Windows;

namespace LoGeCui
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Temporaire : ouvrir MainWindow directement
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
