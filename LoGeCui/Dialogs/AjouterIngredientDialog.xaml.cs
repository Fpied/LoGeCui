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
using LoGeCuiShared.Models;

namespace LoGeCui.Dialogs
{
    public partial class AjouterIngredientDialog : Window
    {
        // Propriété pour récupérer l'ingrédient créé
        public Ingredient? NouvelIngredient { get; private set; }

        public AjouterIngredientDialog()
        {
            InitializeComponent();

            // Focus sur le premier champ
            TxtNom.Focus();
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            // Validation : vérifier que tous les champs sont remplis
            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                MessageBox.Show("Veuillez entrer un nom d'ingrédient.",
                    "Champ requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtNom.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtQuantite.Text))
            {
                MessageBox.Show("Veuillez entrer une quantité.",
                    "Champ requis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TxtQuantite.Focus();
                return;
            }

            // Créer l'ingrédient
            NouvelIngredient = new Ingredient
            {
                Nom = TxtNom.Text.Trim(),
                Quantite = TxtQuantite.Text.Trim(),
                Unite = (CmbUnite.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "pièces",
                EstDisponible = true
            };

            // Fermer la fenêtre avec succès
            DialogResult = true;
            Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            // Fermer sans rien faire
            DialogResult = false;
            Close();
        }
    }
}
