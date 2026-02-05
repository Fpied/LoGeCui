using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Models
{
    public class IngredientUi : INotifyPropertyChanged
    {
        public Ingredient Model { get; }

        public IngredientUi(Ingredient model)
        {
            Model = model;
        }

        public System.Guid Id => Model.Id;
        public string Nom => Model.Nom ?? "";
        public string? Quantite => Model.Quantite;
        public string? Unite => Model.Unite;

        public bool EstDisponible
        {
            get => Model.EstDisponible;
            set
            {
                if (Model.EstDisponible == value) return;
                Model.EstDisponible = value;
                OnPropertyChanged();
            }
        }

        // ✅ NOUVEAU : Favori (nécessite Model.EstFavori)
        public bool EstFavori
        {
            get => Model.EstFavori;
            set
            {
                if (Model.EstFavori == value) return;
                Model.EstFavori = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeartText));
            }
        }

        // ✅ NOUVEAU : emoji coeur
        public string HeartText => EstFavori ? "❤️" : "🤍";

        bool _isSelectedForDelete;
        public bool IsSelectedForDelete
        {
            get => _isSelectedForDelete;
            set
            {
                if (_isSelectedForDelete == value) return;
                _isSelectedForDelete = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
