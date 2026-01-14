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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using LoGeCuiShared.Models;

namespace LoGeCui.Dialogs
{
    /// <summary>
    /// Logique d'interaction pour AjouterRecetteDialog.xaml
    /// </summary>
    public partial class AjouterRecetteDialog : Window
    {

        public Recette? NouvelleRecette { get; private set; }
        private ObservableCollection<IngredientRecette> _ingredients;

        public AjouterRecetteDialog()
        {
            InitializeComponent();

            _ingredients = new ObservableCollection<IngredientRecette>();
            ListeIngredients.ItemsSource = _ingredients;

            TxtNom.Focus();
        }

        private void SliderDifficulte_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtDifficulte != null)
            {
                int valeur = (int)SliderDifficulte.Value;
                TxtDifficulte.Text = new string('⭐', valeur);
            }
        }

        private void BtnAjouterIngredient_Click(object sender, RoutedEventArgs e)
        {
            // Créer une petite fenêtre pour ajouter un ingrédient
            var dialogIngredient = new AjouterIngredientRecetteDialog();
            dialogIngredient.Owner = this;

            bool? resultat = dialogIngredient.ShowDialog();

            if (resultat == true && dialogIngredient.NouvelIngredient != null)
            {
                _ingredients.Add(dialogIngredient.NouvelIngredient);
            }
        }

        private void BtnSupprimerIngredient_Click(object sender, RoutedEventArgs e)
        {
            var bouton = sender as System.Windows.Controls.Button;
            var ingredient = bouton?.Tag as IngredientRecette;

            if (ingredient != null)
            {
                _ingredients.Remove(ingredient);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                MessageBox.Show("Veuillez entrer un nom de recette.",
                    "Champ requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtNom.Focus();
                return;
            }

            if (!int.TryParse(TxtTemps.Text, out int temps) || temps <= 0)
            {
                MessageBox.Show("Veuillez entrer un temps de préparation valide.",
                    "Champ requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtTemps.Focus();
                return;
            }

            if (_ingredients.Count == 0)
            {
                MessageBox.Show("Veuillez ajouter au moins un ingrédient.",
                    "Ingrédients requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtInstructions.Text))
            {
                MessageBox.Show("Veuillez entrer les instructions.",
                    "Champ requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtInstructions.Focus();
                return;
            }

            // Récupérer le type
            var selectedItem = CmbType.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var typeTag = selectedItem?.Tag?.ToString() ?? "Plat";

            TypePlat type = typeTag switch
            {
                "Entree" => TypePlat.Entree,
                "Dessert" => TypePlat.Dessert,
                _ => TypePlat.Plat
            };

            // Créer la recette
            NouvelleRecette = new Recette
            {
                Nom = TxtNom.Text.Trim(),
                Type = type,
                TempsPreparation = temps,
                Difficulte = (int)SliderDifficulte.Value,
                Ingredients = new System.Collections.Generic.List<IngredientRecette>(_ingredients),
                Instructions = TxtInstructions.Text.Trim()
            };

            DialogResult = true;
            Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
