using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Models
{
    // Wrapper UI: on conserve ArticleCourse et on ajoute une sélection locale (non persistée)
    public class ArticleCourseUi : INotifyPropertyChanged
    {
        public ArticleCourseUi(ArticleCourse model) => Model = model;

        public ArticleCourse Model { get; }

        public int Id => Model.Id;

        public string Nom
        {
            get => Model.Nom;
            set { if (Model.Nom != value) { Model.Nom = value; OnPropertyChanged(); } }
        }

        public string Quantite
        {
            get => Model.Quantite;
            set { if (Model.Quantite != value) { Model.Quantite = value; OnPropertyChanged(); } }
        }

        public string Unite
        {
            get => Model.Unite;
            set { if (Model.Unite != value) { Model.Unite = value; OnPropertyChanged(); } }
        }

        public bool EstAchete
        {
            get => Model.EstAchete;
            set { if (Model.EstAchete != value) { Model.EstAchete = value; OnPropertyChanged(); } }
        }

        private bool _isSelectedForDelete;
        public bool IsSelectedForDelete
        {
            get => _isSelectedForDelete;
            set { if (_isSelectedForDelete != value) { _isSelectedForDelete = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

