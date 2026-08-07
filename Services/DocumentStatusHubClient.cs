using HighFidelity.Ui.Services.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace HighFidelity.Ui.Services;

public class DocumentStatusHubClient : IDocumentStatusHubClient, IAsyncDisposable
{
    private readonly HubConnection _connection;

    public event Action<int, string>? DocumentStatusChanged;

    public DocumentStatusHubClient(AuthTokenHandler authTokenHandler)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{ApiSettings.BaseAddress}hubs/document-status", options =>
            {
                // Same cached JWT the REST client uses — the SignalR client
                // can't set an Authorization header on the websocket
                // handshake, so it sends this as an "access_token" query
                // param instead (see HighFidelity.Api's Program.cs).
                options.AccessTokenProvider = () => authTokenHandler.GetValidTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<int, string>("DocumentStatusChanged", (documentId, status) =>
            DocumentStatusChanged?.Invoke(documentId, status));
    }

    public async Task StartAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected) return;

        try
        {
            await _connection.StartAsync();
        }
        catch
        {
            // No hub connection yet (API not reachable at startup, etc.) — the
            // caller's fallback poll timer covers this until reconnect succeeds.
        }
    }

    public async Task StopAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected) return;
        await _connection.StopAsync();
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
