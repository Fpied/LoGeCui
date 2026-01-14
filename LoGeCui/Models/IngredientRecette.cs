using System;
using System.Collections.Generic;
using System.Text;


namespace LoGeCui.Models
{
    public class IngredientRecette
    {
        public string Nom { get; set; }
        public string Quantite { get; set; }
        public string Unite { get; set; }

        public IngredientRecette()
        {
            Nom = "";
            Quantite = "";
            Unite = "";
        }

        // Pour l'affichage
        public override string ToString()
        {
            return $"{Nom} - {Quantite} {Unite}";
        }
    }
}
