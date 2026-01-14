using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LoGeCuiShared.Models;

namespace LoGeCui.Services
{
    public class RecetteService
    {
        private readonly string _cheminFichier;

        public RecetteService()
        {
            string dossier = Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
             "LoGeCui"
            );
            Directory.CreateDirectory(dossier);
            _cheminFichier = Path.Combine(dossier, "recettes.json");
        }

        public List<Recette> ChargerRecettes()
        {
            try
            {
                if (!File.Exists(_cheminFichier))
                {
                    return new List<Recette>();
                }

                string json = File.ReadAllText(_cheminFichier);
                var recettes = JsonSerializer.Deserialize<List<Recette>>(json);
                return recettes ?? new List<Recette>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du chargement des recettes : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return new List<Recette>();
            }
        }

        public void SauvegarderRecettes(List<Recette> recettes)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(recettes, options);
                File.WriteAllText(_cheminFichier, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la sauvegarde des recettes : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
