using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LoGeCuiShared.Models;
using System.Linq;

namespace LoGeCui.Services
{
    public class IngredientService
    {
        // Chemin du fichier de sauvegarde
        private readonly string _cheminFichier;
        private List<Ingredient> _ingredients = new List<Ingredient>();

        public IngredientService()
        {
            // Le fichier sera sauvegardé dans AppData de l'utilisateur
            string dossier = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LoGeCui"
            );

            // Créer le dossier s'il n'existe pas
            Directory.CreateDirectory(dossier);

            _cheminFichier = Path.Combine(dossier, "ingredients.json");
        }

        /// <summary>
        /// Charge les ingrédients depuis le fichier JSON
        /// </summary>
        public List<Ingredient> ChargerIngredients()
        {
            try
            {
                // Vérifier si le fichier existe
                if (!File.Exists(_cheminFichier))
                {
                    // Pas de fichier = liste vide
                    return new List<Ingredient>();
                }

                // Lire le contenu du fichier
                string json = File.ReadAllText(_cheminFichier);

                // Convertir le JSON en liste d'ingrédients
                var ingredients = JsonSerializer.Deserialize<List<Ingredient>>(json);

                return ingredients ?? new List<Ingredient>();
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message et retourner une liste vide
                System.Windows.MessageBox.Show(
                    $"Erreur lors du chargement des ingrédients : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                return new List<Ingredient>();
            }
        }

        public List<Ingredient> ObtenirTousLesIngredients()
        {
            return _ingredients.ToList();
        }

        public List<Ingredient> ObtenirIngredientsDisponibles()
        {
            return _ingredients.Where(i => i.EstDisponible).ToList();
        }

        /// <summary>
        /// Sauvegarde les ingrédients dans le fichier JSON
        /// </summary>
        public void SauvegarderIngredients(List<Ingredient> ingredients)
        {
            try
            {
                // Options pour rendre le JSON lisible (avec indentation)
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true  // Pour avoir un JSON "joli"
                };

                // Convertir la liste en JSON
                string json = JsonSerializer.Serialize(ingredients, options);

                // Écrire dans le fichier
                File.WriteAllText(_cheminFichier, json);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la sauvegarde des ingrédients : {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Retourne le chemin du fichier de sauvegarde
        /// </summary>
        public string ObtenirCheminFichier()
        {
            return _cheminFichier;
        }
    }
}
