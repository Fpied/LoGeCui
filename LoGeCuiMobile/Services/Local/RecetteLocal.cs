using System;
using SQLite;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Services.Local
{
    public class RecetteLocal
    {
        [PrimaryKey]
        public string Id { get; set; } = ""; // Guid stocké en texte

        public string Nom { get; set; } = "";
        public int TempsPreparation { get; set; }
        public string Instructions { get; set; } = "";
        public string CategorieDb { get; set; } = "";
        public int Type { get; set; } // enum -> int

        // ✅ Pour éviter les doublons à l’édition (upsert stable)
        public string? ExternalId { get; set; }

        // ✅ utile pour cohérence (optionnel)
        public string? OwnerUserId { get; set; }

        // ✅ Photos (sync + cache)
        public string? PhotoUrl { get; set; }
        public string? PhotoLocalPath { get; set; }

        public RecetteLocal() { }

        public RecetteLocal(Recette r)
        {
            Id = r.Id.ToString();

            Nom = r.Nom ?? "";
            TempsPreparation = r.TempsPreparation;
            Instructions = r.Instructions ?? "";
            CategorieDb = r.CategorieDb ?? "";
            Type = (int)r.Type;

            ExternalId = r.ExternalId;
            OwnerUserId = r.OwnerUserId?.ToString();

            PhotoUrl = r.PhotoUrl;
            PhotoLocalPath = r.PhotoLocalPath;
        }

        public Recette ToModel()
        {
            Guid.TryParse(Id, out var guid);

            Guid? owner = null;
            if (!string.IsNullOrWhiteSpace(OwnerUserId) && Guid.TryParse(OwnerUserId, out var ouid))
                owner = ouid;

            return new Recette
            {
                Id = guid,
                Nom = Nom,
                TempsPreparation = TempsPreparation,
                Instructions = Instructions,
                CategorieDb = CategorieDb,
                Type = (TypePlat)Type,

                ExternalId = ExternalId,
                OwnerUserId = owner,

                PhotoUrl = PhotoUrl,
                PhotoLocalPath = PhotoLocalPath
            };
        }
    }
}
