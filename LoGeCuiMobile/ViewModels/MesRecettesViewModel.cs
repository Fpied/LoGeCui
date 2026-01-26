using System.Collections.ObjectModel;
using System.Windows.Input;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;

namespace LoGeCuiMobile.ViewModels
{
    public sealed class MesRecettesViewModel : BindableObject
    {
        private readonly RecipesService _recipesService;

        public ObservableCollection<Recette> Recettes { get; } = new();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _selectedCategorie = "Toutes";
        public string SelectedCategorie
        {
            get => _selectedCategorie;
            set
            {
                if (_selectedCategorie == value) return;
                _selectedCategorie = value;
                OnPropertyChanged();
                _ = LoadAsync(); // recharge quand le filtre change
            }
        }

        public IReadOnlyList<string> Categories { get; } = new[] { "Toutes", "Entree", "Plat", "Dessert" };

        public ICommand RefreshCommand { get; }

        public MesRecettesViewModel(RecipesService recipesService)
        {
            _recipesService = recipesService;
            RefreshCommand = new Command(async () => await LoadAsync());
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Recettes.Clear();

                var cat = SelectedCategorie == "Toutes" ? null : SelectedCategorie;

                List<Recette> list;
                try
                {
                    list = await _recipesService.GetMyRecettesAsync(cat);
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        Application.Current.MainPage.DisplayAlert("Erreur API", ex.Message, "OK"));
                    return;
                }

                foreach (var r in list)
                    Recettes.Add(r);
            }
            finally
            {
                IsBusy = false;
            }
        }
    
    }
}

