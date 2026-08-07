namespace HighFidelity.Ui.Services.Interfaces;

/// <summary>
/// Real-time push replacing the old 5-second polling timer: connects to the
/// API's DocumentStatusHub and raises DocumentStatusChanged whenever
/// HighFidelity.Functions finishes updating a document's processing status.
/// </summary>
public interface IDocumentStatusHubClient
{
    event Action<int, string>? DocumentStatusChanged;

    Task StartAsync();
    Task StopAsync();
}
