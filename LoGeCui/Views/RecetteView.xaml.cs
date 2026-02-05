using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LoGeCui.Views
{
    public partial class RecettesView : UserControl
    {
        private readonly ObservableCollection<Recette> _recettesAffichees = new();
        private List<Recette> _toutesLesRecettes = new();

        public RecettesView()
        {
            InitializeComponent();

            ListeRecettes.ItemsSource = _recettesAffichees;

            Loaded += RecettesView_Loaded;
        }

        private async void RecettesView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= RecettesView_Loaded;
            await RefreshDepuisSupabaseAsync();
        }

        private bool EnsureConnected(out Guid userId)
        {
            userId = Guid.Empty;

            if (App.RecipesService == null || App.CurrentUserId == null)
            {
                MessageBox.Show("Connecte-toi d'abord (services non initialisés).");
                return false;
            }

            userId = App.CurrentUserId.Value;
            return true;
        }

        private bool EnsureRestClient()
        {
            if (App.RestClient == null)
            {
                MessageBox.Show("RestClient non initialisé.");
                return false;
            }
            return true;
        }

        private async System.Threading.Tasks.Task RefreshDepuisSupabaseAsync()
        {
            if (!EnsureConnected(out var userId))
                return;

            var recettes = await App.RecipesService!.GetRecettesAsync(userId);
            _toutesLesRecettes = recettes ?? new List<Recette>();

            _recettesAffichees.Clear();
            foreach (var r in _toutesLesRecettes)
                _recettesAffichees.Add(r);
        }

        private void BtnTous_Click(object sender, RoutedEventArgs e)
        {
            _recettesAffichees.Clear();
            foreach (var recette in _toutesLesRecettes)
                _recettesAffichees.Add(recette);
        }

        private void BtnEntrees_Click(object sender, RoutedEventArgs e) => FiltrerParType(TypePlat.Entree);
        private void BtnPlats_Click(object sender, RoutedEventArgs e) => FiltrerParType(TypePlat.Plat);
        private void BtnDesserts_Click(object sender, RoutedEventArgs e) => FiltrerParType(TypePlat.Dessert);

        private void FiltrerParType(TypePlat type)
        {
            _recettesAffichees.Clear();
            foreach (var recette in _toutesLesRecettes.Where(r => r.Type == type))
                _recettesAffichees.Add(recette);
        }

        private void ListeRecettes_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var recette = ListeRecettes.SelectedItem as Recette;
            if (recette != null)
                AfficherDetailsRecette(recette);
        }

        private async void AfficherDetailsRecette(Recette recette)
        {
            try
            {
                if (!EnsureRestClient())
                    return;

                var ingSvc = new RecetteIngredientsService(App.RestClient!);
                var items = await ingSvc.GetForRecetteAsync(recette.Id);

                var ingredientsText = (items.Count == 0)
                    ? "(aucun ingrédient renseigné)"
                    : string.Join("\n", items.Select(x =>
                        string.IsNullOrWhiteSpace(x.quantite) && string.IsNullOrWhiteSpace(x.unite)
                            ? $"  • {x.nom}"
                            : $"  • {x.quantite} {x.unite} {x.nom}".Replace("  ", " ").Trim()
                    ));

                MessageBox.Show(
                    $"📖 {recette.Nom}\n\n" +
                    $"Catégorie: {recette.CategorieDb}\n" +
                    $"Temps: {recette.TempsPreparation} minutes\n\n" +
                    $"Ingrédients:\n{ingredientsText}\n\n" +
                    $"Instructions:\n{recette.Instructions}",
                    "Détails de la recette",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement ingrédients:\n{ex}");
            }
        }

        private async void BtnNouvelleRecette_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterRecetteDialog();
            dialog.Owner = Window.GetWindow(this);

            bool? resultat = dialog.ShowDialog();
            if (resultat != true || dialog.NouvelleRecette == null)
                return;

            if (!EnsureConnected(out var userId))
                return;

            if (!EnsureRestClient())
                return;

            var r = dialog.NouvelleRecette;

            // ExternalId obligatoire pour upsert
            if (string.IsNullOrWhiteSpace(r.ExternalId))
                r.ExternalId = Guid.NewGuid().ToString("N");

            // owner important
            r.OwnerUserId = userId;

            // Save recette (upsert via external_id)
            await App.RecipesService!.UpsertRecetteAsync(userId, r);

            // récupérer recetteId DB à partir de ExternalId (⚠️ nouvelle signature: userId + externalId)
            var recetteId = await App.RecipesService.GetRecetteIdByExternalIdAsync(userId, r.ExternalId);
            if (recetteId == null)
                throw new Exception("Recette introuvable après upsert.");

            // envoyer ingrédients
            var ingSvc = new RecetteIngredientsService(App.RestClient!);
            var items = (r.Ingredients ?? new List<IngredientRecette>())
                .Select(i => (i.Nom, i.Quantite, i.Unite))
                .ToList();

            await ingSvc.ReplaceForRecetteAsync(recetteId.Value, items);

            await RefreshDepuisSupabaseAsync();

            MessageBox.Show(
                $"La recette '{r.Nom}' a été ajoutée avec succès !",
                "Succès",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void BtnSupprimerRecette_Click(object sender, RoutedEventArgs e)
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

            if (resultat != MessageBoxResult.Yes)
                return;

            try
            {
                if (!EnsureConnected(out _))
                    return;

                await App.RecipesService!.DeleteRecetteAsync(recette.Id);

                await RefreshDepuisSupabaseAsync();

                MessageBox.Show(
                    $"La recette '{recette.Nom}' a été supprimée.",
                    "Suppression réussie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression :\n{ex}");
            }
        }

        private async void BtnSyncRecettes_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDepuisSupabaseAsync();
            MessageBox.Show("Recettes rechargées depuis Supabase.");
        }
    }
}
