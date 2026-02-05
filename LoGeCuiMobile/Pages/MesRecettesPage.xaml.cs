using System.Linq;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiMobile.ViewModels;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Pages;

public partial class MesRecettesPage : ContentPage
{
    private MesRecettesViewModel? _vm;
    private bool _suppressSelectAllEvent;

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

        // ✅ UN SEUL chargement propre
        await _vm.LoadAsync(forceRemote: true);

        // ✅ Puis recalcul des ingrédients dispo → bordures vert/rouge
        await _vm.RefreshAvailabilityAsync();

        RefreshSelectionUi();
    }

    private async void OnAjouterRecetteClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AjouterRecettePage());
    }

    // ---------------- TAP RECETTE ----------------
    private async void OnRecetteTapped(object sender, TappedEventArgs e)
    {
        Recette? recette = e.Parameter switch
        {
            Recette r => r,
            RecetteUi ui => ui.Model,
            _ => null
        };

        if (recette == null)
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

        await DisplayAlert(recette.Nom, details, LocalizationResourceManager.Instance["Dialog_Ok"]);
    }

    // ---------------- SELECTION MULTIPLE ----------------

    private void ChkToutSelectionnerRecettes_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_vm == null || _suppressSelectAllEvent) return;

        foreach (var r in _vm.Recettes)
            r.IsSelectedForDelete = e.Value;

        RefreshSelectionUi();
    }

    private void RecetteDeleteCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        RefreshSelectionUi();
    }

    private void RefreshSelectionUi()
    {
        if (_vm == null) return;

        var selectedCount = _vm.Recettes.Count(x => x.IsSelectedForDelete);

        if (SelectionInfoLabel != null)
            SelectionInfoLabel.Text = selectedCount == 0 ? "" : $"{selectedCount} sélectionné(s)";

        _suppressSelectAllEvent = true;
        try
        {
            if (ChkToutSelectionnerRecettes != null)
            {
                if (_vm.Recettes.Count == 0)
                {
                    ChkToutSelectionnerRecettes.IsEnabled = false;
                    ChkToutSelectionnerRecettes.IsChecked = false;
                }
                else
                {
                    ChkToutSelectionnerRecettes.IsEnabled = true;
                    ChkToutSelectionnerRecettes.IsChecked = _vm.Recettes.All(x => x.IsSelectedForDelete);
                }
            }
        }
        finally
        {
            _suppressSelectAllEvent = false;
        }
    }

    private async void BtnSupprimerSelection_Clicked(object sender, EventArgs e)
    {
        if (_vm == null) return;

        var selected = _vm.Recettes.Where(r => r.IsSelectedForDelete).ToList();
        if (selected.Count == 0)
        {
            await DisplayAlert("Info", "Aucune recette sélectionnée.", "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            "Confirmation",
            $"Supprimer {selected.Count} recette(s) sélectionnée(s) ?",
            "Oui",
            "Non");

        if (!confirm) return;

        await _vm.DeleteSelectedAsync(selected);
        RefreshSelectionUi();
    }

    private async void BtnSupprimerRecette_Clicked(object sender, EventArgs e)
    {
        if (_vm == null) return;
        if (sender is not Button btn) return;

        RecetteUi? recetteUi = btn.CommandParameter switch
        {
            RecetteUi ui => ui,
            Recette r => _vm.Recettes.FirstOrDefault(x => x.Model.Id == r.Id),
            _ => null
        };

        if (recetteUi == null) return;

        bool confirm = await DisplayAlert(
            "Confirmation",
            $"Supprimer '{recetteUi.Nom}' ?",
            "Oui",
            "Non");

        if (!confirm) return;

        await _vm.DeleteOneAsync(recetteUi);
        RefreshSelectionUi();
    }

    // ---------------- MODIFIER ----------------

    private async void BtnModifierRecette_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;

        Recette? recette = btn.CommandParameter switch
        {
            Recette r => r,
            RecetteUi ui => ui.Model,
            _ => null
        };

        if (recette == null) return;

        await Navigation.PushAsync(new AjouterRecettePage(recette));
    }

    // ---------------- AJOUTER MANQUANTS ----------------

    private async void BtnAjouterManquants_Clicked(object sender, EventArgs e)
    {
        if (_vm == null) return;
        if (sender is not Button btn) return;

        RecetteUi? recetteUi = btn.CommandParameter switch
        {
            RecetteUi ui => ui,
            Recette r => _vm.Recettes.FirstOrDefault(x => x.Model.Id == r.Id),
            _ => null
        };

        if (recetteUi == null) return;

        var missing = await _vm.GetMissingIngredientsForRecipeAsync(recetteUi.Model);
        if (missing == null)
        {
            await DisplayAlert("Info", "Impossible de calculer les ingrédients (offline ou données manquantes).", "OK");
            return;
        }

        if (missing.Count == 0)
        {
            await DisplayAlert("Info", "Tu as déjà tous les ingrédients pour cette recette ✅", "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            "Ajouter à la liste",
            "Ajouter ces ingrédients manquants ?\n- " + string.Join("\n- ", missing),
            "Oui",
            "Non");

        if (!confirm) return;

        var ok = await _vm.SendMissingToShoppingAsync(missing);

        if (ok)
            await DisplayAlert("Succès", $"{missing.Count} ingrédient(s) ajouté(s) à la liste de courses.", "OK");
        else
            await DisplayAlert("Erreur", "Impossible d’ajouter à la liste de courses.", "OK");
    }
}
