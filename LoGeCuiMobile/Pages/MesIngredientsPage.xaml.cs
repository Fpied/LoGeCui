using System.Collections.ObjectModel;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Pages;

public partial class MesIngredientsPage : ContentPage
{
    private ObservableCollection<Ingredient> _ingredients =
        new ObservableCollection<Ingredient>();

    public MesIngredientsPage()
    {
        InitializeComponent();

        // Données de test (comme ta capture)
        _ingredients.Add(new Ingredient { Nom = "Chocolat", Quantite = "1", Unite = "pièces", EstDisponible = true });
        _ingredients.Add(new Ingredient { Nom = "Nutella", Quantite = "1", Unite = "boîtes", EstDisponible = true });
        _ingredients.Add(new Ingredient { Nom = "Pain", Quantite = "1", Unite = "kg", EstDisponible = true });
        _ingredients.Add(new Ingredient { Nom = "Poivre", Quantite = "1", Unite = "kg", EstDisponible = true });
        _ingredients.Add(new Ingredient { Nom = "Poulet", Quantite = "1", Unite = "pièces", EstDisponible = true });
        _ingredients.Add(new Ingredient { Nom = "Sel", Quantite = "10", Unite = "kg", EstDisponible = true });

        IngredientsCollection.ItemsSource = _ingredients;
    }

    private async void OnAddIngredientClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AjouterIngredientPage));
    }

    private void OnDeleteIngredientClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Ingredient ingredient)
        {
            _ingredients.Remove(ingredient);
        }
    }

    private void OnIngredientSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Ingredient ingredient)
        {
            // Plus tard : voir les recettes liées
            DisplayAlert("Ingrédient", ingredient.Nom, "OK");
            IngredientsCollection.SelectedItem = null;
        }
    }
}
