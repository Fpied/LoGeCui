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
        private readonly IngredientService _ingredientService;
        private MenuJournalier? _menuCourant;

        public MenuAleatoireView()
        {
            InitializeComponent();
            _recetteService = new RecetteService();
            _ingredientService = new IngredientService();
        }

        private void BtnGenererMenu_Click(object sender, RoutedEventArgs e)
        {
            // Charger les recettes
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

            // Vérifier les ingrédients
            VerifierIngredients();
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

            // Afficher le panel
            MenuPanel.Visibility = Visibility.Visible;

            // Date
            TxtDate.Text = $"Menu généré le {_menuCourant.Date:dd/MM/yyyy à HH:mm}";

            // Entrée
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

            // Plat
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

            // Dessert
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

        private void VerifierIngredients()
        {
            if (_menuCourant == null)
                return;

            // Charger les ingrédients disponibles
            _ingredientService.ChargerIngredients();
            var disponibles = _ingredientService.ObtenirIngredientsDisponibles()
                .Select(i => i.Nom.ToLower())
                .ToHashSet();

            // Collecter tous les ingrédients nécessaires
            var necessaires = new List<IngredientRecette>();

            if (_menuCourant.Entree != null)
                necessaires.AddRange(_menuCourant.Entree.Ingredients);

            if (_menuCourant.Plat != null)
                necessaires.AddRange(_menuCourant.Plat.Ingredients);

            if (_menuCourant.Dessert != null)
                necessaires.AddRange(_menuCourant.Dessert.Ingredients);

            // Trouver les manquants
            _menuCourant.IngredientsManquants.Clear();
            foreach (var ingredient in necessaires)
            {
                if (!disponibles.Contains(ingredient.Nom.ToLower()))
                {
                    // Éviter les doublons
                    if (!_menuCourant.IngredientsManquants.Any(i =>
                        i.Nom.Equals(ingredient.Nom, StringComparison.OrdinalIgnoreCase)))
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
                    .Select(i => $"• {i.Nom} - {i.Quantite} {i.Unite}");
            }
            else
            {
                IngredientsManquantsPanel.Visibility = Visibility.Collapsed;
                ToutDisponiblePanel.Visibility = Visibility.Visible;
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
                // ⚠️ REMPLACE PAR TES VRAIES CLÉS !
                string url = "https://wzctiypsadqktzcnswri.supabase.co";
                string key = "sb_publishable_ZFk8ONON5qMA0vZ3V0nVAg_TZZrO1F1";

                var supabase = new LoGeCuiShared.Services.SupabaseService(url, key);

                // Récupérer les articles existants
                var articlesExistants = await supabase.GetArticlesAsync();
                var nomsExistants = articlesExistants.Select(a => a.Nom.ToLower()).ToHashSet();

                int nbAjoutes = 0;

                foreach (var ingredient in _menuCourant.IngredientsManquants)
                {
                    // Vérifier si l'article existe déjà
                    if (!nomsExistants.Contains(ingredient.Nom.ToLower()))
                    {
                        var article = new LoGeCuiShared.Models.ArticleCourse
                        {
                            Nom = ingredient.Nom,
                            Quantite = ingredient.Quantite,
                            Unite = ingredient.Unite,
                            EstAchete = false
                        };

                        await supabase.AddArticleAsync(article);
                        nbAjoutes++;
                    }
                }

                // Message de confirmation
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
