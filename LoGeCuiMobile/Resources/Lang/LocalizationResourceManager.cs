using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace LoGeCuiMobile.Resources.Lang;

public class LocalizationResourceManager : INotifyPropertyChanged
{
    public static LocalizationResourceManager Instance { get; } = new();

    private readonly ResourceManager _resourceManager = AppResources.ResourceManager;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public void SetCulture(CultureInfo culture)
    {
        if (culture == null) return;

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // ✅ Notifie toute l’UI : toutes les bindings se réévaluent
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
