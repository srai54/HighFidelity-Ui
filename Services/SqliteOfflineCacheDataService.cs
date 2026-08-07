using System.Text.Json;
using HighFidelity.Ui.Models;
using HighFidelity.Ui.Services.Interfaces;
using SQLite;

namespace HighFidelity.Ui.Services;

/// <summary>
/// Offline-resilience decorator around the real IDashboardDataService: writes
/// every successful list response to a local SQLite file, and if a later call
/// fails (no network — this app has no embedded/static data source otherwise),
/// falls back to whatever was last cached instead of showing an empty
/// dashboard. Only the five read-mostly list endpoints are cached; orders
/// mutations, document upload, and dead-letter peeks always require a live
/// connection since there's nothing meaningful to serve stale.
/// </summary>
public class SqliteOfflineCacheDataService : IDashboardDataService
{
    private readonly IDashboardDataService _inner;
    private readonly SQLiteAsyncConnection _db;
    private readonly Task _initialization;

    public SqliteOfflineCacheDataService(IDashboardDataService inner)
    {
        _inner = inner;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "offline_cache.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        _initialization = _db.CreateTableAsync<CachedResponse>();
    }

    public Task<Result<IReadOnlyList<DashboardCard>>> GetDashboardCardsAsync() =>
        GetOrFallbackAsync("dashboard-cards", _inner.GetDashboardCardsAsync);

    public Task<Result<IReadOnlyList<RevenueCardItem>>> GetRevenueCardsAsync() =>
        GetOrFallbackAsync("revenue-cards", _inner.GetRevenueCardsAsync);

    public Task<Result<IReadOnlyList<ActivityModel>>> GetActivitiesAsync() =>
        GetOrFallbackAsync("activities", _inner.GetActivitiesAsync);

    public Task<Result<IReadOnlyList<OrderModel>>> GetOrdersAsync() =>
        GetOrFallbackAsync("orders", _inner.GetOrdersAsync);

    public Task<Result<IReadOnlyList<TrafficModel>>> GetTrafficSourcesAsync() =>
        GetOrFallbackAsync("traffic", _inner.GetTrafficSourcesAsync);

    // Always live — no offline fallback makes sense for a mutation or an upload.
    public Task<Result<OrderModel>> AddOrderAsync(string customer, string country, decimal price, string status) =>
        _inner.AddOrderAsync(customer, country, price, status);

    public Task<Result<int>> DeleteOrdersAsync(IReadOnlyList<int> orderIds) =>
        _inner.DeleteOrdersAsync(orderIds);

    public Task<Result<DocumentModel>> UploadDocumentAsync(string fileName, Stream content, string contentType) =>
        _inner.UploadDocumentAsync(fileName, content, contentType);

    public Task<Result<IReadOnlyList<DocumentModel>>> GetDocumentsAsync() =>
        _inner.GetDocumentsAsync();

    public Task<Result<IReadOnlyList<DeadLetterMessageModel>>> GetDeadLetterMessagesAsync() =>
        _inner.GetDeadLetterMessagesAsync();

    private async Task<Result<IReadOnlyList<T>>> GetOrFallbackAsync<T>(string cacheKey, Func<Task<Result<IReadOnlyList<T>>>> loadLive)
    {
        var result = await loadLive();

        await _initialization;

        if (result.IsSuccess)
        {
            try
            {
                await _db.InsertOrReplaceAsync(new CachedResponse
                {
                    Key = cacheKey,
                    Json = JsonSerializer.Serialize(result.Data),
                    CachedAtUtc = DateTime.UtcNow
                });
            }
            catch
            {
                // Caching is a nice-to-have; never let a disk write failure
                // hide an otherwise-successful live result.
            }

            return result;
        }

        try
        {
            var cached = await _db.FindAsync<CachedResponse>(cacheKey);
            if (cached is not null)
            {
                var data = JsonSerializer.Deserialize<List<T>>(cached.Json);
                if (data is not null)
                    return Result<IReadOnlyList<T>>.Success(data);
            }
        }
        catch
        {
            // Fall through to the original live-call failure below.
        }

        return result;
    }
}
