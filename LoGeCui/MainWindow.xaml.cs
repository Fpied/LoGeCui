using LoGeCuiShared.Services;
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

namespace LoGeCui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SupabaseService _supabase;

        public MainWindow()
        {
            InitializeComponent();

            // Recréer l'instance Supabase
            string url = "https://wzctiypsadqktzcnswri.supabase.co";
            string key = "sb_publishable_ZFk8ONON5qMA0vZ3V0nVAg_TZZrO1F1";
            _supabase = new SupabaseService(url, key);

        }

        private void BtnIngredients_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.IngredientsView();
        }

        private void BtnRecettes_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.RecettesView();
        }

        private void BtnAjouterRecette_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterRecetteDialog();
            dialog.Owner = this;

            bool? resultat = dialog.ShowDialog();

            if (resultat == true && dialog.NouvelleRecette != null)
            {
                MessageBox.Show(
                    $"La recette '{dialog.NouvelleRecette.Nom}' a été ajoutée avec succès !\n\n" +
                    "Vous pouvez la voir dans 'Mes Recettes'.",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnMenuAleatoire_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.MenuAleatoireView();
        }

        private void BtnListeCourses_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.ListeCoursesView();
        }
    }
}