using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LoGeCuiShared.Models;

namespace LoGeCui.Services
{
    public class ListeCoursesService
    {
        private readonly string _cheminFichier;

        public ListeCoursesService()
        {
            string dossier = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LoGeCui"
            );
                    Directory.CreateDirectory(dossier);
                    _cheminFichier = Path.Combine(dossier, "liste_courses.json");
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
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du chargement de la liste de courses : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return new List<ArticleCourse>();
            }
        }

        public void SauvegarderListeCourses(List<ArticleCourse> articles)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(articles, options);
                File.WriteAllText(_cheminFichier, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la sauvegarde de la liste de courses : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}