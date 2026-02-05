using SQLite;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Services.Local
{
    public class ArticleLocal
    {
        [PrimaryKey]
        public int Id { get; set; }   // int

        public string Nom { get; set; } = "";
        public string Quantite { get; set; } = "";
        public string Unite { get; set; } = "";
        public bool EstAchete { get; set; }

        public ArticleLocal() { }

        public ArticleLocal(ArticleCourse a)
        {
            Id = a.Id;                 // int -> int ✅
            Nom = a.Nom ?? "";
            Quantite = a.Quantite ?? "";
            Unite = a.Unite ?? "";
            EstAchete = a.EstAchete;
        }

        public ArticleCourse ToModel()
        {
            return new ArticleCourse
            {
                Id = Id,              // int -> int ✅
                Nom = Nom,
                Quantite = Quantite,
                Unite = Unite,
                EstAchete = EstAchete
            };
        }
    }
}
