using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LoGeCuiShared.Models;
using LoGeCui.Services;

namespace LoGeCui.Views
{
    public partial class MenuAleatoireView : UserControl
    {
        private readonly RecetteService _recetteService;
        private MenuJournalier? _menuCourant;

        public MenuAleatoireView()
        {
            InitializeComponent();
            _recetteService = new RecetteService();
        }

        private void BtnGenererMenu_Click(object sender, RoutedEventArgs e)
        {
            // Charger les recettes (source locale actuelle)
            var recettes = _recetteService.ChargerRecettes();

            if (recettes.Count == 0)
            {
                MessageBox.Show(
                    "Vous n'avez aucune recette !\n\nAjoutez des recettes d'abord.",
                    "Aucune recette",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Filtrer par type
            var entrees = recettes.Where(r => r.Type == TypePlat.Entree).ToList();
            var plats = recettes.Where(r => r.Type == TypePlat.Plat).ToList();
            var desserts = recettes.Where(r => r.Type == TypePlat.Dessert).ToList();

            // Créer le menu
            _menuCourant = new MenuJournalier
            {
                Date = DateTime.Now,
                Entree = ChoisirRecetteAleatoire(entrees),
                Plat = ChoisirRecetteAleatoire(plats),
                Dessert = ChoisirRecetteAleatoire(desserts)
            };

            // Afficher le menu
            AfficherMenu();

            // Vérifier les ingrédients (async, Supabase)
            _ = VerifierIngredientsAsync();
        }

        private Recette? ChoisirRecetteAleatoire(List<Recette> recettes)
        {
            if (recettes.Count == 0)
                return null;

            var random = new Random();
            return recettes[random.Next(recettes.Count)];
        }

        private void AfficherMenu()
        {
            if (_menuCourant == null)
                return;

            MenuPanel.Visibility = Visibility.Visible;

            TxtDate.Text = $"Menu généré le {_menuCourant.Date:dd/MM/yyyy à HH:mm}";

            if (_menuCourant.Entree != null)
            {
                TxtEntree.Text = _menuCourant.Entree.Nom;
                TxtEntreeDetails.Text = $"{_menuCourant.Entree.DifficulteTexte} • {_menuCourant.Entree.TempsPreparation} min";
            }
            else
            {
                TxtEntree.Text = "Aucune entrée disponible";
                TxtEntreeDetails.Text = "Ajoutez des recettes de type 'Entrée'";
            }

            if (_menuCourant.Plat != null)
            {
                TxtPlat.Text = _menuCourant.Plat.Nom;
                TxtPlatDetails.Text = $"{_menuCourant.Plat.DifficulteTexte} • {_menuCourant.Plat.TempsPreparation} min";
            }
            else
            {
                TxtPlat.Text = "Aucun plat disponible";
                TxtPlatDetails.Text = "Ajoutez des recettes de type 'Plat'";
            }

            if (_menuCourant.Dessert != null)
            {
                TxtDessert.Text = _menuCourant.Dessert.Nom;
                TxtDessertDetails.Text = $"{_menuCourant.Dessert.DifficulteTexte} • {_menuCourant.Dessert.TempsPreparation} min";
            }
            else
            {
                TxtDessert.Text = "Aucun dessert disponible";
                TxtDessertDetails.Text = "Ajoutez des recettes de type 'Dessert'";
            }
        }

        /// <summary>
        /// Vérifie les ingrédients disponibles dans Supabase et calcule les ingrédients manquants.
        /// </summary>
        private async System.Threading.Tasks.Task VerifierIngredientsAsync()
        {
            if (_menuCourant == null)
                return;

            try
            {
                // Ingrédients disponibles (Supabase)
                var ingredientsDisponibles = await App.SupabaseService.GetIngredientsAsync();

                var disponibles = ingredientsDisponibles
                    .Where(i => i.EstDisponible)
                    .Select(i => (i.Nom ?? "").Trim().ToLower())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToHashSet();

                // Collecter tous les ingrédients nécessaires du menu
                var necessaires = new List<IngredientRecette>();

                if (_menuCourant.Entree != null)
                    necessaires.AddRange(_menuCourant.Entree.Ingredients);

                if (_menuCourant.Plat != null)
                    necessaires.AddRange(_menuCourant.Plat.Ingredients);

                if (_menuCourant.Dessert != null)
                    necessaires.AddRange(_menuCourant.Dessert.Ingredients);

                _menuCourant.IngredientsManquants.Clear();

                foreach (var ingredient in necessaires)
                {
                    var nom = (ingredient.Nom ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(nom))
                        continue;

                    if (!disponibles.Contains(nom.ToLower()))
                    {
                        // éviter doublons
                        if (!_menuCourant.IngredientsManquants.Any(i =>
                                i.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)))
                        {
                            _menuCourant.IngredientsManquants.Add(ingredient);
                        }
                    }
                }

                // Afficher
                if (_menuCourant.IngredientsManquants.Any())
                {
                    IngredientsManquantsPanel.Visibility = Visibility.Visible;
                    ToutDisponiblePanel.Visibility = Visibility.Collapsed;

                    ListeIngredientsManquants.ItemsSource = _menuCourant.IngredientsManquants
                        .Select(i => $"• {i.Nom} - {i.Quantite} {i.Unite}")
                        .ToList();
                }
                else
                {
                    IngredientsManquantsPanel.Visibility = Visibility.Collapsed;
                    ToutDisponiblePanel.Visibility = Visibility.Visible;

                    ListeIngredientsManquants.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur vérification ingrédients (Supabase) : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void BtnAjouterListeCourses_Click(object sender, RoutedEventArgs e)
        {
            if (_menuCourant == null || !_menuCourant.IngredientsManquants.Any())
            {
                MessageBox.Show("Aucun ingrédient à ajouter.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // IMPORTANT : utiliser l'instance connectée (session OK)
                var supabase = App.SupabaseService;

                var articlesExistants = await supabase.GetArticlesAsync();
                var nomsExistants = articlesExistants
                    .Select(a => (a.Nom ?? "").Trim().ToLower())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToHashSet();

                int nbAjoutes = 0;

                foreach (var ingredient in _menuCourant.IngredientsManquants)
                {
                    var nom = (ingredient.Nom ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(nom))
                        continue;

                    if (!nomsExistants.Contains(nom.ToLower()))
                    {
                        var article = new ArticleCourse
                        {
                            Nom = nom,
                            Quantite = ingredient.Quantite,
                            Unite = ingredient.Unite,
                            EstAchete = false
                        };

                        await supabase.AddArticleAsync(article);
                        nbAjoutes++;

                        // Mettre à jour le set pour éviter doublons dans la même exécution
                        nomsExistants.Add(nom.ToLower());
                    }
                }

                if (nbAjoutes > 0)
                {
                    MessageBox.Show(
                        $"{nbAjoutes} ingrédient(s) ajouté(s) à la liste de courses !\n\n" +
                        "Consultez 'Liste de Courses' pour les voir.",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Tous ces ingrédients sont déjà dans votre liste de courses.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'ajout : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

