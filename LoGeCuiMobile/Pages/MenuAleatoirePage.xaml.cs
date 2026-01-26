using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoGeCuiShared.Models;
using Microsoft.Maui.Controls;

namespace LoGeCuiMobile.Pages;

public partial class MenuAleatoirePage : ContentPage
{
    private static readonly Random _random = new();

    private List<string> _lastMissing = new();
    private Guid? _lastUserId;
    private bool _lastIngredientsKnown = false;

    public MenuAleatoirePage()
    {
        InitializeComponent();
        SendMissingButton.IsVisible = false;
        SendMissingButton.IsEnabled = false;
    }

    // ------------------ UTIL ------------------

    private static string Norm(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        var normalized = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);

        return sb.ToString();
    }

    private sealed class RecetteIngredientRow
    {
        public string? ingredient_nom { get; set; }
    }

    private async Task<List<string>> GetRecetteIngredientsAsync(App app, Guid recetteId)
    {
        if (app.RestClient == null)
            return new List<string>();

        // Schéma confirmé : recette_ingredients(recette_id, ingredient_nom, quantite, unite)
        var q = $"recette_ingredients?select=ingredient_nom&recette_id=eq.{recetteId}";

        var rows = await app.RestClient.GetAsync<List<RecetteIngredientRow>>(q)
                   ?? new List<RecetteIngredientRow>();

        return rows
            .Select(r => r.ingredient_nom ?? "")
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();
    }

    private async Task<List<string>> GetUserIngredientsAsync(App app, Guid userId)
    {
        if (app.IngredientsService == null)
            return new List<string>();

        var rows = await app.IngredientsService.GetIngredientsAsync(userId)
                   ?? new List<Ingredient>();

        // On ne considère que ceux disponibles
        return rows
            .Where(i => i.EstDisponible)
            .Select(i => i.Nom ?? "")
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();
    }

    // ------------------ EVENTS ------------------

    private async void OnMenuAleatoireClicked(object sender, EventArgs e)
    {
        try
        {
            await RunMenuAleatoireAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    private async void OnOpenListeCoursesClicked(object sender, EventArgs e)
    {
        try
        {
            var app = Application.Current as App;
            if (app?.ListeCoursesSupabaseService == null)
            {
                await DisplayAlert("Erreur", "Service indisponible.", "OK");
                return;
            }

            await Shell.Current.GoToAsync(nameof(ListeCoursesPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    private async void OnSendMissingClicked(object sender, EventArgs e)
    {
        try
        {
            var app = Application.Current as App;

            if (app?.ListeCoursesSupabaseService == null || app.CurrentUserId == null)
                return;

            if (!_lastIngredientsKnown || _lastMissing.Count == 0)
                return;

            bool confirm = await DisplayAlert(
                "Envoyer ?",
                "Il manque :\n- " + string.Join("\n- ", _lastMissing),
                "Oui",
                "Non");

            if (!confirm)
                return;

            await app.ListeCoursesSupabaseService.AddMissingAsync(
                app.CurrentUserId.Value,
                _lastMissing);

            SendMissingButton.IsEnabled = false;
            SendMissingButton.Text = "🛒 Ingrédients envoyés";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    // ------------------ LOGIQUE ------------------

    private async Task RunMenuAleatoireAsync()
    {
        var app = Application.Current as App;
        if (app == null)
            return;

        // Reset UI
        SendMissingButton.IsVisible = false;
        SendMissingButton.IsEnabled = false;
        SendMissingButton.Text = "🛒 Envoyer les ingrédients manquants dans la liste de courses";
        _lastMissing = new List<string>();
        _lastIngredientsKnown = false;
        _lastUserId = app.CurrentUserId;

        if (app.CurrentUserId == null ||
            app.RestClient == null ||
            app.RecipesService == null ||
            app.IngredientsService == null ||
            app.ListeCoursesSupabaseService == null)
        {
            ResultLabel.Text = "Vous devez être connecté.";
            return;
        }

        var userId = app.CurrentUserId.Value;
        ResultLabel.Text = "Génération du menu (Entrée / Plat / Dessert)…";

        // 1) Recettes utilisateur
        var recettes = await app.RecipesService.GetRecettesAsync(userId);
        if (recettes == null || recettes.Count == 0)
        {
            ResultLabel.Text = "Aucune recette disponible.";
            return;
        }

        // 2) Par type (enum)
        var entrees = recettes.Where(r => r.Type == TypePlat.Entree).ToList();
        var plats = recettes.Where(r => r.Type == TypePlat.Plat).ToList();
        var desserts = recettes.Where(r => r.Type == TypePlat.Dessert).ToList();

        if (entrees.Count == 0 || plats.Count == 0 || desserts.Count == 0)
        {
            ResultLabel.Text =
                "Impossible de générer un menu Entrée/Plat/Dessert.\n\n" +
                $"Entrées: {entrees.Count}\nPlats: {plats.Count}\nDesserts: {desserts.Count}\n\n" +
                "Vérifie que tes recettes ont bien 'categorie' = Entree / Plat / Dessert en DB.";
            return;
        }

        // 3) Tirage aléatoire
        var entree = entrees[_random.Next(entrees.Count)];
        var plat = plats[_random.Next(plats.Count)];
        var dessert = desserts[_random.Next(desserts.Count)];

        // 4) Ingrédients utilisateur
        var userIngredients = await GetUserIngredientsAsync(app, userId);
        var userSet = userIngredients
            .Select(Norm)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();

        // 5) Analyse recette (avec garde-fou si pas d'ingrédients en DB)
        async Task<(List<string> ingredients, List<string> missing, bool hasData)> AnalyzeAsync(Recette r)
        {
            var ingredients = await GetRecetteIngredientsAsync(app, r.Id);
            var distinct = ingredients
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (distinct.Count == 0)
                return (distinct, new List<string>(), false);

            var missing = distinct
                .Where(i => !userSet.Contains(Norm(i)))
                .Distinct()
                .ToList();

            return (distinct, missing, true);
        }

        var entreeA = await AnalyzeAsync(entree);
        var platA = await AnalyzeAsync(plat);
        var dessertA = await AnalyzeAsync(dessert);

        // 6) Agrégation des manquants (uniquement si on a des données)
        var allHaveData = entreeA.hasData && platA.hasData && dessertA.hasData;

        if (allHaveData)
        {
            _lastMissing = entreeA.missing
                .Concat(platA.missing)
                .Concat(dessertA.missing)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();
            _lastIngredientsKnown = true;
        }
        else
        {
            _lastMissing = new List<string>();
            _lastIngredientsKnown = false;
        }

        // 7) UI
        var sb = new StringBuilder();

        sb.AppendLine("Menu généré :");
        sb.AppendLine($"- {entree.TypeTexte} : {entree.Nom}");
        sb.AppendLine($"- {plat.TypeTexte} : {plat.Nom}");
        sb.AppendLine($"- {dessert.TypeTexte} : {dessert.Nom}");
        sb.AppendLine();

        void AppendRecetteBlock(Recette r, List<string> ingredients, List<string> missing, bool hasData)
        {
            sb.AppendLine($"{r.TypeTexte} — {r.Nom}");

            if (!hasData)
            {
                sb.AppendLine("Ingrédients : (aucun ingrédient enregistré pour cette recette)");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("Ingrédients :");
            foreach (var i in ingredients)
                sb.AppendLine("- " + i);

            if (missing.Count == 0)
            {
                sb.AppendLine("Manquants : aucun");
            }
            else
            {
                sb.AppendLine("Manquants :");
                foreach (var m in missing)
                    sb.AppendLine("- " + m);
            }

            sb.AppendLine();
        }

        AppendRecetteBlock(entree, entreeA.ingredients, entreeA.missing, entreeA.hasData);
        AppendRecetteBlock(plat, platA.ingredients, platA.missing, platA.hasData);
        AppendRecetteBlock(dessert, dessertA.ingredients, dessertA.missing, dessertA.hasData);

        if (!allHaveData)
        {
            sb.AppendLine("Impossible de calculer les ingrédients manquants : certaines recettes n’ont pas d’ingrédients enregistrés.");
            SendMissingButton.IsVisible = false;
            SendMissingButton.IsEnabled = false;
        }
        else if (_lastMissing.Count == 0)
        {
            sb.AppendLine("Tout est disponible. Aucun ingrédient manquant.");
            SendMissingButton.IsVisible = false;
            SendMissingButton.IsEnabled = false;
        }
        else
        {
            sb.AppendLine("Liste globale des ingrédients manquants :");
            foreach (var m in _lastMissing)
                sb.AppendLine("- " + m);

            SendMissingButton.IsVisible = true;
            SendMissingButton.IsEnabled = true;
        }

        ResultLabel.Text = sb.ToString();
    }
}
