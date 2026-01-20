using System;
using System.Collections.Generic;
using LoGeCuiShared.Models;

namespace LoGeCui.Models
{
    public class MenuJournalier
    {
        public DateTime Date { get; set; }
        public Recette? Entree { get; set; }
        public Recette? Plat { get; set; }
        public Recette? Dessert { get; set; }
        public List<IngredientRecette> IngredientsManquants { get; set; }

        public MenuJournalier()
        {
            Date = DateTime.Now;
            IngredientsManquants = new List<IngredientRecette>();
        }
    }
}
