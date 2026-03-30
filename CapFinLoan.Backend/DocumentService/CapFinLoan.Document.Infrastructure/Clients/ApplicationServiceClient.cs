using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Document.Infrastructure.Clients;

public class ApplicationServiceClient : IApplicationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationServiceOptions _options;

    public ApplicationServiceClient(HttpClient httpClient, IOptions<ApplicationServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<bool> ValidateApplicationAccessAsync(
        Guid applicationId,
        Guid userId,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (!_options.RequireValidation)
        {
            return true;
        }

        var path = ResolvePath(_options.ValidationPath, applicationId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddBearerToken(request, bearerToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            return false;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Application validation failed: {(int)response.StatusCode} {responseBody}");
    }

    public async Task UpdateDocumentStatusAsync(
        ApplicationDocumentStatusUpdateDto request,
        string? bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (!_options.StatusSyncEnabled)
        {
            return;
        }

        var path = ResolvePath(_options.DocumentStatusPath, request.ApplicationId);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(request)
        };

        AddBearerToken(httpRequest, bearerToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Document status sync failed: {(int)response.StatusCode} {responseBody}");
    }

    private static void AddBearerToken(HttpRequestMessage request, string? bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return;
        }

        var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken[7..].Trim()
            : bearerToken.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static string ResolvePath(string template, Guid applicationId)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new InvalidOperationException("ApplicationService path configuration is missing.");
        }

        return template.Replace("{applicationId}", applicationId.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
