using HighFidelity.Ui.Models;

namespace HighFidelity.Ui.Services.Interfaces;

public interface IDashboardDataService
{
    Task<Result<IReadOnlyList<DashboardCard>>> GetDashboardCardsAsync();
    Task<Result<IReadOnlyList<RevenueCardItem>>> GetRevenueCardsAsync();
    Task<Result<IReadOnlyList<ActivityModel>>> GetActivitiesAsync();
    Task<Result<IReadOnlyList<OrderModel>>> GetOrdersAsync();
    Task<Result<IReadOnlyList<TrafficModel>>> GetTrafficSourcesAsync();

    /// <summary>Persists a new order; the backend assigns Id and Invoice.</summary>
    Task<Result<OrderModel>> AddOrderAsync(string customer, string country, decimal price, string status);

    /// <summary>Deletes orders by database Id. Returns the number deleted.</summary>
    Task<Result<int>> DeleteOrdersAsync(IReadOnlyList<int> orderIds);

    /// <summary>Uploads a file to Azure Blob Storage.</summary>
    Task<Result<DocumentModel>> UploadDocumentAsync(string fileName, Stream content, string contentType);

    /// <summary>Gets all uploaded documents metadata.</summary>
    Task<Result<IReadOnlyList<DocumentModel>>> GetDocumentsAsync();

    /// <summary>Peeks messages currently in the Service Bus dead-letter sub-queue — doesn't remove/lock them, diagnostics only.</summary>
    Task<Result<IReadOnlyList<DeadLetterMessageModel>>> GetDeadLetterMessagesAsync();
}
