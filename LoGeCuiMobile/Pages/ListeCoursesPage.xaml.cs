using System;
using System.Collections.ObjectModel;
using System.Linq;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using LoGeCuiMobile.Models;

namespace LoGeCuiMobile.Pages
{
    public partial class ListeCoursesPage : ContentPage
    {
        private readonly ObservableCollection<ArticleCourseUi> _articles = new();
        private readonly SupabaseService _supabase;

        // Empêche la checkbox "Tout sélectionner" de déclencher une sélection de masse
        // quand on la met à jour depuis le code.
        private bool _suppressSelectAllEvent;

        public ListeCoursesPage()
        {
            InitializeComponent();

            _supabase = ((App)Application.Current).Supabase
                ?? throw new InvalidOperationException("Supabase non initialisé.");

            // IMPORTANT : ne pas re-s'abonner ici si ton XAML a déjà Clicked="BtnSupprimerSelection_Clicked"
            // BtnSupprimerSelection.Clicked += BtnSupprimerSelection_Clicked;  <-- SUPPRIMÉ

            ListeCourses.ItemsSource = _articles;

            ChargerDonnees();
        }

        private async void ChargerDonnees()
        {
            try
            {
                // 1️⃣ Charger depuis le cache local
                var local = await App.LocalDb.GetArticlesAsync();
                _articles.Clear();
                foreach (var a in local)
                    _articles.Add(new ArticleCourseUi(a.ToModel()));

                // 2️⃣ Si internet → rafraîchir depuis Supabase
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    var remote = await _supabase.GetArticlesAsync();

                    _articles.Clear();
                    foreach (var a in remote)
                        _articles.Add(new ArticleCourseUi(a));

                    // 🔄 Mettre à jour le cache
                    await App.LocalDb.SaveArticlesAsync(remote);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Chargement impossible : {ex.Message}", "OK");
            }
        }

        private async void BtnSupprimerSelection_Clicked(object sender, EventArgs e)
        {
            var selected = _articles.Where(a => a.IsSelectedForDelete).ToList();

            if (selected.Count == 0)
            {
                await DisplayAlert("Information", "Aucun article sélectionné.", "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Supprimer {selected.Count} article(s) sélectionné(s) ?",
                "Oui",
                "Non");

            if (!confirm)
                return;

            try
            {
                foreach (var articleUi in selected)
                {
                    await _supabase.DeleteArticleAsync(articleUi.Id);
                    _articles.Remove(articleUi);
                }

                RefreshSelectAllCheckBox();

                await DisplayAlert("Succès", $"{selected.Count} article(s) supprimé(s).", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Suppression impossible : {ex.Message}", "OK");
                ChargerDonnees();
            }
        }

        private async void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            // Checkbox "Acheté"
            if (sender is not CheckBox checkbox) return;
            if (checkbox.BindingContext is not ArticleCourseUi articleUi) return;

            var article = articleUi.Model;

            checkbox.IsEnabled = false;
            var oldValue = !e.Value;

            try
            {
                await _supabase.UpdateArticleAsync(article.Id, article);
            }
            catch (Exception ex)
            {
                article.EstAchete = oldValue;
                checkbox.IsChecked = oldValue;

                await DisplayAlert("Erreur", $"Synchronisation échouée : {ex.Message}", "OK");
            }
            finally
            {
                checkbox.IsEnabled = true;
            }
        }

        // Checkbox "Tout sélectionner" (pour suppression)
        private void ChkToutSelectionner_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (_suppressSelectAllEvent) return;

            foreach (var a in _articles)
                a.IsSelectedForDelete = e.Value;

            // Optionnel : si tu veux désactiver le bouton supprimer quand rien n'est coché,
            // il faudra écouter les changements de IsSelectedForDelete (INotifyPropertyChanged).
            // Ici on se contente de garder la checkbox cohérente.
        }

        private void RefreshSelectAllCheckBox()
        {
            if (ChkToutSelectionner == null) return;

            _suppressSelectAllEvent = true;
            try
            {
                if (_articles.Count == 0)
                {
                    ChkToutSelectionner.IsEnabled = false;
                    ChkToutSelectionner.IsChecked = false;
                    return;
                }

                ChkToutSelectionner.IsEnabled = true;

                // MAUI CheckBox n'a pas de tri-state :
                // on met "checked" seulement si TOUT est sélectionné.
                bool allSelected = _articles.All(a => a.IsSelectedForDelete);
                ChkToutSelectionner.IsChecked = allSelected;
            }
            finally
            {
                _suppressSelectAllEvent = false;
            }
        }

        private async void BtnAjouter_Clicked(object sender, EventArgs e)
        {
            string nom = await DisplayPromptAsync("Ajouter", "Nom de l'article :");
            if (string.IsNullOrWhiteSpace(nom))
                return;

            string? quantite = await DisplayPromptAsync("Ajouter", "Quantité (optionnel) :");
            string? unite = await DisplayPromptAsync("Ajouter", "Unité (optionnel) :");

            try
            {
                var app = (App)Application.Current;
                if (app.CurrentUserId == null)
                {
                    await DisplayAlert("Erreur", "Vous devez être connecté.", "OK");
                    return;
                }

                var article = new ArticleCourse
                {
                    UserId = app.CurrentUserId.Value,
                    Nom = nom,
                    Quantite = quantite,
                    Unite = unite,
                    EstAchete = false
                };

                var added = await _supabase.AddArticleAsync(article);
                if (added != null)
                {
                    _articles.Add(new ArticleCourseUi(added));

                    // Nouveau item => on met à jour la checkbox "Tout sélectionner"
                    RefreshSelectAllCheckBox();

                    await DisplayAlert("Succès", $"'{added.Nom}' ajouté !", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible d'ajouter : {ex.Message}", "OK");
            }
        }
    }
}
