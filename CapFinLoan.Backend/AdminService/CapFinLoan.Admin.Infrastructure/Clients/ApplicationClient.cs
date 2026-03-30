using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CapFinLoan.Admin.Application.DTOs;
using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Admin.Infrastructure.Clients;

public class ApplicationClient : IApplicationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly ApplicationServiceOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationClient(
        HttpClient httpClient,
        IOptions<ApplicationServiceOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<ApplicationServiceApplicationDto>> GetApplicationsAsync(string? status)
    {
        var path = _options.QueuePath;
        if (!string.IsNullOrWhiteSpace(status))
        {
            var separator = path.Contains('?') ? "&" : "?";
            path = $"{path}{separator}status={status}";
        }

        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch applications. Status code: {response.StatusCode}");
        }

        var items = await ReadFromEnvelopeOrListAsync(response);
        return items;
    }

    public async Task<ApplicationServiceApplicationDto?> GetApplicationByIdAsync(Guid applicationId)
    {
        var path = BuildPath(_options.DetailsPath, applicationId);
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch application details. Status code: {response.StatusCode}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ApplicationServiceApplicationDto>(JsonOptions);
        return payload;
    }

    public async Task<string?> GetCurrentStatusAsync(Guid applicationId)
    {
        var path = BuildPath(_options.StatusPath, applicationId);
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch application status. Status code: {response.StatusCode}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ApplicationStatusPayload>(JsonOptions);
        return payload?.CurrentStatus ?? payload?.Status;
    }

    public async Task<bool> UpdateStatusAsync(Guid applicationId, string status, string? remarks)
    {
        var path = BuildPath(_options.StatusPath, applicationId);
        var payload = new
        {
            status,
            remarks
        };

        using var request = CreateRequest(HttpMethod.Patch, path, payload);
        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException("Application service rejected the requested transition.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to update application status. Status code: {response.StatusCode}");
        }

        return true;
    }

    private static string BuildPath(string template, Guid applicationId)
    {
        return template.Replace("{id}", applicationId.ToString());
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);

        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static async Task<IReadOnlyList<ApplicationServiceApplicationDto>> ReadFromEnvelopeOrListAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var listPayload = JsonSerializer.Deserialize<List<ApplicationServiceApplicationDto>>(json, JsonOptions);
            return listPayload ?? [];
        }

        if (document.RootElement.TryGetProperty("items", out var itemsElement)
            && itemsElement.ValueKind == JsonValueKind.Array)
        {
            var itemsPayload = JsonSerializer.Deserialize<List<ApplicationServiceApplicationDto>>(itemsElement.GetRawText(), JsonOptions);
            return itemsPayload ?? [];
        }

        return [];
    }

    private sealed class ApplicationStatusPayload
    {
        public string? CurrentStatus { get; set; }
        public string? Status { get; set; }
    }
}
