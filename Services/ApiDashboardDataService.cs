using System.Net.Http.Json;
using HighFidelity.Ui.Models;
using HighFidelity.Ui.Services.Interfaces;

namespace HighFidelity.Ui.Services;

public class ApiDashboardDataService : IDashboardDataService
{
    private readonly HttpClient _httpClient;

    public ApiDashboardDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Result<IReadOnlyList<DashboardCard>>> GetDashboardCardsAsync() =>
        GetListAsync<DashboardCard>("api/dashboard/cards", "dashboard cards");

    public Task<Result<IReadOnlyList<RevenueCardItem>>> GetRevenueCardsAsync() =>
        GetListAsync<RevenueCardItem>("api/dashboard/revenue-cards", "revenue cards");

    public Task<Result<IReadOnlyList<ActivityModel>>> GetActivitiesAsync() =>
        GetListAsync<ActivityModel>("api/dashboard/activities", "activities");

    public Task<Result<IReadOnlyList<OrderModel>>> GetOrdersAsync() =>
        GetListAsync<OrderModel>("api/dashboard/orders", "orders");

    public Task<Result<IReadOnlyList<TrafficModel>>> GetTrafficSourcesAsync() =>
        GetListAsync<TrafficModel>("api/dashboard/traffic", "traffic sources");

    public async Task<Result<OrderModel>> AddOrderAsync(string customer, string country, decimal price, string status)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/dashboard/orders",
                new { customer, country, price, status });
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<OrderModel>();
            return created is null
                ? Result<OrderModel>.Failure("Server returned an empty order.")
                : Result<OrderModel>.Success(created);
        }
        catch (Exception ex)
        {
            return Result<OrderModel>.Failure($"Failed to add order: {ex.Message}");
        }
    }

    public async Task<Result<int>> DeleteOrdersAsync(IReadOnlyList<int> orderIds)
    {
        try
        {
            var query = string.Join("&", orderIds.Select(id => $"ids={id}"));
            var response = await _httpClient.DeleteAsync($"api/dashboard/orders?{query}");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<DeleteResponse>();
            return Result<int>.Success(body?.Deleted ?? orderIds.Count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to delete orders: {ex.Message}");
        }
    }

    public async Task<Result<DocumentModel>> UploadDocumentAsync(string fileName, Stream content, string contentType)
    {
        try
        {
            using var formData = new MultipartFormDataContent();
            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formData.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("api/documents/upload", formData);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return Result<DocumentModel>.Failure(
                    $"Upload failed ({response.StatusCode}): {errorBody}");
            }
            var document = await response.Content.ReadFromJsonAsync<DocumentModel>();
            return document is null
                ? Result<DocumentModel>.Failure("Server returned empty response.")
                : Result<DocumentModel>.Success(document);
        }
        catch (Exception ex)
        {
            return Result<DocumentModel>.Failure($"Failed to upload document: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<DocumentModel>>> GetDocumentsAsync()
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<List<DocumentModel>>("api/documents");
            return Result<IReadOnlyList<DocumentModel>>.Success(data ?? []);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<DocumentModel>>.Failure($"Failed to load documents: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<DeadLetterMessageModel>>> GetDeadLetterMessagesAsync() =>
        GetListAsync<DeadLetterMessageModel>("api/service-bus/dead-letter-messages", "dead-letter messages");

    private sealed record DeleteResponse(int Deleted);

    private async Task<Result<IReadOnlyList<T>>> GetListAsync<T>(string endpoint, string resourceName)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<List<T>>(endpoint);
            return Result<IReadOnlyList<T>>.Success(data ?? []);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<T>>.Failure($"Failed to load {resourceName}: {ex.Message}");
        }
    }
}
