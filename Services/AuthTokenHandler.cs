using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HighFidelity.Ui.Services;

/// <summary>
/// Logs in against HighFidelity.Api's demo account and attaches the resulting
/// JWT as a Bearer token to every outgoing request. Caches the token until it
/// is close to expiry, then transparently re-logs in.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly HttpClient _authClient;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTime _expiresAtUtc = DateTime.MinValue;

    public AuthTokenHandler(HttpClient authClient) => _authClient = authClient;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }

    // Exposed so other clients that need the same Bearer token but aren't
    // themselves an HttpClient pipeline (e.g. the SignalR hub connection's
    // AccessTokenProvider) can reuse this same cached login instead of
    // re-authenticating separately.
    public Task<string?> GetValidTokenAsync(CancellationToken cancellationToken = default) =>
        GetTokenAsync(cancellationToken);

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && DateTime.UtcNow < _expiresAtUtc)
            return _token;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && DateTime.UtcNow < _expiresAtUtc)
                return _token;

            var response = await _authClient.PostAsJsonAsync("api/auth/login",
                new { username = ApiSettings.DemoUsername, password = ApiSettings.DemoPassword },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
            if (login is null)
                return null;

            _token = login.Token;
            // Refresh a minute early so an in-flight request never races expiry.
            _expiresAtUtc = login.ExpiresAtUtc.AddMinutes(-1);
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record LoginResponse(string Token, DateTime ExpiresAtUtc);
}
