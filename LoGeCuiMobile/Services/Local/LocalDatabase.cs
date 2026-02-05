using SQLite;
using LoGeCuiShared.Models;

namespace LoGeCuiMobile.Services.Local;

public class LocalDatabase
{
    private readonly SQLiteAsyncConnection _db;

    public LocalDatabase(string path)
    {
        _db = new SQLiteAsyncConnection(path);
        _db.CreateTableAsync<IngredientLocal>().Wait();
        _db.CreateTableAsync<ArticleLocal>().Wait();
        _db.CreateTableAsync<RecetteLocal>().Wait();

    }

    // ---------- INGREDIENTS ----------
    public Task<List<IngredientLocal>> GetIngredientsAsync() =>
        _db.Table<IngredientLocal>().ToListAsync();

    public Task SaveIngredientsAsync(IEnumerable<Ingredient> ingredients)
    {
        return _db.RunInTransactionAsync(tran =>
        {
            tran.DeleteAll<IngredientLocal>();
            foreach (var i in ingredients)
                tran.Insert(new IngredientLocal(i));
        });
    }

    // ---------- ARTICLES ----------
    public Task<List<ArticleLocal>> GetArticlesAsync() =>
        _db.Table<ArticleLocal>().ToListAsync();

    public Task SaveArticlesAsync(IEnumerable<ArticleCourse> articles)
    {
        return _db.RunInTransactionAsync(tran =>
        {
            tran.DeleteAll<ArticleLocal>();
            foreach (var a in articles)
                tran.Insert(new ArticleLocal(a));
        });
    }

    public Task<List<RecetteLocal>> GetRecettesAsync() =>
    _db.Table<RecetteLocal>().ToListAsync();

    public Task SaveRecettesAsync(IEnumerable<Recette> recettes)
    {
        return _db.RunInTransactionAsync(tran =>
        {
            tran.DeleteAll<RecetteLocal>();
            foreach (var r in recettes)
                tran.Insert(new RecetteLocal(r));
        });
    }
}
