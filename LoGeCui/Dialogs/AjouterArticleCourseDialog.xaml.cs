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
    public partial class AjouterArticleCourseDialog : Window
    {
        public ArticleCourse? NouvelArticle { get; private set; }

        public AjouterArticleCourseDialog()
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

            NouvelArticle = new ArticleCourse
            {
                Nom = TxtNom.Text.Trim(),
                Quantite = TxtQuantite.Text.Trim(),
                Unite = (CmbUnite.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "pièces",
                EstAchete = false
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
