using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoGeCuiMobile.Resources.Lang;
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
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
        }
    }

    private async void OnOpenListeCoursesClicked(object sender, EventArgs e)
    {
        try
        {
            var app = Application.Current as App;
            if (app?.ListeCoursesSupabaseService == null)
            {
                await DisplayAlert(
                    LocalizationResourceManager.Instance["ErrorTitle"],
                    LocalizationResourceManager.Instance["ServiceUnavailable"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );
                return;
            }

            await Shell.Current.GoToAsync(nameof(ListeCoursesPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
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
                LocalizationResourceManager.Instance["RandomMenu_SendConfirmTitle"],
                LocalizationResourceManager.Instance["RandomMenu_SendConfirmBody"] + "\n- " + string.Join("\n- ", _lastMissing),
                LocalizationResourceManager.Instance["Dialog_Yes"],
                LocalizationResourceManager.Instance["Dialog_No"]
            );

            if (!confirm)
                return;

            await app.ListeCoursesSupabaseService.AddMissingAsync(
                app.CurrentUserId.Value,
                _lastMissing);

            SendMissingButton.IsEnabled = false;
            SendMissingButton.Text = LocalizationResourceManager.Instance["RandomMenu_SentButton"];
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                LocalizationResourceManager.Instance["ErrorTitle"],
                ex.Message,
                LocalizationResourceManager.Instance["Dialog_Ok"]
            );
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
        SendMissingButton.Text = LocalizationResourceManager.Instance["RandomMenu_SendMissingToShopping"];
        _lastMissing = new List<string>();
        _lastIngredientsKnown = false;
        _lastUserId = app.CurrentUserId;

        if (app.CurrentUserId == null ||
            app.RestClient == null ||
            app.RecipesService == null ||
            app.IngredientsService == null ||
            app.ListeCoursesSupabaseService == null)
        {
            ResultLabel.Text = LocalizationResourceManager.Instance["LoginRequired"];
            return;
        }

        var userId = app.CurrentUserId.Value;
        ResultLabel.Text = LocalizationResourceManager.Instance["RandomMenu_Generating"];

        // 1) Recettes utilisateur
        var recettes = await app.RecipesService.GetMyRecettesAsync();

        if (recettes == null || recettes.Count == 0)
        {
            ResultLabel.Text = LocalizationResourceManager.Instance["RandomMenu_NoRecipes"];
            return;
        }

        // 2) Par type (enum)
        var entrees = recettes.Where(r => r.Type == TypePlat.Entree).ToList();
        var plats = recettes.Where(r => r.Type == TypePlat.Plat).ToList();
        var desserts = recettes.Where(r => r.Type == TypePlat.Dessert).ToList();

        if (entrees.Count == 0 || plats.Count == 0 || desserts.Count == 0)
        {
            ResultLabel.Text =
                LocalizationResourceManager.Instance["RandomMenu_CannotGenerate"] + "\n\n" +
                string.Format(LocalizationResourceManager.Instance["RandomMenu_CountsFormat"], entrees.Count, plats.Count, desserts.Count) + "\n\n" +
                LocalizationResourceManager.Instance["RandomMenu_CheckCategories"];
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

        sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_GeneratedHeader"]);
        sb.AppendLine($"- {entree.TypeTexte} : {entree.Nom}");
        sb.AppendLine($"- {plat.TypeTexte} : {plat.Nom}");
        sb.AppendLine($"- {dessert.TypeTexte} : {dessert.Nom}");
        sb.AppendLine();

        void AppendRecetteBlock(Recette r, List<string> ingredients, List<string> missing, bool hasData)
        {
            sb.AppendLine($"{r.TypeTexte} — {r.Nom}");

            if (!hasData)
            {
                sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_NoIngredientsForRecipe"]);
                sb.AppendLine();
                return;
            }

            sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_IngredientsHeader"]);
            foreach (var i in ingredients)
                sb.AppendLine("- " + i);

            if (missing.Count == 0)
            {
                sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_MissingNone"]);
            }
            else
            {
                sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_MissingHeader"]);
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
            sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_CannotComputeMissing"]);
            SendMissingButton.IsVisible = false;
            SendMissingButton.IsEnabled = false;
        }
        else if (_lastMissing.Count == 0)
        {
            sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_AllAvailable"]);
            SendMissingButton.IsVisible = false;
            SendMissingButton.IsEnabled = false;
        }
        else
        {
            sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_GlobalMissingHeader"]);
            foreach (var m in _lastMissing)
                sb.AppendLine("- " + m);

            SendMissingButton.IsVisible = true;
            SendMissingButton.IsEnabled = true;
        }

        ResultLabel.Text = sb.ToString();
    }
}
