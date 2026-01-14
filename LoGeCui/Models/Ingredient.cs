namespace LoGeCui.Models
{
    public class Ingredient
    {
        public string Nom { get; set; }
        public string Quantite { get; set; }
        public string Unite { get; set; }
        public bool EstDisponible { get; set; }

        // Constructeur (optionnel mais pratique)
        public Ingredient()
        {
            Nom = "";
            Quantite = "";
            Unite = "";
            EstDisponible = true;
        }
    }
}
