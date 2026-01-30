using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiMobile.Services;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;

namespace LoGeCuiMobile.Pages
{
    public partial class AjouterRecettePage : ContentPage
    {
        public AjouterRecettePage()
        {
            InitializeComponent();

            // Valeurs conformes à ta DB Supabase (Entree / Plat / Dessert)
            CategoriePicker.ItemsSource = new List<string> { "Entree", "Plat", "Dessert" };
            CategoriePicker.SelectedIndex = 1; // Plat par défaut
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var app = (App)Application.Current;

                if (app.CurrentUserId == null || app.RecipesService == null)
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_LoginRequired"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                if (app.RecetteIngredientsService == null)
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_ServiceNotReady"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                if (string.IsNullOrWhiteSpace(NomEntry.Text))
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_NameRequired"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                var categorie = (CategoriePicker.SelectedItem as string) ?? "Plat";
                int temps = int.TryParse(TempsEntry.Text, out var t) ? t : 0;

                // ExternalId stable pour retrouver l'id après upsert
                var externalId = Guid.NewGuid().ToString("N");

                var recette = new Recette
                {
                    Nom = NomEntry.Text.Trim(),
                    CategorieDb = categorie,
                    TempsPreparation = temps,
                    Difficulte = 1,
                    Instructions = InstructionsEditor.Text ?? "",
                    IsFavorite = false,
                    ExternalId = externalId
                };

                // 1) Upsert recette (table recettes)
                await app.RecipesService.UpsertRecetteAsync(app.CurrentUserId.Value, recette);

                // 2) Récupérer l'id réel de la recette (UUID en DB) via ExternalId
                var recetteId = await app.RecipesService.GetRecetteIdByExternalIdAsync(externalId);
                if (recetteId == null)
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_IdNotFound"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                // 3) Parser les ingrédients (1 par ligne)
                var lines = (IngredientsEditor.Text ?? "")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .ToList();

                var items = new List<(string nom, string? quantite, string? unite)>();

                foreach (var line in lines)
                {
                    // Si vous ne gérez pas quantite/unite maintenant, gardez simple:
                    // tout va dans ingredient_nom
                    items.Add((line, null, null));
                }

                // 4) Écriture dans recette_ingredients (delete + insert)
                await app.RecetteIngredientsService.ReplaceForRecetteAsync(recetteId.Value, items);

                await DisplayAlert(
                    LocalizationResourceManager.Instance["SuccessTitle"],
                    LocalizationResourceManager.Instance["Recipes_Add_Saved"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                await DisplayAlert(
                    LocalizationResourceManager.Instance["ErrorTitle"],
                    ex.Message,
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );
            }
        }

        // Transforme le texte du multi-line editor en liste (nom, quantite, unite)
        private static List<(string nom, string? quantite, string? unite)> ParseIngredientsEditor(string? text)
        {
            text ??= "";

            return text
                .Replace("\r\n", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(ParseIngredientLine)
                .Where(x => !string.IsNullOrWhiteSpace(x.nom))
                .ToList();
        }

        // Supporte:
        // - "Tomate"
        // - "200 g Farine"
        // - "2 pcs Tomate"
        // - "Farine - 200 g" (si tu colles depuis ToString)
        private static (string nom, string? quantite, string? unite) ParseIngredientLine(string line)
        {
            // "Nom - Quantite Unite"
            var dashSplit = line.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (dashSplit.Length >= 2)
            {
                var nomPart = dashSplit[0];
                var qtePart = dashSplit[1];

                var parts = qtePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return (nomPart, parts[0], string.Join(" ", parts.Skip(1)));

                return (nomPart, qtePart, null);
            }

            // "200 g Farine"
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3 && char.IsDigit(tokens[0][0]))
            {
                var quantite = tokens[0];
                var unite = tokens[1];
                var nom = string.Join(" ", tokens.Skip(2));
                return (nom, quantite, unite);
            }

            // Sinon : juste le nom
            return (line, null, null);
        }

        private async void OnScanClicked(object sender, EventArgs e)
        {
            try
            {
                // Permission caméra (Android 6+)
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["PermissionTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_CameraPermission"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                // Capture supportée ?
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_CaptureNotSupported"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = LocalizationResourceManager.Instance["Recipes_Add_ScanTitle"]
                });

                if (photo == null)
                    return;

                byte[] bytes;
                using (var stream = await photo.OpenReadAsync())
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                Debug.WriteLine($"Image originale: {bytes.Length / 1024} KB");

                // Compression pour rester sous la limite OCR.Space (souvent 1024 KB)
                bytes = ImageCompressionHelper.CompressJpeg(bytes, maxWidth: 1600, jpegQuality: 75);
                Debug.WriteLine($"Image compressée: {bytes.Length / 1024} KB");

                // Si encore trop gros, on retente plus agressif automatiquement
                if (bytes.Length > 1024 * 1024)
                {
                    bytes = ImageCompressionHelper.CompressJpeg(bytes, maxWidth: 1200, jpegQuality: 65);
                    Debug.WriteLine($"Image recompressée: {bytes.Length / 1024} KB");
                }

                if (bytes.Length > 1024 * 1024)
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_ImageTooLarge"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                // OCR
                var ocrKey = LoGeCuiShared.Services.ConfigurationHelper.GetOcrApiKey();
                var ocr = new OcrService(ocrKey);

                var fullText = await ocr.ExtractTextFromImageAsync(bytes, photo.FileName);

                if (string.IsNullOrWhiteSpace(fullText))
                {
                    await DisplayAlert(
                        LocalizationResourceManager.Instance["OcrTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_NoTextDetected"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]
                    );
                    return;
                }

                fullText = NormalizeOcrText(fullText);

                var (ingredients, instructions) = SplitRecipeText(fullText);

                if (!string.IsNullOrWhiteSpace(instructions))
                    InstructionsEditor.Text = instructions.Trim();

                if (!string.IsNullOrWhiteSpace(ingredients))
                    IngredientsEditor.Text = ingredients.Trim();

                if (string.IsNullOrWhiteSpace(NomEntry.Text))
                {
                    var title = GuessTitle(fullText);
                    if (!string.IsNullOrWhiteSpace(title))
                        NomEntry.Text = title;
                }

                await DisplayAlert(
                    LocalizationResourceManager.Instance["SuccessTitle"],
                    LocalizationResourceManager.Instance["Recipes_Add_OcrSuccess"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );
            }
            catch (Exception ex)
            {
                var msg = ex.Message ?? LocalizationResourceManager.Instance["UnknownError"];

                if (msg.Contains("File failed validation", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("maximum permissible file size", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("1024kb", StringComparison.OrdinalIgnoreCase))
                {
                    msg = LocalizationResourceManager.Instance["Recipes_Add_ImageTooLarge"];
                }

                await DisplayAlert(
                    LocalizationResourceManager.Instance["ErrorTitle"],
                    msg,
                    LocalizationResourceManager.Instance["Dialog_Ok"]
                );
            }
        }

        private static string NormalizeOcrText(string s)
        {
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            while (s.Contains("\n\n\n")) s = s.Replace("\n\n\n", "\n\n");
            return s.Trim();
        }

        private static (string ingredients, string instructions) SplitRecipeText(string text)
        {
            var lower = text.ToLowerInvariant();

            int idxIng = IndexOfAny(lower, "ingrédients", "ingredients");
            int idxPrep = IndexOfAny(lower, "préparation", "preparation", "instructions", "réalisation", "realisation", "étapes", "etapes");

            if (idxIng >= 0 && idxPrep > idxIng)
            {
                var ingPart = text.Substring(idxIng, idxPrep - idxIng);
                var prepPart = text.Substring(idxPrep);

                ingPart = RemoveHeaderLine(ingPart);
                prepPart = RemoveHeaderLine(prepPart);

                return (CleanupListLike(ingPart), CleanupParagraph(prepPart));
            }

            if (idxPrep >= 0)
            {
                var before = text.Substring(0, idxPrep);
                var after = text.Substring(idxPrep);

                after = RemoveHeaderLine(after);
                return (CleanupListLike(before), CleanupParagraph(after));
            }

            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

            var ingLines = new List<string>();
            var instrLines = new List<string>();

            bool inInstructions = false;
            foreach (var line in lines)
            {
                if (!inInstructions && LooksLikeStep(line))
                    inInstructions = true;

                if (!inInstructions && LooksLikeIngredientLine(line))
                    ingLines.Add(line);
                else
                    instrLines.Add(line);
            }

            return (string.Join("\n", ingLines), string.Join("\n", instrLines));
        }

        private static int IndexOfAny(string haystack, params string[] needles)
        {
            int best = -1;
            foreach (var n in needles)
            {
                var i = haystack.IndexOf(n, StringComparison.OrdinalIgnoreCase);
                if (i >= 0 && (best < 0 || i < best)) best = i;
            }
            return best;
        }

        private static string RemoveHeaderLine(string block)
        {
            var lines = block.Replace("\r\n", "\n").Split('\n').ToList();
            if (lines.Count == 0) return block;
            lines.RemoveAt(0);
            return string.Join("\n", lines).Trim();
        }

        private static string CleanupListLike(string s)
        {
            var lines = s.Replace("\r\n", "\n").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Select(l => l.TrimStart('-', '•', '*', '–', '—', '·', ' '))
                .Where(l => l.Length > 0);

            return string.Join("\n", lines);
        }

        private static string CleanupParagraph(string s)
        {
            var lines = s.Replace("\r\n", "\n").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0);

            return string.Join("\n", lines);
        }

        private static bool LooksLikeIngredientLine(string line)
        {
            if (line.StartsWith("-") || line.StartsWith("•") || line.StartsWith("*")) return true;

            var l = line.ToLowerInvariant();
            string[] units = { " g", "kg", " ml", "cl", " l", "càs", "cas", "càc", "cac", "cuillère", "pincée", "tranche", "sachet" };
            if (units.Any(u => l.Contains(u))) return true;

            if (char.IsDigit(line.FirstOrDefault())) return true;

            return false;
        }

        private static bool LooksLikeStep(string line)
        {
            var l = line.Trim();
            if (l.StartsWith("1)") || l.StartsWith("1.") ||
                l.StartsWith("Étape", StringComparison.OrdinalIgnoreCase) ||
                l.StartsWith("Etape", StringComparison.OrdinalIgnoreCase))
                return true;

            if (l.Length >= 2 && char.IsDigit(l[0]) && (l[1] == ')' || l[1] == '.'))
                return true;

            return false;
        }

        private static string GuessTitle(string text)
        {
            var first = text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "";
            if (first.Length >= 3 && first.Length <= 50) return first;
            return "";
        }
    }
}
