using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using LoGeCuiShared.Utils;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;

namespace LoGeCuiMobile.ViewModels
{
    public enum AvailabilityState
    {
        Unknown,
        AllAvailable,
        Missing
    }

    // ✅ UI wrapper
    public class RecetteUi : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Recette Model { get; }

        private bool _isSelectedForDelete;
        public bool IsSelectedForDelete
        {
            get => _isSelectedForDelete;
            set
            {
                if (_isSelectedForDelete == value) return;
                _isSelectedForDelete = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelectedForDelete)));
            }
        }

        private AvailabilityState _availabilityState = AvailabilityState.Unknown;
        public AvailabilityState AvailabilityState
        {
            get => _availabilityState;
            set
            {
                if (_availabilityState == value) return;
                _availabilityState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailabilityState)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailabilityLabel)));
            }
        }

        private int _missingCount;
        public int MissingCount
        {
            get => _missingCount;
            set
            {
                if (_missingCount == value) return;
                _missingCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MissingCount)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailabilityLabel)));
            }
        }

        public string AvailabilityLabel =>
            AvailabilityState == AvailabilityState.Unknown ? "Disponibilité inconnue"
            : AvailabilityState == AvailabilityState.AllAvailable ? "✅ Tous ingrédients disponibles"
            : $"❌ Manque {MissingCount} ingrédient(s)";

        public Guid Id => Model.Id;
        public string Nom => Model.Nom;
        public string TypeTexte => Model.TypeTexte;
        public int TempsPreparation => Model.TempsPreparation;
        public string DifficulteTexte => Model.DifficulteTexte;

        // ✅ Image pour la liste (local > url + cache busting optionnel)
        public ImageSource? PhotoSource
        {
            get
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(Model.PhotoLocalPath) && File.Exists(Model.PhotoLocalPath))
                        return ImageSource.FromFile(Model.PhotoLocalPath);

                    if (!string.IsNullOrWhiteSpace(Model.PhotoUrl))
                        return ImageSource.FromUri(new Uri(Model.PhotoUrl));

                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public void NotifyPhotoChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PhotoSource)));

        public RecetteUi(Recette r) => Model = r;
    }

    public class MesRecettesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly RecipesService _recipesService;

        public ObservableCollection<RecetteUi> Recettes { get; } = new();

        public ObservableCollection<string> Categories { get; } =
            new() { "Toutes", "Entrées", "Plats", "Desserts" };

        private string _selectedCategorie = "Toutes";
        public string SelectedCategorie
        {
            get => _selectedCategorie;
            set
            {
                if (_selectedCategorie == value) return;
                _selectedCategorie = value;
                OnPropertyChanged(nameof(SelectedCategorie));
                ApplyFilter();
            }
        }

        public ICommand RefreshCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private List<Recette> _all = new();

        public MesRecettesViewModel(RecipesService recipesService)
        {
            _recipesService = recipesService;
            RefreshCommand = new Command(async () => await LoadAsync(forceRemote: true));
        }

        private static bool IsJwtExpired(Exception ex)
            => ex.ToString().Contains("JWT expired", StringComparison.OrdinalIgnoreCase);

        private static bool IsNetworkRelated(Exception ex)
        {
            var msg = ex.ToString();
            return msg.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("network", StringComparison.OrdinalIgnoreCase);
        }

        // ✅ Normalisation unique partout (safe)
        private static string Norm(string? s)
        {
            var n = IngredientNormalizer.Normalize(s);
            return string.IsNullOrWhiteSpace(n) ? "" : n;
        }

        public async Task LoadAsync(bool forceRemote = false)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // 0) Cache local d'abord
                var local = await App.LocalDb.GetRecettesAsync();
                _all = local.Select(x => x.ToModel()).ToList();

                // 1) Remote si internet + connecté
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    var app = (App)Application.Current;

                    if (app.IsConnected && app.CurrentUserId != null)
                    {
                        try
                        {
                            var remote = await _recipesService.GetRecettesAsync(app.CurrentUserId.Value)
                                         ?? new List<Recette>();

                            // Ne pas écraser le cache par vide si pas force
                            if (forceRemote || remote.Count > 0)
                            {
                                _all = remote;
                                await App.LocalDb.SaveRecettesAsync(remote);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!(IsJwtExpired(ex) || IsNetworkRelated(ex)))
                                throw;
                        }
                    }
                }

                ApplyFilter();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            IEnumerable<Recette> filtered = _all;

            if (SelectedCategorie == "Entrées")
                filtered = _all.Where(r => r.Type == TypePlat.Entree);
            else if (SelectedCategorie == "Plats")
                filtered = _all.Where(r => r.Type == TypePlat.Plat);
            else if (SelectedCategorie == "Desserts")
                filtered = _all.Where(r => r.Type == TypePlat.Dessert);

            Recettes.Clear();
            foreach (var r in filtered)
                Recettes.Add(new RecetteUi(r));
        }

        public async Task RefreshAvailabilityAsync()
        {
            var app = (App)Application.Current;

            // Offline => Unknown
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                foreach (var r in Recettes)
                {
                    r.AvailabilityState = AvailabilityState.Unknown;
                    r.MissingCount = 0;
                }
                return;
            }

            if (!app.IsConnected || app.CurrentUserId == null || app.IngredientsService == null || app.RecetteIngredientsService == null)
            {
                foreach (var r in Recettes)
                {
                    r.AvailabilityState = AvailabilityState.Unknown;
                    r.MissingCount = 0;
                }
                return;
            }

            try
            {
                var userId = app.CurrentUserId.Value;

                // ✅ ingrédients utilisateur disponibles
                var userIngredients = await app.IngredientsService.GetIngredientsAsync(userId) ?? new List<Ingredient>();
                var userSet = userIngredients
                    .Where(i => i.EstDisponible)
                    .Select(i => Norm(i.Nom))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var r in Recettes)
                {
                    var rows = await app.RecetteIngredientsService.GetForRecetteAsync(r.Model.Id);

                    var recipeNames = rows
                        .Select(x => Norm(x.nom))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (recipeNames.Count == 0)
                    {
                        r.AvailabilityState = AvailabilityState.Unknown;
                        r.MissingCount = 0;
                        continue;
                    }

                    var missing = recipeNames
                        .Where(n => !userSet.Contains(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    r.MissingCount = missing.Count;
                    r.AvailabilityState = (missing.Count == 0)
                        ? AvailabilityState.AllAvailable
                        : AvailabilityState.Missing;
                }
            }
            catch (Exception ex)
            {
                if (IsJwtExpired(ex) || IsNetworkRelated(ex))
                {
                    foreach (var r in Recettes)
                    {
                        r.AvailabilityState = AvailabilityState.Unknown;
                        r.MissingCount = 0;
                    }
                    return;
                }

                throw;
            }
        }

        public async Task<List<string>?> GetMissingIngredientsForRecipeAsync(Recette recette)
        {
            var app = (App)Application.Current;

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return null;

            if (!app.IsConnected || app.CurrentUserId == null || app.IngredientsService == null || app.RecetteIngredientsService == null)
                return null;

            try
            {
                var userId = app.CurrentUserId.Value;

                var userIngredients = await app.IngredientsService.GetIngredientsAsync(userId) ?? new List<Ingredient>();
                var userSet = userIngredients
                    .Where(i => i.EstDisponible)
                    .Select(i => Norm(i.Nom))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var rows = await app.RecetteIngredientsService.GetForRecetteAsync(recette.Id);

                var recipeNames = rows
                    .Select(x => Norm(x.nom))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (recipeNames.Count == 0)
                    return new List<string>(); // pas null => "aucun ingrédient renseigné"

                var missing = recipeNames
                    .Where(n => !userSet.Contains(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return missing;
            }
            catch (Exception ex)
            {
                if (IsJwtExpired(ex) || IsNetworkRelated(ex))
                    return null;

                throw;
            }
        }

        public async Task<bool> SendMissingToShoppingAsync(List<string> missing)
        {
            var app = (App)Application.Current;

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return false;

            if (!app.IsConnected || app.CurrentUserId == null || app.ListeCoursesSupabaseService == null)
                return false;

            // ✅ FORCER la récupération de la liste si null
            if (app.CurrentShoppingListId == null && app.RestClient != null)
            {
                try
                {
                    var url = $"shopping_lists?select=id&owner_user_id=eq.{app.CurrentUserId.Value}&limit=1";
                    var result = await app.RestClient.GetAsync<List<Dictionary<string, object>>>(url);

                    if (result != null && result.Count > 0 && result[0].ContainsKey("id"))
                    {
                        var idObj = result[0]["id"];
                        if (idObj != null)
                        {
                            app.SetCurrentShoppingListId(Guid.Parse(idObj.ToString()));
                        }
                    }
                }
                catch { }
            }

            var listId = app.CurrentShoppingListId;
            if (listId == null || listId.Value == Guid.Empty)
                return false;

            try
            {
                await app.ListeCoursesSupabaseService.AddMissingAsync(app.CurrentUserId.Value, missing, listId.Value);
                return true;
            }
            catch (Exception ex)
            {
                if (IsJwtExpired(ex) || IsNetworkRelated(ex))
                    return false;

                throw;
            }
        }

        public async Task DeleteSelectedAsync(List<RecetteUi> selected)
        {
            var app = (App)Application.Current;

            // Optimistic UI
            foreach (var r in selected.ToList())
                Recettes.Remove(r);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet && app.IsConnected && app.RecipesService != null)
            {
                try
                {
                    foreach (var r in selected)
                        await app.RecipesService.DeleteRecetteAsync(r.Model.Id);
                }
                catch (Exception ex)
                {
                    if (!IsJwtExpired(ex) && !IsNetworkRelated(ex))
                        throw;
                }
            }

            await LoadAsync(forceRemote: true);

            try
            {
                await RefreshAvailabilityAsync();
            }
            catch (Exception ex)
            {
                if (!IsJwtExpired(ex) && !IsNetworkRelated(ex))
                    throw;
            }
        }

        public Task DeleteOneAsync(RecetteUi one)
            => DeleteSelectedAsync(new List<RecetteUi> { one });
    }
}
