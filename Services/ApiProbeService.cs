using System.Net.Http.Json;
using HighFidelity.Ui.Models;
using HighFidelity.Ui.Services.Interfaces;

namespace HighFidelity.Ui.Services;

public class ApiProbeService : IApiProbeService
{
    private readonly HttpClient _httpClient;

    public ApiProbeService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<string>> GetAsync(string relativePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(relativePath);
            var body = await response.Content.ReadAsStringAsync();
            return Result<string>.Success($"HTTP {(int)response.StatusCode} {response.StatusCode}\n\n{body}");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Request failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> PostAsync(string relativePath, object? body = null)
    {
        try
        {
            var response = body is null
                ? await _httpClient.PostAsync(relativePath, new StringContent(string.Empty))
                : await _httpClient.PostAsJsonAsync(relativePath, body);
            var responseBody = await response.Content.ReadAsStringAsync();
            return Result<string>.Success($"HTTP {(int)response.StatusCode} {response.StatusCode}\n\n{responseBody}");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Request failed: {ex.Message}");
        }
    }
}
