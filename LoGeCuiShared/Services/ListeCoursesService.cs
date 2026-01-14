using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using LoGeCuiShared.Models;

namespace LoGeCuiShared.Services
{
    public class ListeCoursesService
    {
        private readonly string _cheminFichier;

        public ListeCoursesService(string cheminFichier)
        {
            _cheminFichier = cheminFichier;
        }

        public List<ArticleCourse> ChargerListeCourses()
        {
            try
            {
                if (!File.Exists(_cheminFichier))
                {
                    return new List<ArticleCourse>();
                }

                string json = File.ReadAllText(_cheminFichier);
                var liste = JsonSerializer.Deserialize<List<ArticleCourse>>(json);
                return liste ?? new List<ArticleCourse>();
            }
            catch (Exception)
            {
                return new List<ArticleCourse>();
            }
        }

        public void SauvegarderListeCourses(List<ArticleCourse> articles)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(articles, options);

                // Créer le dossier si nécessaire
                var dossier = Path.GetDirectoryName(_cheminFichier);
                if (!string.IsNullOrEmpty(dossier) && !Directory.Exists(dossier))
                {
                    Directory.CreateDirectory(dossier);
                }

                File.WriteAllText(_cheminFichier, json);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
