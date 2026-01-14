using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using LoGeCuiShared.Models;
using System.Linq;
using LoGeCui.Services;

namespace LoGeCui.Views
{
    public partial class IngredientsView : UserControl
    {
        private ObservableCollection<Ingredient> _ingredients;
        private readonly IngredientService _ingredientService;  // ← NOUVEAU

        public IngredientsView()
        {
            InitializeComponent();

            // Créer le service
            _ingredientService = new IngredientService();  // ← NOUVEAU

            // Charger les ingrédients depuis le fichier
            var ingredientsCharges = _ingredientService.ChargerIngredients();  // ← NOUVEAU

            _ingredients = new ObservableCollection<Ingredient>(ingredientsCharges);  // ← MODIFIÉ

            // Si la liste est vide (première utilisation), ajouter des exemples
            if (_ingredients.Count == 0)  // ← NOUVEAU
            {
                _ingredients.Add(new Ingredient
                {
                    Nom = "Tomates",
                    Quantite = "5",
                    Unite = "pièces",
                    EstDisponible = true
                });

                _ingredients.Add(new Ingredient
                {
                    Nom = "Oignons",
                    Quantite = "3",
                    Unite = "pièces",
                    EstDisponible = true
                });

                _ingredients.Add(new Ingredient
                {
                    Nom = "Farine",
                    Quantite = "1",
                    Unite = "kg",
                    EstDisponible = false
                });

                // Sauvegarder les exemples
                Sauvegarder();  // ← NOUVEAU
            }

            ListeIngredients.ItemsSource = _ingredients;
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterIngredientDialog();
            dialog.Owner = Window.GetWindow(this);

            bool? resultat = dialog.ShowDialog();

            if (resultat == true && dialog.NouvelIngredient != null)
            {
                _ingredients.Add(dialog.NouvelIngredient);

                Sauvegarder();  // ← NOUVEAU : Sauvegarder après ajout

                MessageBox.Show($"L'ingrédient '{dialog.NouvelIngredient.Nom}' a été ajouté avec succès !",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        // ← NOUVELLE MÉTHODE : Sauvegarde les ingrédients
        private void Sauvegarder()
        {
            var liste = _ingredients.ToList();
            _ingredientService.SauvegarderIngredients(liste);
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer le bouton qui a été cliqué
            var bouton = sender as Button;

            // Récupérer l'ingrédient associé au bouton
            var ingredient = bouton?.Tag as Ingredient;

            if (ingredient == null)
                return;

            // Demander confirmation
            var resultat = MessageBox.Show(
                $"Voulez-vous vraiment supprimer '{ingredient.Nom}' ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            // Si l'utilisateur confirme
            if (resultat == MessageBoxResult.Yes)
            {
                // Supprimer de la liste
                _ingredients.Remove(ingredient);

                // Sauvegarder
                Sauvegarder();

                // Message de confirmation
                MessageBox.Show(
                    $"L'ingrédient '{ingredient.Nom}' a été supprimé.",
                    "Suppression réussie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ChkDisponible_Changed(object sender, RoutedEventArgs e)
        {
            // Sauvegarder quand on change la disponibilité
            Sauvegarder();
        }
    }
}
