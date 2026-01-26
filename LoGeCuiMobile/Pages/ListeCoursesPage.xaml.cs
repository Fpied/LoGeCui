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

        public ListeCoursesPage()
        {
            InitializeComponent();

            _supabase = ((App)Application.Current).Supabase
                ?? throw new InvalidOperationException("Supabase non initialisé.");

            // Plus besoin de SelectionChanged / SelectedItems : on passe par IsSelectedForDelete
            BtnSupprimerSelection.Clicked += BtnSupprimerSelection_Clicked;

            ListeCourses.ItemsSource = _articles;

            // Bouton actif en permanence; il affichera un message si rien n’est coché
            BtnSupprimerSelection.IsEnabled = true;

            if (SelectionInfoLabel != null)
                SelectionInfoLabel.Text = "";

            ChargerDonnees();
        }

        private async void ChargerDonnees()
        {
            try
            {
                var articles = await _supabase.GetArticlesAsync();

                _articles.Clear();
                foreach (var a in articles)
                    _articles.Add(new ArticleCourseUi(a));

                if (SelectionInfoLabel != null)
                    SelectionInfoLabel.Text = "";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible de charger : {ex.Message}", "OK");
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

                if (SelectionInfoLabel != null)
                    SelectionInfoLabel.Text = "";

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
            var checkbox = (CheckBox)sender;
            var articleUi = checkbox.BindingContext as ArticleCourseUi;
            if (articleUi == null) return;

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

        private async void BtnAjouter_Clicked(object sender, EventArgs e)
        {
            string nom = await DisplayPromptAsync("Ajouter", "Nom de l'article :");
            if (string.IsNullOrWhiteSpace(nom))
                return;

            string quantite = await DisplayPromptAsync("Ajouter", "Quantité :");
            if (string.IsNullOrWhiteSpace(quantite))
                return;

            string unite = await DisplayPromptAsync("Ajouter", "Unité (g, kg, L, pièces...) :");
            if (string.IsNullOrWhiteSpace(unite))
                return;

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
