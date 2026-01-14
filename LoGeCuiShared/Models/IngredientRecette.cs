using System;
using System.Collections.Generic;
using System.Text;

namespace LoGeCuiShared.Models
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

        public override string ToString()
        {
            return $"{Nom} - {Quantite} {Unite}";
        }
    }
}
