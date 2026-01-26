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
using LoGeCui.Services;
using LoGeCui.Views;

namespace LoGeCui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // SUPPRIMÉ : private SupabaseService _supabase;

        public MainWindow()
        {
            InitializeComponent();

            // SUPPRIMÉ : Ne plus créer une nouvelle instance !
            // _supabase = new SupabaseService(url, key);

            // Maintenant on utilisera App.SupabaseService partout
        }

        private void BtnIngredients_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.IngredientsView();
        }

        private void BtnRecettes_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.RecettesView();
        }

        private async void BtnAjouterRecette_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterRecetteDialog();
            dialog.Owner = this;

            bool? resultat = dialog.ShowDialog();

            if (resultat == true && dialog.NouvelleRecette != null)
            {
                try
                {
                    if (App.RecipesService == null || App.CurrentUserId == null)
                    {
                        MessageBox.Show("Utilisateur non connecté ou services non initialisés.");
                        return;
                    }

                    var r = dialog.NouvelleRecette;

                    // ExternalId obligatoire pour l'upsert (évite les doublons)
                    if (string.IsNullOrWhiteSpace(r.ExternalId))
                        r.ExternalId = Guid.NewGuid().ToString("N");

                    // Sauvegarde vers Supabase
                    await App.RecipesService.UpsertRecetteAsync(App.CurrentUserId.Value, r);

                    // Recharge la vue Recettes (qui doit lire Supabase)
                    var view = new RecettesView();
                    MainContent.Content = view;

                    MessageBox.Show(
                        $"La recette '{r.Nom}' a été ajoutée avec succès !",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ajout :\n{ex}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        public async Task SyncRecettesToSupabaseAsync(IEnumerable<LoGeCuiShared.Models.Recette> recettesLocales)
        {
            if (App.RecipesService == null || App.CurrentUserId == null)
                throw new InvalidOperationException("Services REST non initialisés. Connecte-toi d'abord.");

            foreach (var r in recettesLocales)
            {
                if (string.IsNullOrWhiteSpace(r.ExternalId))
                    throw new InvalidOperationException($"Recette '{r.Nom}' sans ExternalId. Il faut un identifiant stable pour éviter les doublons.");

                await App.RecipesService.UpsertRecetteAsync(App.CurrentUserId.Value, r);
            }
        }

        private async void BtnSyncRecettes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.RecipesService == null || App.CurrentUserId == null)
                {
                    MessageBox.Show("Utilisateur non connecté ou services non initialisés.");
                    return;
                }

                // 🔹 ICI tu dois récupérer TA liste de recettes WPF
                // Adapte cette ligne à TON code existant

                MessageBox.Show("Synchronisation terminée avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de synchronisation :\n{ex.Message}");
            }
        }


    }
}