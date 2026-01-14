using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;

namespace LoGeCui.Views
{
    public partial class ListeCoursesView : UserControl
    {
        private ObservableCollection<ArticleCourse> _articles = new ObservableCollection<ArticleCourse>();
        private SupabaseService _supabase;

        public ListeCoursesView()  // ← PARAMÈTRE ICI !
        {
            InitializeComponent();
            string url = "https://wzctiypsadqktzcnswri.supabase.co";
            string key = "sb_publishable_ZFk8ONON5qMA0vZ3V0nVAg_TZZrO1F1";
            _supabase = new SupabaseService(url, key);
            ChargerDonnees();
        }

        private async void ChargerDonnees()
        {
            try
            {
                var articles = await _supabase.GetArticlesAsync();

                _articles.Clear();
                foreach (var article in articles)
                {
                    _articles.Add(article);
                }

                ListeCourses.ItemsSource = _articles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAjouterArticle_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AjouterArticleCourseDialog();
            dialog.Owner = Window.GetWindow(this);

            bool? resultat = dialog.ShowDialog();

            if (resultat == true && dialog.NouvelArticle != null)
            {
                try
                {
                    var added = await _supabase.AddArticleAsync(dialog.NouvelArticle);

                    if (added != null)
                    {
                        _articles.Add(added);
                        MessageBox.Show($"'{added.Nom}' ajouté avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

            if (resultat == MessageBoxResult.Yes)
            {
                try
                {
                    await _supabase.DeleteArticleAsync(article.Id);
                    _articles.Remove(article);
                    MessageBox.Show("Article supprimé !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ChkAchete_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as System.Windows.Controls.CheckBox;
            var article = checkbox?.DataContext as ArticleCourse;

            if (article != null)
            {
                try
                {
                    await _supabase.UpdateArticleAsync(article.Id, article);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur de synchronisation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnSupprimerAchetes_Click(object sender, RoutedEventArgs e)
        {
            var achetes = _articles.Where(a => a.EstAchete).ToList();

            if (!achetes.Any())
            {
                MessageBox.Show("Aucun article acheté à supprimer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var resultat = MessageBox.Show(
                $"Voulez-vous supprimer les {achetes.Count} article(s) acheté(s) ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultat == MessageBoxResult.Yes)
            {
                try
                {
                    await _supabase.DeleteAchetesAsync();

                    foreach (var article in achetes)
                    {
                        _articles.Remove(article);
                    }

                    MessageBox.Show($"{achetes.Count} article(s) supprimé(s).", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

