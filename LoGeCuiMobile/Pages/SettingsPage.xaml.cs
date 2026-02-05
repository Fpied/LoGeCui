using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoGeCuiMobile.Resources.Lang;
using Microsoft.Maui.Storage;

namespace LoGeCuiMobile.Pages;

public partial class SettingsPage : ContentPage
{
    private const string ListsTable = "shopping_lists";
    private const string MembersTable = "shopping_list_members";
    private const string JoinRpc = "join_shopping_list";

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await RefreshSharingUiAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.Message, "OK");
        }
    }

    // =========================
    // UI actions (partage)
    // =========================

    private async void OnCreateOrShowMyCodeClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;

            if (app.CurrentUserId == null || app.RestClient == null)
            {
                await DisplayAlert("Erreur", "Tu dois être connecté.", "OK");
                return;
            }

            var myList = await GetOrCreateMyOwnedListAsync(app.CurrentUserId.Value);
            app.SetCurrentShoppingListId(myList.id);

            await RefreshSharingUiAsync();
            await DisplayAlert("OK", $"Ton code de partage : {myList.share_code}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.ToString(), "OK");
        }
    }

    private async void OnCopyCodeClicked(object sender, EventArgs e)
    {
        try
        {
            var code = LblShareCode?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(code) || code == "(aucun)")
            {
                await DisplayAlert("Info", "Aucun code à copier.", "OK");
                return;
            }

            await Clipboard.SetTextAsync(code);
            await DisplayAlert("OK", "Code copié ✅", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.ToString(), "OK");
        }
    }

    private async void OnJoinWithCodeClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;

            if (app.CurrentUserId == null || app.RestClient == null || !app.IsConnected)
            {
                await DisplayAlert("Erreur", "Tu dois être connecté pour rejoindre une liste.", "OK");
                return;
            }

            var code = await DisplayPromptAsync("Rejoindre une liste", "Entre le code de partage :");
            if (string.IsNullOrWhiteSpace(code))
                return;

            code = code.Trim();

            var joined = await JoinByCodeViaRpcAsync(code);

            if (joined == null)
            {
                await DisplayAlert("Erreur", "Code invalide (liste introuvable).", "OK");
                return;
            }

            app.SetCurrentShoppingListId(joined.Value.listId);

            await RefreshSharingUiAsync();
            await DisplayAlert("Succès", $"Tu as rejoint la liste : {joined.Value.name}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.ToString(), "OK");
        }
    }

    private async void OnPickActiveListClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;

            if (app.CurrentUserId == null || app.RestClient == null)
            {
                await DisplayAlert("Erreur", "Tu dois être connecté.", "OK");
                return;
            }

            var lists = await GetMyAccessibleListsAsync(app.CurrentUserId.Value);

            if (lists.Count == 0)
            {
                await DisplayAlert("Info", "Tu n'as aucune liste partagée.", "OK");
                return;
            }

            var labels = lists
                .Select(l => $"{(string.IsNullOrWhiteSpace(l.name) ? "Liste partagée" : l.name)}  ({l.share_code})")
                .ToArray();

            var chosen = await DisplayActionSheet("Choisir la liste active", "Annuler", null, labels);
            if (string.IsNullOrWhiteSpace(chosen) || chosen == "Annuler")
                return;

            var idx = Array.IndexOf(labels, chosen);
            if (idx < 0) return;

            app.SetCurrentShoppingListId(lists[idx].id);

            await RefreshSharingUiAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.ToString(), "OK");
        }
    }

    private async void OnDisableSharedListClicked(object sender, EventArgs e)
    {
        try
        {
            var app = (App)Application.Current;
            app.SetCurrentShoppingListId(Guid.Empty);

            await RefreshSharingUiAsync();
            await DisplayAlert("OK", "Retour à ta liste perso ✅", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", ex.ToString(), "OK");
        }
    }

    // =========================
    // UI actions (compte)
    // =========================

    private async void OnDesinscriptionClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmation",
            "Supprimer définitivement ton compte ? Cette action est irréversible.",
            "Oui, supprimer",
            "Annuler");

        if (!confirm) return;

        var app = (App)Application.Current;

        if (app.Supabase == null || app.CurrentAccessToken == null)
        {
            await DisplayAlert("Erreur", "Session invalide. Reconnecte-toi d'abord.", "OK");
            app.ShowLogin();
            return;
        }

        var userId = app.CurrentUserId?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Erreur", "Session invalide.", "OK");
            app.ShowLogin();
            return;
        }

        app.Supabase.SetSession(app.CurrentAccessToken, userId);

        var (ok, err) = await app.Supabase.DeleteAccountAsync();

        if (!ok)
        {
            await DisplayAlert("Erreur", $"Suppression impossible : {err}", "OK");

            if (err?.Contains("JWT", StringComparison.OrdinalIgnoreCase) == true)
            {
                app.ShowLogin();
            }
            return;
        }

        await DisplayAlert("Succès", "Ton compte a été supprimé.", "OK");
        app.Logout(silent: true);
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        var app = (App)Application.Current;

        app.Supabase?.ClearSession();
        SecureStorage.Remove("sb_access_token");
        SecureStorage.Remove("sb_user_id");

        app.ShowLogin();
    }

    // =========================
    // Helpers: refresh UI
    // =========================

    private async Task RefreshSharingUiAsync()
    {
        try
        {
            var app = (App)Application.Current;

            if (app.CurrentUserId == null)
            {
                LblActiveList.Text = "Liste perso (non partagée)";
                LblShareCode.Text = "(aucun)";
                return;
            }

            if (app.CurrentShoppingListId == null || app.CurrentShoppingListId == Guid.Empty)
            {
                LblActiveList.Text = "Liste perso (non partagée)";
                LblShareCode.Text = "(aucun)";
                return;
            }

            if (!app.IsConnected || app.RestClient == null)
            {
                LblActiveList.Text = "Ma liste";
                LblShareCode.Text = "(mode hors ligne)";
                return;
            }

            var list = await GetListByIdAsync(app.CurrentShoppingListId.Value);
            if (list == null)
            {
                LblActiveList.Text = "Ma liste";
                LblShareCode.Text = "(aucun)";
                return;
            }

            LblActiveList.Text = string.IsNullOrWhiteSpace(list.name) ? "Ma liste" : list.name;
            LblShareCode.Text = list.share_code ?? "(aucun)";
        }
        catch
        {
            LblActiveList.Text = "Ma liste";
            LblShareCode.Text = "(aucun)";
        }
    }

    // =========================
    // JOIN via RPC
    // =========================

    private async Task<(Guid listId, string name)?> JoinByCodeViaRpcAsync(string code)
    {
        var app = (App)Application.Current;

        var payload = new { p_share_code = code };

        try
        {
            var rows = await app.RestClient!.PostAsync<List<JoinResultRow>>(
                $"rpc/{JoinRpc}",
                payload,
                returnRepresentation: true);

            var row = rows?.FirstOrDefault();
            if (row == null || row.list_id == Guid.Empty)
                return null;

            return (row.list_id, row.name ?? "Liste partagée");
        }
        catch (Exception ex)
        {
            var msg = ex.ToString();

            if (msg.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Not Found", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("could not find the function", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    "La fonction RPC 'join_shopping_list' n'existe pas côté Supabase.\n" +
                    "Il faut la créer (SECURITY DEFINER) pour rejoindre une liste par share_code avec RLS.",
                    ex);
            }

            throw;
        }
    }

    // =========================
    // Supabase queries
    // =========================

    private async Task<ShoppingListRow?> GetListByIdAsync(Guid id)
    {
        var app = (App)Application.Current;
        var q = $"{ListsTable}?select=id,owner_user_id,name,share_code,created_at&id=eq.{id}&limit=1";

        var rows = await app.RestClient!.GetAsync<List<ShoppingListRow>>(q);
        return rows?.FirstOrDefault();
    }

    private async Task<ShoppingListRow> GetOrCreateMyOwnedListAsync(Guid userId)
    {
        var app = (App)Application.Current;

        var q = $"{ListsTable}?select=id,owner_user_id,name,share_code,created_at&owner_user_id=eq.{userId}&order=created_at.asc&limit=1";
        var existing = await app.RestClient!.GetAsync<List<ShoppingListRow>>(q);
        var row = existing?.FirstOrDefault();
        if (row != null)
            return row;

        var payload = new
        {
            owner_user_id = userId,
            name = "Ma liste"
        };

        var created = await app.RestClient!.PostAsync<List<ShoppingListRow>>(
            $"{ListsTable}?select=id,owner_user_id,name,share_code,created_at",
            payload,
            returnRepresentation: true);

        var createdRow = created?.FirstOrDefault()
            ?? throw new Exception("Création de liste impossible.");

        await UpsertMemberAsync(createdRow.id, userId, role: "owner");

        return createdRow;
    }

    private async Task<List<ShoppingListRow>> GetMyAccessibleListsAsync(Guid userId)
    {
        var app = (App)Application.Current;

        var mq = $"{MembersTable}?select=list_id,role&user_id=eq.{userId}";
        var memberships = await app.RestClient!.GetAsync<List<MemberRow>>(mq) ?? new List<MemberRow>();

        var ids = memberships.Select(m => m.list_id).Distinct().ToList();

        var oq = $"{ListsTable}?select=id,owner_user_id,name,share_code,created_at&owner_user_id=eq.{userId}";
        var owned = await app.RestClient!.GetAsync<List<ShoppingListRow>>(oq) ?? new List<ShoppingListRow>();

        foreach (var o in owned)
            if (!ids.Contains(o.id))
                ids.Add(o.id);

        if (ids.Count == 0)
            return owned;

        var inList = string.Join(",", ids.Select(x => x.ToString()));
        var q = $"{ListsTable}?select=id,owner_user_id,name,share_code,created_at&id=in.({inList})&order=created_at.desc";

        var rows = await app.RestClient!.GetAsync<List<ShoppingListRow>>(q);
        return rows ?? new List<ShoppingListRow>();
    }

    private async Task UpsertMemberAsync(Guid listId, Guid userId, string role = "member")
    {
        var app = (App)Application.Current;

        var payload = new[]
        {
            new { list_id = listId, user_id = userId, role = role }
        };

        await app.RestClient!.PostAsync<object>(
            $"{MembersTable}?on_conflict=list_id,user_id",
            payload,
            returnRepresentation: false,
            mergeDuplicates: true);
    }

    // =========================
    // DTOs
    // =========================

    private sealed class ShoppingListRow
    {
        public Guid id { get; set; }
        public Guid owner_user_id { get; set; }
        public string? name { get; set; }
        public string? share_code { get; set; }
        public DateTimeOffset created_at { get; set; }
    }

    private sealed class MemberRow
    {
        public Guid list_id { get; set; }
        public string? role { get; set; }
    }

    private sealed class JoinResultRow
    {
        public Guid list_id { get; set; }
        public string? name { get; set; }
    }
}