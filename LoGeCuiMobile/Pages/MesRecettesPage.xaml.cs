using LoGeCuiMobile.ViewModels;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;


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
            // Debug: on affiche une alerte, puis on ne charge pas la page
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert(
                    "Erreur",
                    "RecipesService est NULL (InitRestServices n'a pas été appelé après login).",
                    "OK"));

            return;
        }

        _vm = new MesRecettesViewModel(app.RecipesService);
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Si _vm est null, c’est qu’on a quitté le constructeur à cause d’un service non initialisé
        if (_vm == null)
            return;

        await _vm.LoadAsync();
    }
    private async void OnAjouterRecetteClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AjouterRecettePage());
    }

    private async void OnRecetteSelected(object sender, SelectionChangedEventArgs e)
    {
        var recette = e.CurrentSelection?.FirstOrDefault() as Recette;
        if (recette == null)
            return;

        ((CollectionView)sender).SelectedItem = null;

        var app = (App)Application.Current;
        if (app.RestClient == null)
        {
            await DisplayAlert("Erreur", "RestClient non initialisé.", "OK");
            return;
        }

        // ✅ Lire les ingrédients depuis la table recette_ingredients
        var ingSvc = new RecetteIngredientsService(app.RestClient);
        var items = await ingSvc.GetForRecetteAsync(recette.Id);

        var ingredientsText = (items.Count == 0)
            ? "(aucun ingrédient renseigné)"
            : string.Join("\n", items.Select(x =>
                string.IsNullOrWhiteSpace(x.quantite) && string.IsNullOrWhiteSpace(x.unite)
                    ? $"• {x.nom}"
                    : $"• {x.quantite} {x.unite} {x.nom}".Replace("  ", " ").Trim()
            ));

        await DisplayAlert(
            recette.Nom,
            $"Catégorie: {recette.CategorieDb}\n" +
            $"Temps: {recette.TempsPreparation} min\n\n" +
            $"Ingrédients:\n{ingredientsText}\n\n" +
            $"Instructions:\n{recette.Instructions}",
            "OK"
        );
    }

    private async void OnRecetteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Recette recette)
            return;

        var app = (App)Application.Current;
        if (app.RestClient == null)
        {
            await DisplayAlert("Erreur", "RestClient non initialisé.", "OK");
            return;
        }

        var ingSvc = new RecetteIngredientsService(app.RestClient);
        var items = await ingSvc.GetForRecetteAsync(recette.Id);

        var ingredientsText = (items.Count == 0)
            ? "(aucun ingrédient renseigné)"
            : string.Join("\n", items.Select(x =>
                string.IsNullOrWhiteSpace(x.quantite) && string.IsNullOrWhiteSpace(x.unite)
                    ? $"• {x.nom}"
                    : $"• {x.quantite} {x.unite} {x.nom}".Replace("  ", " ").Trim()
            ));

        await DisplayAlert(
            recette.Nom,
            $"Catégorie: {recette.CategorieDb}\n" +
            $"Temps: {recette.TempsPreparation} min\n\n" +
            $"Ingrédients:\n{ingredientsText}\n\n" +
            $"Instructions:\n{recette.Instructions}",
            "OK"
        );
    }

}

