namespace LoGeCui.Models
{
    public class ArticleCourse
    {
        public string Nom { get; set; }
        public string Quantite { get; set; }
        public string Unite { get; set; }
        public bool EstAchete { get; set; }

        public ArticleCourse()
        {
            Nom = "";
            Quantite = "";
            Unite = "";
            EstAchete = false;
        }

        // Pour l'affichage
        public override string ToString()
        {
            return $"{Nom} - {Quantite} {Unite}";
        }
    }
}
