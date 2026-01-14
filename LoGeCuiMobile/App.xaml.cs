using Microsoft.Extensions.DependencyInjection;

namespace LoGeCuiMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new Pages.ListeCoursesPage());
        }
    }
}