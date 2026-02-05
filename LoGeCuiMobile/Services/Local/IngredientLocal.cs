using SQLite;
using LoGeCuiShared.Models;

public class IngredientLocal
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Nom { get; set; } = "";
    public string? Quantite { get; set; }
    public string? Unite { get; set; }
    public bool EstDisponible { get; set; }

    public IngredientLocal() { }

    public IngredientLocal(Ingredient i)
    {
        Id = i.Id;
        Nom = i.Nom;
        Quantite = i.Quantite;
        Unite = i.Unite;
        EstDisponible = i.EstDisponible;
    }

    public Ingredient ToModel() => new Ingredient
    {
        Id = Id,
        Nom = Nom,
        Quantite = Quantite,
        Unite = Unite,
        EstDisponible = EstDisponible
    };
}
