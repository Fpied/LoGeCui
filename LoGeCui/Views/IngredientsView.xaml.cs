using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LoGeCuiShared.Models;

namespace LoGeCui.Views
{
    public partial class IngredientsView : UserControl
    {
        private readonly ObservableCollection<Ingredient> _ingredients = new();

        public IngredientsView()
        {
            InitializeComponent();

            ListeIngredients.ItemsSource = _ingredients;

            Loaded += IngredientsView_Loaded;
        }

        private async void IngredientsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Evite de recharger plusieurs fois si Loaded se déclenche à nouveau
            Loaded -= IngredientsView_Loaded;
            await ReloadIngredientsAsync();
        }

        private async Task ReloadIngredientsAsync()
        {
            try
            {
                var data = await App.SupabaseService.GetIngredientsAsync();

                _ingredients.Clear();
                foreach (var ing in data.OrderBy(i => i.Nom))
                    _ingredients.Add(ing);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement ingrédients (Supabase) : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterIngredientDialog
            {
                Owner = Window.GetWindow(this)
            };

            bool? resultat = dialog.ShowDialog();
            if (resultat != true || dialog.NouvelIngredient == null)
                return;

            try
            {
                var saved = await App.SupabaseService.AddIngredientAsync(dialog.NouvelIngredient);

                if (saved == null)
                {
                    MessageBox.Show("L'ajout a échoué (aucune donnée retournée).",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _ingredients.Add(saved);

                MessageBox.Show(
                    $"L'ingrédient '{saved.Nom}' a été ajouté avec succès !",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout (Supabase) : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button bouton)
                return;

            // On récupère l'objet lié à la ligne (plus fiable que Tag si ton XAML est bindé correctement)
            if (bouton.DataContext is not Ingredient ingredient)
                return;

            var confirmation = MessageBox.Show(
                $"Voulez-vous vraiment supprimer '{ingredient.Nom}' ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                if (ingredient.Id == Guid.Empty)
                {
                    MessageBox.Show("Impossible de supprimer : l'ingrédient n'a pas d'Id.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool ok = await App.SupabaseService.DeleteIngredientAsync(ingredient.Id);
                if (!ok)
                {
                    MessageBox.Show("La suppression a échoué côté serveur.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _ingredients.Remove(ingredient);

                MessageBox.Show(
                    $"L'ingrédient '{ingredient.Nom}' a été supprimé.",
                    "Suppression réussie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression (Supabase) : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChkDisponible_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb)
                return;

            // Objet lié à la ligne
            if (cb.DataContext is not Ingredient ingredient)
                return;

            try
            {
                if (ingredient.Id == Guid.Empty)
                    return; // pas persisté côté Supabase

                bool ok = await App.SupabaseService.UpdateIngredientAsync(ingredient);
                if (!ok)
                {
                    MessageBox.Show("La mise à jour a échoué côté serveur.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur mise à jour (Supabase) : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
