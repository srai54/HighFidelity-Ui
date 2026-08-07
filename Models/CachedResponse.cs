using SQLite;

namespace HighFidelity.Ui.Models;

/// <summary>SQLite table backing SqliteOfflineCacheDataService — one row per cached list endpoint.</summary>
public class CachedResponse
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Json { get; set; } = string.Empty;

    public DateTime CachedAtUtc { get; set; }
}
