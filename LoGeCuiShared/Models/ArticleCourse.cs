using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LoGeCuiShared.Models
{
    public class ArticleCourse : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private int _id;
        private Guid _userId;
        private string _nom = "";
        private string _quantite = "";
        private string _unite = "";
        private bool _estAchete;

        [JsonPropertyName("id")]
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("user_id")]
        public Guid UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("nom")]
        public string Nom
        {
            get => _nom;
            set { _nom = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("quantite")]
        public string Quantite
        {
            get => _quantite;
            set { _quantite = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("unite")]
        public string Unite
        {
            get => _unite;
            set { _unite = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("est_achete")]
        public bool EstAchete
        {
            get => _estAchete;
            set { _estAchete = value; OnPropertyChanged(); }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}