using System.Collections.ObjectModel;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;

namespace LoGeCuiMobile.Pages
{
    public partial class ListeCoursesPage : ContentPage
    {
        private ObservableCollection<ArticleCourse> _articles = new ObservableCollection<ArticleCourse>();
        private SupabaseService _supabase;

        public ListeCoursesPage()
        {
            InitializeComponent();

            string url = ConfigurationHelper.GetSupabaseUrl();
            string key = ConfigurationHelper.GetSupabaseKey();
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
                await DisplayAlert("Erreur", $"Impossible de charger : {ex.Message}", "OK");
            }
        }

        private async void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var article = checkbox?.BindingContext as ArticleCourse;

            if (article != null)
            {
                try
                {
                    await _supabase.UpdateArticleAsync(article.Id, article);

                    // Force le refresh visuel
                    var index = _articles.IndexOf(article);
                    if (index >= 0)
                    {
                        _articles.RemoveAt(index);
                        _articles.Insert(index, article);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur", $"Synchronisation échouée : {ex.Message}", "OK");
                }
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
                var article = new ArticleCourse
                {
                    Nom = nom,
                    Quantite = quantite,
                    Unite = unite,
                    EstAchete = false
                };

                var added = await _supabase.AddArticleAsync(article);
                if (added != null)
                {
                    _articles.Add(added);
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