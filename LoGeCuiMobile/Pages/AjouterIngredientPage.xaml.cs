using LoGeCuiShared.Models;
using LoGeCuiMobile.Resources.Lang;
using Microsoft.Maui.Controls;

namespace LoGeCuiMobile.Pages;

[QueryProperty(nameof(Result), "result")]
public partial class AjouterIngredientPage : ContentPage
{
    public Ingredient? Result { get; set; }

    public AjouterIngredientPage()
    {
        InitializeComponent();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
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

        Result = new Ingredient
        {
            Nom = NomEntry.Text.Trim(),
            Quantite = QuantiteEntry.Text?.Trim() ?? "",
            Unite = UniteEntry.Text?.Trim() ?? "",
            EstDisponible = DisponibleCheckBox.IsChecked
        };

        // retour vers la page précédente
        await Shell.Current.GoToAsync("..");
    }
}
