using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoGeCuiShared.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LoGeCuiShared.Services
{
    public class SupabaseService
    {
        private readonly Client _client;

        public SupabaseService(string url, string key)
        {
            var options = new ClientOptions
            {
                Headers = new Dictionary<string, string>
                {
                    { "apikey", key },
                    { "Authorization", $"Bearer {key}" }
                }
            };

            _client = new Client($"{url}/rest/v1", options);
        }

        // Récupérer tous les articles
        public async Task<List<ArticleCourse>> GetArticlesAsync()
        {
            var response = await _client
                .Table<ArticleCourseDb>()
                .Get();

            return response.Models.Select(db => new ArticleCourse
            {
                Id = db.Id,
                Nom = db.Nom ?? "",
                Quantite = db.Quantite ?? "",
                Unite = db.Unite ?? "",
                EstAchete = db.EstAchete
            }).ToList();
        }

        // Ajouter un article
        public async Task<ArticleCourse?> AddArticleAsync(ArticleCourse article)
        {
            var dbArticle = new ArticleCourseDb
            {
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete
            };

            var response = await _client
                .Table<ArticleCourseDb>()
                .Insert(dbArticle);

            var inserted = response.Models.FirstOrDefault();
            if (inserted != null)
            {
                return new ArticleCourse
                {
                    Id = inserted.Id,
                    Nom = inserted.Nom ?? "",
                    Quantite = inserted.Quantite ?? "",
                    Unite = inserted.Unite ?? "",
                    EstAchete = inserted.EstAchete
                };
            }

            return null;
        }

        // Mettre à jour un article
        public async Task<bool> UpdateArticleAsync(int id, ArticleCourse article)
        {
            var dbArticle = new ArticleCourseDb
            {
                Id = id,
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete
            };

            await _client
                .Table<ArticleCourseDb>()
                .Update(dbArticle);

            return true;
        }

        // Supprimer un article par ID
        public async Task<bool> DeleteArticleAsync(int id)
        {
            await _client
                .Table<ArticleCourseDb>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();

            return true;
        }

        // Supprimer tous les articles achetés
        public async Task<bool> DeleteAchetesAsync()
        {
            await _client
                .Table<ArticleCourseDb>()
                .Filter("est_achete", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Delete();

            return true;
        }
    }

    // Modèle pour Supabase (avec attributs Postgrest)
    [Table("articles_courses")]
    public class ArticleCourseDb : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("nom")]
        public string? Nom { get; set; }

        [Column("quantite")]
        public string? Quantite { get; set; }

        [Column("unite")]
        public string? Unite { get; set; }

        [Column("est_achete")]
        public bool EstAchete { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}