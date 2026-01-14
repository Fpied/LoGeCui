using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LoGeCuiShared.Models;
using LoGeCui.Services;

namespace LoGeCui.Views
{
    public partial class RecettesView : UserControl
    {
        private ObservableCollection<Recette> _recettesAffichees;
        private List<Recette> _toutesLesRecettes;
        private readonly RecetteService _recetteService;

        public RecettesView()
        {
            InitializeComponent();

            _recetteService = new RecetteService();
            _toutesLesRecettes = _recetteService.ChargerRecettes();

            // Si vide, ajouter des exemples
            if (_toutesLesRecettes.Count == 0)
            {
                AjouterExemples();
            }

            _recettesAffichees = new ObservableCollection<Recette>(_toutesLesRecettes);
            ListeRecettes.ItemsSource = _recettesAffichees;
        }

        private void AjouterExemples()
        {
            _toutesLesRecettes.Add(new Recette
            {
                Nom = "Salade César",
                Type = TypePlat.Entree,
                TempsPreparation = 15,
                Difficulte = 1,
                Ingredients = new List<IngredientRecette>
                {
                    new IngredientRecette { Nom = "Laitue", Quantite = "1", Unite = "pièce" },
                    new IngredientRecette { Nom = "Poulet", Quantite = "200", Unite = "g" },
                    new IngredientRecette { Nom = "Parmesan", Quantite = "50", Unite = "g" }
                },
                Instructions = "Laver la laitue. Couper le poulet. Mélanger avec le parmesan et la sauce."
            });

            _toutesLesRecettes.Add(new Recette
            {
                Nom = "Spaghetti Carbonara",
                Type = TypePlat.Plat,
                TempsPreparation = 30,
                Difficulte = 2,
                Ingredients = new List<IngredientRecette>
                {
                    new IngredientRecette { Nom = "Spaghetti", Quantite = "400", Unite = "g" },
                    new IngredientRecette { Nom = "Lardons", Quantite = "200", Unite = "g" },
                    new IngredientRecette { Nom = "Œufs", Quantite = "4", Unite = "pièces" }
                },
                Instructions = "Cuire les pâtes. Faire revenir les lardons. Mélanger avec les œufs battus."
            });

            _toutesLesRecettes.Add(new Recette
            {
                Nom = "Mousse au chocolat",
                Type = TypePlat.Dessert,
                TempsPreparation = 20,
                Difficulte = 3,
                Ingredients = new List<IngredientRecette>
                {
                    new IngredientRecette { Nom = "Chocolat noir", Quantite = "200", Unite = "g" },
                    new IngredientRecette { Nom = "Œufs", Quantite = "6", Unite = "pièces" },
                    new IngredientRecette { Nom = "Sucre", Quantite = "50", Unite = "g" }
                },
                Instructions = "Faire fondre le chocolat. Séparer blancs et jaunes. Monter les blancs en neige. Mélanger délicatement."
            });

            _recetteService.SauvegarderRecettes(_toutesLesRecettes);
        }

        private void BtnTous_Click(object sender, RoutedEventArgs e)
        {
            _recettesAffichees.Clear();
            foreach (var recette in _toutesLesRecettes)
                _recettesAffichees.Add(recette);
        }

        private void BtnEntrees_Click(object sender, RoutedEventArgs e)
        {
            FiltrerParType(TypePlat.Entree);
        }

        private void BtnPlats_Click(object sender, RoutedEventArgs e)
        {
            FiltrerParType(TypePlat.Plat);
        }

        private void BtnDesserts_Click(object sender, RoutedEventArgs e)
        {
            FiltrerParType(TypePlat.Dessert);
        }

        private void FiltrerParType(TypePlat type)
        {
            _recettesAffichees.Clear();
            var filtrees = _toutesLesRecettes.Where(r => r.Type == type);
            foreach (var recette in filtrees)
                _recettesAffichees.Add(recette);
        }

        private void ListeRecettes_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var recette = ListeRecettes.SelectedItem as Recette;
            if (recette != null)
            {
                AfficherDetailsRecette(recette);
            }
        }

        private void AfficherDetailsRecette(Recette recette)
        {
            var ingredients = string.Join("\n", recette.Ingredients.Select(i => $"  • {i}"));

            MessageBox.Show(
                $"📖 {recette.Nom}\n\n" +
                $"Type: {recette.TypeTexte}\n" +
                $"Temps: {recette.TempsPreparation} minutes\n" +
                $"Difficulté: {recette.DifficulteTexte}\n\n" +
                $"Ingrédients:\n{ingredients}\n\n" +
                $"Instructions:\n{recette.Instructions}",
                "Détails de la recette",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnSupprimerRecette_Click(object sender, RoutedEventArgs e)
        {
            var bouton = sender as Button;
            var recette = bouton?.Tag as Recette;

            if (recette == null)
                return;

            var resultat = MessageBox.Show(
                $"Voulez-vous vraiment supprimer la recette '{recette.Nom}' ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultat == MessageBoxResult.Yes)
            {
                _toutesLesRecettes.Remove(recette);
                _recettesAffichees.Remove(recette);
                _recetteService.SauvegarderRecettes(_toutesLesRecettes);

                MessageBox.Show(
                    $"La recette '{recette.Nom}' a été supprimée.",
                    "Suppression réussie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnNouvelleRecette_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterRecetteDialog();
            dialog.Owner = Window.GetWindow(this);

            bool? resultat = dialog.ShowDialog();

            if (resultat == true && dialog.NouvelleRecette != null)
            {
                _toutesLesRecettes.Add(dialog.NouvelleRecette);
                _recettesAffichees.Add(dialog.NouvelleRecette);
                _recetteService.SauvegarderRecettes(_toutesLesRecettes);

                MessageBox.Show($"La recette '{dialog.NouvelleRecette.Nom}' a été ajoutée avec succès !",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}