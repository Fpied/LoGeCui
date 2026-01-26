namespace LoGeCuiMobile;

public static class RootPage
{
    public static Page CreateLoginRoot()
        => new NavigationPage(new Pages.LoginPage());

    public static Page CreateAppShellRoot()
        => new AppShell();
}