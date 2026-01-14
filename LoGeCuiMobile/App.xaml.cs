using Microsoft.Extensions.DependencyInjection;

namespace LoGeCuiMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Ouvrir la page de connexion en premier
            MainPage = new NavigationPage(new Pages.LoginPage());
        }
    }
}