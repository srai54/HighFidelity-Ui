using HighFidelity.Ui.Models;

namespace HighFidelity.Ui.Services.Interfaces;

/// <summary>
/// Calls an arbitrary backend endpoint and hands back the raw response text
/// (status line + body), unparsed — used by the MongoDB-concepts demo page
/// to prove 20+ endpoints work without needing a typed model per endpoint.
/// Deliberately separate from IDashboardDataService, which every other page
/// depends on and which this must not touch.
/// </summary>
public interface IApiProbeService
{
    Task<Result<string>> GetAsync(string relativePath);
    Task<Result<string>> PostAsync(string relativePath, object? body = null);
}
