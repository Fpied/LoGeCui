using System;
using System.Collections.Generic;
using System.Text;

namespace LoGeCuiShared.Models
{
    public class Ingredient
    {
        public string Nom { get; set; }
        public string Quantite { get; set; }
        public string Unite { get; set; }
        public bool EstDisponible { get; set; }

        public Ingredient()
        {
            Nom = "";
            Quantite = "";
            Unite = "";
            EstDisponible = true;
        }
    }
}
