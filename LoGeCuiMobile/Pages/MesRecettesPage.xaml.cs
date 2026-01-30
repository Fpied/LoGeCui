using System.Linq;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiMobile.ViewModels;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Pages;

public partial class MesRecettesPage : ContentPage
{
    private MesRecettesViewModel? _vm;

    public MesRecettesPage()
    {
        InitializeComponent();

        var app = (App)Application.Current;

        if (app.RecipesService == null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert(
                    LocalizationResourceManager.Instance["ErrorTitle"],
                    LocalizationResourceManager.Instance["Recipes_ServiceNull"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                ));

            return;
        }

        _vm = new MesRecettesViewModel(app.RecipesService);
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_vm == null)
            return;

        await _vm.LoadAsync();
    }

    private async void OnAjouterRecetteClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AjouterRecettePage());
    }

    // ✅ Tap fiable sur une recette
    private async void OnRecetteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Recette recette)
            return;

        var app = (App)Application.Current;

        if (app.RecetteIngredientsService == null)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                LocalizationResourceManager.Instance["Recipes_IngredientsServiceNull"],
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
            return;
        }

        // 🔎 Charge les ingrédients depuis la table recette_ingredients
        var items = await app.RecetteIngredientsService.GetForRecetteAsync(recette.Id);

        var ingredientsText = (items.Count == 0)
            ? LocalizationResourceManager.Instance["Recipes_NoIngredients"]
            : string.Join("\n", items.Select(x =>
                string.IsNullOrWhiteSpace(x.quantite) && string.IsNullOrWhiteSpace(x.unite)
                    ? $"• {x.nom}"
                    : $"• {x.quantite} {x.unite} {x.nom}".Replace("  ", " ").Trim()
            ));

        var details = string.Format(
            LocalizationResourceManager.Instance["Recipes_DetailsFormat"],
            recette.CategorieDb,
            recette.TempsPreparation,
            ingredientsText,
            recette.Instructions ?? ""
        );

        await DisplayAlert(
            recette.Nom,
            details,
            LocalizationResourceManager.Instance["Dialog_Ok"]
        );
    }
}
