using LoGeCuiMobile.Pages;
using LoGeCuiMobile; // si ForgotPasswordPage est à la racine du projet

namespace LoGeCuiMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ListeCoursesPage), typeof(ListeCoursesPage));
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));

    }
}
