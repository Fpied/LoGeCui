using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LoGeCuiShared.Models;

namespace LoGeCui.Views
{
    public partial class ListeCoursesView : UserControl
    {
        private readonly ObservableCollection<ArticleCourse> _articles = new ObservableCollection<ArticleCourse>();

        public ListeCoursesView()
        {
            InitializeComponent();

            // Binding une fois
            ListeCourses.ItemsSource = _articles;

            ChargerDonnees();
        }

        private async void ChargerDonnees()
        {
            try
            {
                var articles = await App.SupabaseService.GetArticlesAsync();

                _articles.Clear();
                foreach (var article in articles)
                    _articles.Add(article);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAjouterArticle_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterArticleCourseDialog
            {
                Owner = Window.GetWindow(this)
            };

            bool? resultat = dialog.ShowDialog();

            if (resultat == true && dialog.NouvelArticle != null)
            {
                try
                {
                    var added = await App.SupabaseService.AddArticleAsync(dialog.NouvelArticle);

                    if (added != null)
                    {
                        _articles.Add(added);
                        MessageBox.Show($"'{added.Nom}' ajouté avec succès !", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnSupprimerArticle_Click(object sender, RoutedEventArgs e)
        {
            var bouton = sender as Button;
            var article = bouton?.Tag as ArticleCourse;

            if (article == null)
                return;

            var resultat = MessageBox.Show(
                $"Voulez-vous vraiment supprimer '{article.Nom}' ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultat != MessageBoxResult.Yes)
                return;

            try
            {
                await App.SupabaseService.DeleteArticleAsync(article.Id);
                _articles.Remove(article);

                MessageBox.Show("Article supprimé !", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChkAchete_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var article = checkbox?.DataContext as ArticleCourse;

            if (article == null)
                return;

            try
            {
                await App.SupabaseService.UpdateArticleAsync(article.Id, article);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de synchronisation : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToutSelectionner_Click(object sender, RoutedEventArgs e)
        {
            ListeCourses.SelectAll();
        }

        private async void BtnSupprimerSelection_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: SelectedItems est un IList -> on copie avant toute suppression
            var selected = ListeCourses.SelectedItems.Cast<ArticleCourse>().ToList();

            if (!selected.Any())
            {
                MessageBox.Show("Aucun article sélectionné.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var resultat = MessageBox.Show(
                $"Voulez-vous supprimer {selected.Count} article(s) sélectionné(s) ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultat != MessageBoxResult.Yes)
                return;

            try
            {
                // Variante qui fonctionne tout de suite, sans ajouter de méthode Supabase en lot :
                // on supprime en boucle via la méthode existante DeleteArticleAsync(id).
                foreach (var article in selected)
                {
                    await App.SupabaseService.DeleteArticleAsync(article.Id);
                    _articles.Remove(article);
                }

                MessageBox.Show($"{selected.Count} article(s) supprimé(s).", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSupprimerAchetes_Click(object sender, RoutedEventArgs e)
        {
            var achetes = _articles.Where(a => a.EstAchete).ToList();

            if (!achetes.Any())
            {
                MessageBox.Show("Aucun article acheté à supprimer.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var resultat = MessageBox.Show(
                $"Voulez-vous supprimer les {achetes.Count} article(s) acheté(s) ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultat != MessageBoxResult.Yes)
                return;

            try
            {
                await App.SupabaseService.DeleteAchetesAsync();

                foreach (var article in achetes)
                    _articles.Remove(article);

                MessageBox.Show($"{achetes.Count} article(s) supprimé(s).", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


