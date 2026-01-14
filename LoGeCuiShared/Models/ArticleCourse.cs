using System;
using System.Collections.Generic;
using System.Text;

namespace LoGeCuiShared.Models
{
    public class ArticleCourse
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Quantite { get; set; }
        public string Unite { get; set; }
        public bool EstAchete { get; set; }

        public ArticleCourse()
        {
            Id = 0;
            Nom = "";
            Quantite = "";
            Unite = "";
            EstAchete = false;
        }

        public override string ToString()
        {
            return $"{Nom} - {Quantite} {Unite}";
        }
    }
}
