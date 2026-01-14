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
    /// <summary>
    /// Logique d'interaction pour AjouterIngredientRecetteDialog.xaml
    /// </summary>
    public partial class AjouterIngredientRecetteDialog : Window
    {
        public IngredientRecette? NouvelIngredient { get; private set; }  // ← LIGNE AJOUTÉE !

        public AjouterIngredientRecetteDialog()
        {
            InitializeComponent();
            TxtNom.Focus();
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                MessageBox.Show("Veuillez entrer un nom.", "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtQuantite.Text))
            {
                MessageBox.Show("Veuillez entrer une quantité.", "Champ requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NouvelIngredient = new IngredientRecette  // ← CHANGÉ aussi ici !
            {
                Nom = TxtNom.Text.Trim(),
                Quantite = TxtQuantite.Text.Trim(),
                Unite = (CmbUnite.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "pièces"
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
