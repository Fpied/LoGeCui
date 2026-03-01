using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using Supabase;
using System;
using System.Collections.Generic;
using System.Text;
using Supabase.Postgrest;

namespace LoGeCuiShared.Functions
{
    internal static class ArticlesCoursesFunction
    {
        public static async Task<List<ArticleCourse>> GetArticlesAsync(Supabase.Postgrest.Client client, Guid userGuid)
        {

            var response = await client
                .Table<ArticleCourseDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Get();

            return response.Models.Select(db => new ArticleCourse
            {
                Id = db.Id,
                Nom = db.Nom ?? "",
                Quantite = db.Quantite ?? "",
                Unite = db.Unite ?? "",
                EstAchete = db.EstAchete,
                // IMPORTANT: si ton modèle ArticleCourse.UserId est Guid (non nullable),
                // on protège avec ?? Guid.Empty
                UserId = db.UserId ?? Guid.Empty
            }).ToList();
        }

        public static async Task<ArticleCourse?> AddArticleAsync(Supabase.Postgrest.Client client, ArticleCourse article, Guid userGuid)
        {

            var dbArticle = new ArticleCourseDb
            {
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            var response = await client.Table<ArticleCourseDb>().Insert(dbArticle);
            var inserted = response.Models.FirstOrDefault();
            if (inserted == null) return null;

            return new ArticleCourse
            {
                Id = inserted.Id,
                Nom = inserted.Nom ?? "",
                Quantite = inserted.Quantite ?? "",
                Unite = inserted.Unite ?? "",
                EstAchete = inserted.EstAchete,
                UserId = inserted.UserId ?? Guid.Empty
            };
        }

        public static async Task<bool> UpdateArticleAsync(int id, ArticleCourse article, Supabase.Postgrest.Client client, Guid userGuid)
        {

            var dbArticle = new ArticleCourseDb
            {
                Id = id,
                Nom = article.Nom,
                Quantite = article.Quantite,
                Unite = article.Unite,
                EstAchete = article.EstAchete,
                UserId = (Guid?)userGuid // ✅ Guid -> Guid?
            };

            await client.Table<ArticleCourseDb>().Update(dbArticle);
            return true;
        }

        public static async Task<bool> DeleteArticleAsync(Supabase.Postgrest.Client client, int id, Guid userGuid)
        {

            // Sécurise par user_id (sinon RLS peut bloquer ou pire, toucher autre chose si policy permissive)
            await client.Table<ArticleCourseDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();

            return true;
        }

        public static async Task<bool> DeleteAchetesAsync(Supabase.Postgrest.Client client, Guid userGuid)
        {

            await client.Table<ArticleCourseDb>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userGuid.ToString())
                .Filter("est_achete", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Delete();

            return true;
        }
    }
}
