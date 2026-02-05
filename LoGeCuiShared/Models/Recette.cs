using System;
using System.Collections.Generic;
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
        // ----------------------------
        // Champs DB / JSON (Supabase)
        // ----------------------------

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("owner_user_id")]
        public Guid? OwnerUserId { get; set; }

        [JsonPropertyName("external_id")]
        public string? ExternalId { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; } = "";

        [JsonPropertyName("categorie")]
        public string CategorieDb
        {
            get => Type switch
            {
                TypePlat.Entree => "Entree",
                TypePlat.Plat => "Plat",
                TypePlat.Dessert => "Dessert",
                _ => "Plat"
            };
            set
            {
                Type = value switch
                {
                    "Entree" => TypePlat.Entree,
                    "Plat" => TypePlat.Plat,
                    "Dessert" => TypePlat.Dessert,
                    _ => TypePlat.Plat
                };
            }
        }

        // Colonne DB: temps_minutes
        [JsonPropertyName("temps_minutes")]
        public int TempsMinutesDb
        {
            get => TempsPreparation;
            set => TempsPreparation = value;
        }

        // Colonne DB: note (1..5)
        [JsonPropertyName("note")]
        public int? NoteDb
        {
            get => Difficulte;
            set => Difficulte = Math.Clamp(value ?? 1, 1, 5);
        }

        [JsonPropertyName("is_favorite")]
        public bool IsFavorite { get; set; }

        [JsonPropertyName("instructions")]
        public string InstructionsDb
        {
            get => Instructions;
            set => Instructions = value ?? "";
        }

        // ✅ Photo synchronisée (DB Supabase: photo_url)
        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }

        // ✅ Photo locale (cache sur l'appareil) - NON envoyée à Supabase
        [JsonIgnore]
        public string? PhotoLocalPath { get; set; }

        // ----------------------------
        // Propriétés métier (non DB)
        // ----------------------------

        [JsonIgnore]
        public TypePlat Type { get; set; } = TypePlat.Plat;

        [JsonIgnore]
        public int TempsPreparation { get; set; } = 0;

        [JsonIgnore]
        public int Difficulte { get; set; } = 1;

        [JsonIgnore]
        public List<IngredientRecette> Ingredients { get; set; } = new();

        [JsonIgnore]
        public string Instructions { get; set; } = "";

        [JsonIgnore]
        public string TypeTexte => Type switch
        {
            TypePlat.Entree => "Entrée",
            TypePlat.Plat => "Plat",
            TypePlat.Dessert => "Dessert",
            _ => "Inconnu"
        };

        [JsonIgnore]
        public string DifficulteTexte => new string('⭐', Math.Clamp(Difficulte, 1, 5));
    }
}
