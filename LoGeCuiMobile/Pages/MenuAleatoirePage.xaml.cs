using CommunityToolkit.Maui.Views;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiShared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoGeCuiMobile.Pages;

public partial class MenuAleatoirePage : ContentPage
{
    private static readonly Random _random = new();
    public record MenuOptionsResult(bool Entree, bool Plat, bool Dessert);


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

        // recette_ingredients(recette_id, ingredient_nom, quantite, unite)
        var q = $"recette_ingredients?select=ingredient_nom&recette_id=eq.{recetteId}";

        var rows = await app.RestClient.GetAsync<List<RecetteIngredientRow>>(q)
                   ?? new List<RecetteIngredientRow>();

        return rows
            .Select(r => r.ingredient_nom ?? "")
            .Select(s => s.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<string>> GetUserIngredientsAsync(App app, Guid userId)
    {
        if (app.IngredientsService == null)
            return new List<string>();

        var rows = await app.IngredientsService.GetIngredientsAsync(userId)
                   ?? new List<Ingredient>();

        return rows
            .Where(i => i.EstDisponible)
            .Select(i => (i.Nom ?? "").Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Recette? PickRandom(List<Recette> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[_random.Next(list.Count)];
    }

    // ------------------ EVENTS ------------------

    private async void OnMenuAleatoireClicked(object sender, EventArgs e)
    {
        try
        {
            // Choix initial : on démarre tout coché
            bool wantEntree = true;
            bool wantPlat = true;
            bool wantDessert = true;

            while (true)
            {
                string entreeTxt = wantEntree ? "✅ Entrée" : "⬜ Entrée";
                string platTxt = wantPlat ? "✅ Plat" : "⬜ Plat";
                string dessertTxt = wantDessert ? "✅ Dessert" : "⬜ Dessert";

                var choice = await DisplayActionSheet(
                    "Choisis les catégories",
                    "Annuler",
                    "Générer",
                    entreeTxt,
                    platTxt,
                    dessertTxt
                );

                if (choice == "Annuler")
                    return;

                if (choice == "Générer")
                {
                    if (!wantEntree && !wantPlat && !wantDessert)
                    {
                        await DisplayAlert("Info", "Coche au moins une catégorie.", "OK");
                        continue;
                    }

                    var opts = new MenuOptionsResult(wantEntree, wantPlat, wantDessert);
                    await RunMenuAleatoireAsync(opts);
                    return;
                }

                if (choice == entreeTxt) wantEntree = !wantEntree;
                else if (choice == platTxt) wantPlat = !wantPlat;
                else if (choice == dessertTxt) wantDessert = !wantDessert;
            }
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

            // ✅ FORCER la récupération de la liste si null
            if (app.CurrentShoppingListId == null && app.RestClient != null)
            {
                try
                {
                    var url = $"shopping_lists?select=id&owner_user_id=eq.{app.CurrentUserId.Value}&limit=1";
                    var result = await app.RestClient.GetAsync<List<Dictionary<string, object>>>(url);

                    if (result != null && result.Count > 0 && result[0].ContainsKey("id"))
                    {
                        var idObj = result[0]["id"];
                        if (idObj != null)
                        {
                            app.SetCurrentShoppingListId(Guid.Parse(idObj.ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Debug", $"Erreur récup liste: {ex.Message}", "OK");
                }
            }

            // ✅ list_id obligatoire dans articles_courses
            var listId = app.CurrentShoppingListId;
            if (listId == null || listId.Value == Guid.Empty)
            {
                await DisplayAlert(
                    LocalizationResourceManager.Instance["ErrorTitle"],
                    "Aucune liste active. Crée ou rejoins une liste avant d'envoyer les ingrédients.",
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );
                return;
            }

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
                _lastMissing,
                listId.Value);

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


    private Guid? GetActiveListIdOrNull()
    {
        var app = (App)Application.Current;
        if (app.CurrentShoppingListId == null || app.CurrentShoppingListId == Guid.Empty)
            return null;

        return app.CurrentShoppingListId.Value;
    }

    // ------------------ LOGIQUE ------------------

    private async Task RunMenuAleatoireAsync(MenuOptionsResult opts)
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
        // (tu utilises GetMyRecettesAsync dans ton code : je le garde)
        var recettes = await app.RecipesService.GetMyRecettesAsync(app.CurrentUserId!.Value, null);



        if (recettes == null || recettes.Count == 0)
        {
            ResultLabel.Text = LocalizationResourceManager.Instance["RandomMenu_NoRecipes"];
            return;
        }

        // 2) Listes par type (si l'option est cochée)
        var entrees = opts.Entree ? recettes.Where(r => r.Type == TypePlat.Entree).ToList() : new List<Recette>();
        var plats = opts.Plat ? recettes.Where(r => r.Type == TypePlat.Plat).ToList() : new List<Recette>();
        var desserts = opts.Dessert ? recettes.Where(r => r.Type == TypePlat.Dessert).ToList() : new List<Recette>();

        // 3) Tirage (uniquement si demandé)
        var entree = opts.Entree ? PickRandom(entrees) : null;
        var plat = opts.Plat ? PickRandom(plats) : null;
        var dessert = opts.Dessert ? PickRandom(desserts) : null;

        // Si une catégorie cochée n'a aucune recette
        if ((opts.Entree && entree == null) || (opts.Plat && plat == null) || (opts.Dessert && dessert == null))
        {
            var sbErr = new StringBuilder();
            sbErr.AppendLine(LocalizationResourceManager.Instance["RandomMenu_CannotGenerate"]);
            sbErr.AppendLine();

            if (opts.Entree && entree == null) sbErr.AppendLine("• Aucune recette de type Entrée");
            if (opts.Plat && plat == null) sbErr.AppendLine("• Aucune recette de type Plat");
            if (opts.Dessert && dessert == null) sbErr.AppendLine("• Aucune recette de type Dessert");

            sbErr.AppendLine();
            sbErr.AppendLine(LocalizationResourceManager.Instance["RandomMenu_CheckCategories"]);

            ResultLabel.Text = sbErr.ToString();
            return;
        }

        // 4) Ingrédients utilisateur disponibles
        var userIngredients = await GetUserIngredientsAsync(app, userId);
        var userSet = userIngredients
            .Select(Norm)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();

        // 5) Analyse d'une recette
        async Task<(List<string> ingredients, List<string> missing, bool hasData)> AnalyzeAsync(Recette r)
        {
            var ingredients = await GetRecetteIngredientsAsync(app, r.Id);
            var distinct = ingredients
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinct.Count == 0)
                return (distinct, new List<string>(), false);

            var missing = distinct
                .Where(i => !userSet.Contains(Norm(i)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (distinct, missing, true);
        }

        // 6) Analyse seulement des recettes réellement tirées
        var chosen = new List<Recette>();
        if (entree != null) chosen.Add(entree);
        if (plat != null) chosen.Add(plat);
        if (dessert != null) chosen.Add(dessert);

        var analyses = new Dictionary<Guid, (List<string> ingredients, List<string> missing, bool hasData)>();
        foreach (var r in chosen)
            analyses[r.Id] = await AnalyzeAsync(r);

        // 7) Agrégation manquants (uniquement si on a des données pour toutes les recettes choisies)
        var allHaveData = chosen.All(r => analyses[r.Id].hasData);

        if (allHaveData)
        {
            _lastMissing = chosen
                .SelectMany(r => analyses[r.Id].missing)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _lastIngredientsKnown = true;
        }
        else
        {
            _lastMissing = new List<string>();
            _lastIngredientsKnown = false;
        }

        // 8) UI texte
        var sb = new StringBuilder();

        sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_GeneratedHeader"]);

        if (entree != null) sb.AppendLine($"- {entree.TypeTexte} : {entree.Nom}");
        if (plat != null) sb.AppendLine($"- {plat.TypeTexte} : {plat.Nom}");
        if (dessert != null) sb.AppendLine($"- {dessert.TypeTexte} : {dessert.Nom}");
        sb.AppendLine();

        void AppendRecetteBlock(Recette r)
        {
            var a = analyses[r.Id];

            sb.AppendLine($"{r.TypeTexte} — {r.Nom}");

            if (!a.hasData)
            {
                sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_NoIngredientsForRecipe"]);
                sb.AppendLine();
                return;
            }

            sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_IngredientsHeader"]);
            foreach (var i in a.ingredients)
                sb.AppendLine("- " + i);

            if (a.missing.Count == 0)
            {
                sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_MissingNone"]);
            }
            else
            {
                sb.AppendLine(LocalizationResourceManager.Instance["RandomMenu_MissingHeader"]);
                foreach (var m in a.missing)
                    sb.AppendLine("- " + m);
            }

            sb.AppendLine();
        }

        foreach (var r in chosen)
            AppendRecetteBlock(r);

        // 9) Bouton “envoyer manquants”
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
