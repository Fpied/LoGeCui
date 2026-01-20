using System;
using System.Collections.Generic;
using System.Text;

namespace LoGeCuiShared.Models
{
    public class Ingredient
    {
        public Guid Id { get; set; }          // id (uuid) dans Supabase
        public Guid? UserId { get; set; }     // user_id (uuid) dans Supabase
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
