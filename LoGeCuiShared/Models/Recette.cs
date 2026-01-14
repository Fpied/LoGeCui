using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LoGeCuiShared.Models
{
    public enum TypePlat
    {
        Entree,
        Plat,
        Dessert
    }

    public class Recette
    {
        public string Nom { get; set; }
        public TypePlat Type { get; set; }
        public int TempsPreparation { get; set; }
        public int Difficulte { get; set; }
        public List<IngredientRecette> Ingredients { get; set; }
        public string Instructions { get; set; }

        public Recette()
        {
            Nom = "";
            Type = TypePlat.Plat;
            TempsPreparation = 0;
            Difficulte = 1;
            Ingredients = new List<IngredientRecette>();
            Instructions = "";
        }

        [JsonIgnore]
        public string TypeTexte
        {
            get
            {
                return Type switch
                {
                    TypePlat.Entree => "Entrée",
                    TypePlat.Plat => "Plat",
                    TypePlat.Dessert => "Dessert",
                    _ => "Inconnu"
                };
            }
        }

        [JsonIgnore]
        public string DifficulteTexte
        {
            get
            {
                return new string('⭐', Difficulte);
            }
        }
    }
}
