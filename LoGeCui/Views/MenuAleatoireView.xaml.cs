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
        private MenuJournalier? _menuCourant;
        private static readonly Random _random = new Random();

        public MenuAleatoireView()
        {
            InitializeComponent();
        }

        private async void BtnGenererMenu_Click(object sender, RoutedEventArgs e)
        {
            // Charger les recettes depuis Supabase
            if (App.RecipesService == null || App.CurrentUserId == null)
            {
                MessageBox.Show("Connecte-toi d'abord (services non initialisés).");
                return;
            }

            var recettes = await App.RecipesService.GetRecettesAsync(App.CurrentUserId.Value)
                          ?? new List<Recette>();

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

            // Charger les ingrédients depuis Supabase
            if (App.RestClient == null)
            {
                MessageBox.Show("RestClient non initialisé.");
                return;
            }

            try
            {
                var ingSvc = new LoGeCuiShared.Services.RecetteIngredientsService(App.RestClient);

                if (_menuCourant.Entree != null)
                {
                    var tuples = await ingSvc.GetForRecetteAsync(_menuCourant.Entree.Id);
                    _menuCourant.Entree.Ingredients = tuples
                        .Select(t => new IngredientRecette
                        {
                            Nom = t.nom,
                            Quantite = t.quantite,
                            Unite = t.unite
                        })
                        .ToList();
                }

                if (_menuCourant.Plat != null)
                {
                    var tuples = await ingSvc.GetForRecetteAsync(_menuCourant.Plat.Id);
                    _menuCourant.Plat.Ingredients = tuples
                        .Select(t => new IngredientRecette
                        {
                            Nom = t.nom,
                            Quantite = t.quantite,
                            Unite = t.unite
                        })
                        .ToList();
                }

                if (_menuCourant.Dessert != null)
                {
                    var tuples = await ingSvc.GetForRecetteAsync(_menuCourant.Dessert.Id);
                    _menuCourant.Dessert.Ingredients = tuples
                        .Select(t => new IngredientRecette
                        {
                            Nom = t.nom,
                            Quantite = t.quantite,
                            Unite = t.unite
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur chargement ingrédients des recettes : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Afficher menu
            AfficherMenu();

            IngredientsManquantsPanel.Visibility = Visibility.Collapsed;
            ToutDisponiblePanel.Visibility = Visibility.Collapsed;
            ListeIngredientsManquants.ItemsSource = null;

            await VerifierIngredientsAsync();
        }

        private Recette? ChoisirRecetteAleatoire(List<Recette> recettes)
        {
            if (recettes == null || recettes.Count == 0)
                return null;

            return recettes[_random.Next(recettes.Count)];
        }

        private void AfficherMenu()
        {
            if (_menuCourant == null)
                return;

            MenuPanel.Visibility = Visibility.Visible;
            TxtDate.Text = $"Menu généré le {_menuCourant.Date:dd/MM/yyyy à HH:mm}";

            AfficherBlocRecette(_menuCourant.Entree, TxtEntree, TxtEntreeDetails, "entrée");
            AfficherBlocRecette(_menuCourant.Plat, TxtPlat, TxtPlatDetails, "plat");
            AfficherBlocRecette(_menuCourant.Dessert, TxtDessert, TxtDessertDetails, "dessert");
        }

        private void AfficherBlocRecette(Recette? recette, TextBlock titre, TextBlock details, string type)
        {
            if (recette != null)
            {
                titre.Text = recette.Nom;
                details.Text = $"{recette.DifficulteTexte} • {recette.TempsPreparation} min";
            }
            else
            {
                titre.Text = $"Aucun {type} disponible";
                details.Text = $"Ajoutez des recettes de type '{type}'";
            }
        }

        private async System.Threading.Tasks.Task VerifierIngredientsAsync()
        {
            if (_menuCourant == null)
                return;

            try
            {
                var ingredientsDisponibles = await App.SupabaseService.GetIngredientsAsync();

                var disponibles = (ingredientsDisponibles ?? new List<Ingredient>())
                    .Where(i => i.EstDisponible)
                    .Select(i => (i.Nom ?? "").Trim().ToLowerInvariant())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToHashSet();

                var necessaires = new List<IngredientRecette>();

                if (_menuCourant.Entree != null)
                    necessaires.AddRange(_menuCourant.Entree.Ingredients ?? new List<IngredientRecette>());
                if (_menuCourant.Plat != null)
                    necessaires.AddRange(_menuCourant.Plat.Ingredients ?? new List<IngredientRecette>());
                if (_menuCourant.Dessert != null)
                    necessaires.AddRange(_menuCourant.Dessert.Ingredients ?? new List<IngredientRecette>());

                if (necessaires.Count == 0)
                {
                    IngredientsManquantsPanel.Visibility = Visibility.Collapsed;
                    ToutDisponiblePanel.Visibility = Visibility.Collapsed;
                    ListeIngredientsManquants.ItemsSource = null;
                    return;
                }

                _menuCourant.IngredientsManquants.Clear();

                foreach (var ingredient in necessaires)
                {
                    var nom = (ingredient.Nom ?? "").Trim();
                    if (!disponibles.Contains(nom.ToLowerInvariant()))
                    {
                        if (!_menuCourant.IngredientsManquants.Any(i =>
                                (i.Nom ?? "").Equals(nom, StringComparison.OrdinalIgnoreCase)))
                        {
                            _menuCourant.IngredientsManquants.Add(ingredient);
                        }
                    }
                }

                if (_menuCourant.IngredientsManquants.Any())
                {
                    IngredientsManquantsPanel.Visibility = Visibility.Visible;
                    ToutDisponiblePanel.Visibility = Visibility.Collapsed;

                    ListeIngredientsManquants.ItemsSource = _menuCourant.IngredientsManquants
                        .Select(i => $"• {i.Quantite} {i.Unite} {i.Nom}".Replace("  ", " ").Trim())
                        .ToList();
                }
                else
                {
                    IngredientsManquantsPanel.Visibility = Visibility.Collapsed;
                    ToutDisponiblePanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur vérification ingrédients : {ex.Message}");
            }
        }

        private async void BtnAjouterListeCourses_Click(object sender, RoutedEventArgs e)
        {
            if (_menuCourant == null || !_menuCourant.IngredientsManquants.Any())
                return;

            var supabase = App.SupabaseService;
            var articlesExistants = await supabase.GetArticlesAsync();
            var nomsExistants = (articlesExistants ?? new List<ArticleCourse>())
                .Select(a => (a.Nom ?? "").Trim().ToLowerInvariant())
                .ToHashSet();

            foreach (var ingredient in _menuCourant.IngredientsManquants)
            {
                var nom = (ingredient.Nom ?? "").Trim();
                if (!nomsExistants.Contains(nom.ToLowerInvariant()))
                {
                    await supabase.AddArticleAsync(new ArticleCourse
                    {
                        Nom = nom,
                        Quantite = ingredient.Quantite,
                        Unite = ingredient.Unite,
                        EstAchete = false
                    });
                }
            }

            MessageBox.Show("Ingrédients ajoutés à la liste de courses !");
        }
    }
}
