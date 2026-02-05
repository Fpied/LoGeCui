using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LoGeCuiMobile.Models;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiShared.Models;
using Microsoft.Maui.Networking;


namespace LoGeCuiMobile.Pages;

public partial class MesIngredientsPage : ContentPage
{
    private readonly ObservableCollection<IngredientUi> _ingredients = new();

    private bool _isLoading;
    private bool _suppressSelectAllEvent;

    public MesIngredientsPage()
    {
        InitializeComponent();
        IngredientsCollection.ItemsSource = _ingredients;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadIngredientsAsync();
    }

    private async Task LoadIngredientsAsync()
    {
        if (_isLoading) return;

        try
        {
            _isLoading = true;

            // 0) Toujours tenter d'afficher le cache local en premier (offline-friendly)
            var local = await App.LocalDb.GetIngredientsAsync();
            _ingredients.Clear();
            foreach (var li in local)
                _ingredients.Add(new IngredientUi(li.ToModel()));

            RefreshSelectAllCheckBox();

            // 1) Si pas connecté (session) -> on s'arrête après le cache
            if (Application.Current is not App app || !app.IsConnected || app.IngredientsService == null || app.CurrentUserId == null)
                return;

            // 2) Si pas internet -> on s'arrête après le cache
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;

            var userId = app.CurrentUserId.Value;

            // 3) Refresh depuis Supabase
            var remote = await app.IngredientsService.GetIngredientsAsync(userId);

            // 4) Mettre à jour l'UI avec remote
            _ingredients.Clear();
            foreach (var ing in remote)
                _ingredients.Add(new IngredientUi(ing));

            // 5) Sauver remote dans SQLite
            await App.LocalDb.SaveIngredientsAsync(remote);

            RefreshSelectAllCheckBox();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
        }
        finally
        {
            _isLoading = false;
        }
    }


    private async void OnAddIngredientClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(AjouterIngredientPage));
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

    // Suppression d'un seul ingrédient via l'icône 🗑
    private async void OnDeleteIngredientClicked(object sender, EventArgs e)
    {
        if (_isLoading) return;

        if (sender is not Button btn || btn.BindingContext is not IngredientUi ingredientUi)
            return;

        var ok = await DisplayAlert(
            LocalizationResourceManager.Instance["Ingredients_DialogTitle"],
            $"{LocalizationResourceManager.Instance["Dialog_DeleteConfirm"]}\n{ingredientUi.Nom}",
            LocalizationResourceManager.Instance["Dialog_Yes"],
            LocalizationResourceManager.Instance["Dialog_No"]
        );

        if (!ok) return;

        try
        {
            _isLoading = true;

            if (Application.Current is not App app || !app.IsConnected || app.IngredientsService == null || app.CurrentUserId == null)
                return;

            _ingredients.Remove(ingredientUi);

            if (ingredientUi.Id != Guid.Empty)
                await app.IngredientsService.DeleteIngredientAsync(ingredientUi.Id);

            RefreshSelectAllCheckBox();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );

            await LoadIngredientsAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Checkbox "Tout sélectionner" (sélection suppression)
    private void ChkToutSelectionnerIngredients_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_suppressSelectAllEvent) return;

        foreach (var ing in _ingredients)
            ing.IsSelectedForDelete = e.Value;
    }

    private void RefreshSelectAllCheckBox()
    {
        if (ChkToutSelectionnerIngredients == null) return;

        _suppressSelectAllEvent = true;
        try
        {
            if (_ingredients.Count == 0)
            {
                ChkToutSelectionnerIngredients.IsEnabled = false;
                ChkToutSelectionnerIngredients.IsChecked = false;
                return;
            }

            ChkToutSelectionnerIngredients.IsEnabled = true;
            ChkToutSelectionnerIngredients.IsChecked = _ingredients.All(i => i.IsSelectedForDelete);
        }
        finally
        {
            _suppressSelectAllEvent = false;
        }
    }

    // Supprimer sélection
    private async void OnDeleteSelectedIngredientsClicked(object sender, EventArgs e)
    {
        if (_isLoading) return;

        var selected = _ingredients.Where(i => i.IsSelectedForDelete).ToList();

        if (selected.Count == 0)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["Ingredients_DialogTitle"],
                "Aucun ingrédient sélectionné.",
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
            return;
        }

        var ok = await DisplayAlert(
            LocalizationResourceManager.Instance["Ingredients_DialogTitle"],
            $"Supprimer {selected.Count} ingrédient(s) sélectionné(s) ?",
            LocalizationResourceManager.Instance["Dialog_Yes"],
            LocalizationResourceManager.Instance["Dialog_No"]
        );

        if (!ok) return;

        try
        {
            _isLoading = true;

            if (Application.Current is not App app || !app.IsConnected || app.IngredientsService == null || app.CurrentUserId == null)
                return;

            foreach (var ingUi in selected)
            {
                if (ingUi.Id != Guid.Empty)
                    await app.IngredientsService.DeleteIngredientAsync(ingUi.Id);

                _ingredients.Remove(ingUi);
            }

            RefreshSelectAllCheckBox();

            await DisplayAlert(
                LocalizationResourceManager.Instance["Ingredients_DialogTitle"],
                $"{selected.Count} ingrédient(s) supprimé(s).",
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );

            await LoadIngredientsAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Mise à jour "Disponible" -> synchro Supabase (si tu as une méthode UpdateIngredientAsync)
    private async void OnDisponibleChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isLoading) return;

        if (sender is not CheckBox cb || cb.BindingContext is not IngredientUi ingUi)
            return;

        try
        {
            if (Application.Current is not App app || !app.IsConnected || app.IngredientsService == null)
                return;

            // Ici il faut une méthode d'update dans IngredientsService.
            // Si tu l'as déjà : await app.IngredientsService.UpdateIngredientAsync(ingUi.Model.Id, ingUi.Model);
            // Sinon : on ne fait rien (l'UI reste locale). Dis-moi et je te donne l'implémentation côté service.
        }
        catch
        {
            // Optionnel : rollback si update échoue
        }
    }
}
