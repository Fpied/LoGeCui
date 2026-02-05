using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LoGeCuiMobile.Resources.Lang;
using LoGeCuiMobile.Services;
using LoGeCuiShared.Models;
using LoGeCuiShared.Services;
using LoGeCuiShared.Utils;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace LoGeCuiMobile.Pages
{
    public partial class AjouterRecettePage : ContentPage
    {
        private Recette? _editingRecette;
        private string? _photoLocalPath;

        private readonly SupabaseStorageService _storage = new SupabaseStorageService();

        // ✅ HttpClient réutilisé (évite ANR/sockets)
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        public AjouterRecettePage(Recette? recette = null)
        {
            InitializeComponent();

            CategoriePicker.ItemsSource = new List<string> { "Entree", "Plat", "Dessert" };
            CategoriePicker.SelectedIndex = 1;

            _editingRecette = recette;

            if (_editingRecette != null)
            {
                Title = "Modifier recette";
                _ = LoadRecetteForEditAsync();
            }
            else
            {
                Title = "Ajouter recette";
                SetPhotoPreview(null);
            }
        }

        private async System.Threading.Tasks.Task LoadRecetteForEditAsync()
        {
            try
            {
                if (_editingRecette == null)
                    return;

                NomEntry.Text = _editingRecette.Nom;
                TempsEntry.Text = _editingRecette.TempsPreparation.ToString(CultureInfo.InvariantCulture);
                InstructionsEditor.Text = _editingRecette.Instructions ?? "";

                var cat = string.IsNullOrWhiteSpace(_editingRecette.CategorieDb) ? "Plat" : _editingRecette.CategorieDb.Trim();
                if (CategoriePicker.ItemsSource is List<string> cats && cats.Contains(cat))
                    CategoriePicker.SelectedItem = cat;
                else
                    CategoriePicker.SelectedItem = "Plat";

                // Photo : local d'abord, sinon URL
                if (!string.IsNullOrWhiteSpace(_editingRecette.PhotoLocalPath) && File.Exists(_editingRecette.PhotoLocalPath))
                {
                    _photoLocalPath = _editingRecette.PhotoLocalPath;
                    SetPhotoPreview(_photoLocalPath);
                }
                else if (!string.IsNullOrWhiteSpace(_editingRecette.PhotoUrl))
                {
                    _photoLocalPath = null;
                    PhotoImage.Source = ImageSource.FromUri(new Uri(_editingRecette.PhotoUrl));
                    RemovePhotoButton.IsVisible = true;
                }
                else
                {
                    _photoLocalPath = null;
                    SetPhotoPreview(null);
                }

                // Ingrédients (depuis Supabase)
                var app = (App)Application.Current;
                if (app.RecetteIngredientsService != null && _editingRecette.Id != Guid.Empty)
                {
                    var items = await app.RecetteIngredientsService.GetForRecetteAsync(_editingRecette.Id);

                    IngredientsEditor.Text = (items.Count == 0)
                        ? ""
                        : string.Join("\n", items.Select(x =>
                            string.IsNullOrWhiteSpace(x.quantite) && string.IsNullOrWhiteSpace(x.unite)
                                ? x.nom
                                : $"{x.quantite} {x.unite} {x.nom}".Replace("  ", " ").Trim()
                        ));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"], ex.Message, LocalizationResourceManager.Instance["Dialog_Ok"]);
            }
        }

        // ---------- PHOTO ----------
        private async void OnPickPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var path = await PickOrTakeAsync(takePhoto: false);
                if (path == null) return;

                _photoLocalPath = path;
                SetPhotoPreview(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"], ex.Message, LocalizationResourceManager.Instance["Dialog_Ok"]);
            }
        }

        private async void OnTakePhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["PermissionTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_CameraPermission"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_CaptureNotSupported"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                var path = await PickOrTakeAsync(takePhoto: true);
                if (path == null) return;

                _photoLocalPath = path;
                SetPhotoPreview(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"], ex.Message, LocalizationResourceManager.Instance["Dialog_Ok"]);
            }
        }

        private void OnRemovePhotoClicked(object sender, EventArgs e)
        {
            _photoLocalPath = null;

            if (_editingRecette != null)
            {
                _editingRecette.PhotoLocalPath = null;
                _editingRecette.PhotoUrl = null;
            }

            SetPhotoPreview(null);
        }

        private void SetPhotoPreview(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                PhotoImage.Source = ImageSource.FromFile(path);
                RemovePhotoButton.IsVisible = true;
            }
            else
            {
                PhotoImage.Source = null;
                RemovePhotoButton.IsVisible = false;
            }
        }

        private async System.Threading.Tasks.Task<string?> PickOrTakeAsync(bool takePhoto)
        {
            FileResult? result = takePhoto
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();

            if (result == null) return null;

            var ext = Path.GetExtension(result.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            var fileName = $"recipe_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
            var destPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await using var src = await result.OpenReadAsync();
            await using var dest = File.OpenWrite(destPath);
            await src.CopyToAsync(dest);

            return destPath;
        }

        // ✅ UPDATE par ID (anti-duplicata)
        private static async System.Threading.Tasks.Task UpdateRecetteByIdAsync(
            string supabaseUrl,
            string supabaseKey,
            string accessToken,
            Guid recetteId,
            Guid userId,
            Recette recette)
        {
            supabaseUrl = supabaseUrl.TrimEnd('/');

            var url = $"{supabaseUrl}/rest/v1/recettes?id=eq.{recetteId}";

            using var req = new HttpRequestMessage(HttpMethod.Patch, url);
            req.Headers.Add("apikey", supabaseKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.Add("Prefer", "return=representation");

            var payload = new
            {
                owner_user_id = userId,
                external_id = recette.ExternalId,
                nom = recette.Nom,
                categorie = recette.CategorieDb,
                temps_minutes = recette.TempsPreparation,
                note = recette.Difficulte,
                is_favorite = recette.IsFavorite,
                instructions = recette.Instructions,
                photo_url = recette.PhotoUrl
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Update recette failed ({(int)res.StatusCode}): {body}");
        }

        // ---------- SAVE ----------
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var app = (App)Application.Current;

                if (app.CurrentUserId == null || app.RecipesService == null)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_LoginRequired"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                if (app.RecetteIngredientsService == null)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_ServiceNotReady"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NomEntry.Text))
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_NameRequired"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                var userId = app.CurrentUserId.Value;
                var categorie = (CategoriePicker.SelectedItem as string) ?? "Plat";
                int temps = int.TryParse(TempsEntry.Text, out var t) ? t : 0;

                bool isEdit = (_editingRecette != null && _editingRecette.Id != Guid.Empty);

                var recette = _editingRecette ?? new Recette();

                // ✅ ExternalId stable : seulement si nouveau
                if (string.IsNullOrWhiteSpace(recette.ExternalId))
                    recette.ExternalId = Guid.NewGuid().ToString("N");

                // ✅ owner
                recette.OwnerUserId = userId;

                // ✅ champs
                recette.Nom = NomEntry.Text.Trim();
                recette.CategorieDb = categorie;
                recette.TempsPreparation = temps;
                recette.Difficulte = 1;
                recette.Instructions = InstructionsEditor.Text ?? "";
                recette.IsFavorite = false;

                // photo locale
                recette.PhotoLocalPath = _photoLocalPath;

                // 1) Save recette (anti-duplicata)
                if (isEdit)
                {
                    if (string.IsNullOrWhiteSpace(app.CurrentAccessToken))
                    {
                        await DisplayAlert("Erreur", "Session invalide, reconnecte-toi.", "OK");
                        return;
                    }

                    recette.Id = _editingRecette!.Id;

                    await UpdateRecetteByIdAsync(
                        ConfigurationHelper.GetSupabaseUrl(),
                        ConfigurationHelper.GetSupabaseKey(),
                        app.CurrentAccessToken!,
                        recette.Id,
                        userId,
                        recette);
                }
                else
                {
                    // CREATE via upsert(external_id)
                    await app.RecipesService.UpsertRecetteAsync(userId, recette);
                }

                // 2) Récupérer l'ID DB
                Guid recetteId;
                if (isEdit)
                {
                    recetteId = _editingRecette!.Id;
                }
                else
                {
                    // ✅ ta signature actuelle: (Guid userId, string externalId)
                    var rid = await app.RecipesService.GetRecetteIdByExternalIdAsync(userId, recette.ExternalId!);

                    if (rid == null)
                    {
                        await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                            LocalizationResourceManager.Instance["Recipes_Add_IdNotFound"],
                            LocalizationResourceManager.Instance["Dialog_Ok"]);
                        return;
                    }

                    recetteId = rid.Value;
                }

                // stabiliser
                recette.Id = recetteId;
                if (_editingRecette != null) _editingRecette.Id = recetteId;

                // 3) Upload photo + UPDATE photo_url (jamais upsert ici)
                if (!string.IsNullOrWhiteSpace(_photoLocalPath) && File.Exists(_photoLocalPath))
                {
                    if (string.IsNullOrWhiteSpace(app.CurrentAccessToken))
                    {
                        await DisplayAlert("Erreur", "Session invalide, reconnecte-toi.", "OK");
                        return;
                    }

                    var url = await _storage.UploadRecipePhotoAndGetPublicUrlAsync(
                        app.CurrentAccessToken!, userId, recetteId, _photoLocalPath);

                    recette.PhotoUrl = url;

                    await UpdateRecetteByIdAsync(
                        ConfigurationHelper.GetSupabaseUrl(),
                        ConfigurationHelper.GetSupabaseKey(),
                        app.CurrentAccessToken!,
                        recetteId,
                        userId,
                        recette);
                }

                // 4) Ingrédients (NORMALISÉS)
                var items = ParseIngredientsEditorNormalized(IngredientsEditor.Text);

                await app.RecetteIngredientsService.ReplaceForRecetteAsync(recetteId, items);

                await DisplayAlert(LocalizationResourceManager.Instance["SuccessTitle"],
                    LocalizationResourceManager.Instance["Recipes_Add_Saved"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]);

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"], ex.Message, LocalizationResourceManager.Instance["Dialog_Ok"]);
            }
        }

        // ---------- INGREDIENTS PARSER (NORMALISÉ) ----------
        private static List<(string nom, string? quantite, string? unite)> ParseIngredientsEditorNormalized(string? text)
        {
            text ??= "";

            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(ParseIngredientLineNormalized)
                .Where(x => !string.IsNullOrWhiteSpace(x.nom))
                .ToList();
        }

        private static (string nom, string? quantite, string? unite) ParseIngredientLineNormalized(string line)
        {
            // "Nom - Quantite Unite"
            var dashSplit = line.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (dashSplit.Length >= 2)
            {
                var nomPart = IngredientNormalizer.Normalize(dashSplit[0]); // ✅ normalize
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
                var nom = IngredientNormalizer.Normalize(string.Join(" ", tokens.Skip(2))); // ✅ normalize
                return (nom, quantite, unite);
            }

            // Sinon : juste le nom
            return (IngredientNormalizer.Normalize(line), null, null); // ✅ normalize
        }

        // ---------- OCR ----------
        private async void OnScanClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["PermissionTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_CameraPermission"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_CaptureNotSupported"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
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

                bytes = ImageCompressionHelper.CompressJpeg(bytes, maxWidth: 1600, jpegQuality: 75);
                if (bytes.Length > 1024 * 1024)
                    bytes = ImageCompressionHelper.CompressJpeg(bytes, maxWidth: 1200, jpegQuality: 65);

                if (bytes.Length > 1024 * 1024)
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_ImageTooLarge"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
                    return;
                }

                var ocrKey = ConfigurationHelper.GetOcrApiKey();
                var ocr = new OcrService(ocrKey);

                var fullText = await ocr.ExtractTextFromImageAsync(bytes, photo.FileName);
                if (string.IsNullOrWhiteSpace(fullText))
                {
                    await DisplayAlert(LocalizationResourceManager.Instance["OcrTitle"],
                        LocalizationResourceManager.Instance["Recipes_Add_NoTextDetected"],
                        LocalizationResourceManager.Instance["Dialog_Ok"]);
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

                await DisplayAlert(LocalizationResourceManager.Instance["SuccessTitle"],
                    LocalizationResourceManager.Instance["Recipes_Add_OcrSuccess"],
                    LocalizationResourceManager.Instance["Dialog_Ok"]);
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

                await DisplayAlert(LocalizationResourceManager.Instance["ErrorTitle"], msg, LocalizationResourceManager.Instance["Dialog_Ok"]);
            }
        }

        // ---------- OCR HELPERS ----------
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
