using System;
using System.Linq;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiShared.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;

namespace LoGeCuiMobile.Pages;

public partial class AjouterIngredientPage : ContentPage
{
    public AjouterIngredientPage()
    {
        InitializeComponent();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NomEntry.Text))
            {
                await DisplayAlert(
                    LocalizationResourceManager.Instance["ErrorTitle"],
                    LocalizationResourceManager.Instance["Ingredients_Add_NameRequired"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );
                return;
            }

            var ingredient = new Ingredient
            {
                Nom = NomEntry.Text.Trim(),
                Quantite = QuantiteEntry.Text?.Trim() ?? "",
                Unite = UniteEntry.Text?.Trim() ?? "",
                EstDisponible = DisponibleCheckBox.IsChecked
            };

            // ✅ 1) Toujours sauver dans le cache local (offline-first)
            await SaveIngredientToLocalCacheAsync(ingredient);

            // ✅ 2) Si internet + connecté => push Supabase
            var app = (App)Application.Current;

            var hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

            if (hasInternet && app.IsConnected && app.IngredientsService != null && app.CurrentUserId != null)
            {
                await app.IngredientsService.CreateIngredientAsync(app.CurrentUserId.Value, ingredient);

            }

            // ✅ Retour
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
        }
    }

    private static async System.Threading.Tasks.Task SaveIngredientToLocalCacheAsync(Ingredient ingredient)
    {
        // Récupère cache local
        var local = await App.LocalDb.GetIngredientsAsync();
        var models = local.Select(x => x.ToModel()).ToList();

        // Evite doublon par Nom (tu peux adapter sur ExternalId si tu en as)
        bool exists = models.Any(x =>
            string.Equals(x.Nom?.Trim(), ingredient.Nom?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!exists)
            models.Add(ingredient);

        await App.LocalDb.SaveIngredientsAsync(models);
    }
}
