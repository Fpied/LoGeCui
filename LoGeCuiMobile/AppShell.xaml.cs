using LoGeCuiMobile.Pages;

namespace LoGeCuiMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ListeCoursesPage), typeof(ListeCoursesPage));
    }
}
