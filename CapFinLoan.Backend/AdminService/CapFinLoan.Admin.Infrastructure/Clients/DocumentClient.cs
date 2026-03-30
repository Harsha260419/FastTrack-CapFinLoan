using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CapFinLoan.Admin.Application.DTOs;
using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Admin.Infrastructure.Clients;

public class DocumentClient : IDocumentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly DocumentServiceOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DocumentClient(
        HttpClient httpClient,
        IOptions<DocumentServiceOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DocumentVerificationResponseDto?> GetDocumentByIdAsync(Guid documentId)
    {
        var path = _options.GetByIdPath.Replace("{id}", documentId.ToString());
        using var httpRequest = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(httpRequest);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Not authorized to access this document.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch document. Status code: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<DocumentVerificationResponseDto>(JsonOptions);
    }

    public async Task<DocumentVerificationResponseDto?> VerifyDocumentAsync(Guid documentId, VerifyDocumentRequestDto request)
    {
        var path = _options.VerifyPath.Replace("{id}", documentId.ToString());
        using var httpRequest = CreateRequest(HttpMethod.Put, path, request);
        using var response = await _httpClient.SendAsync(httpRequest);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException("Document verification request is invalid.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Not authorized to verify this document.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to verify document. Status code: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<DocumentVerificationResponseDto>(JsonOptions);
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
}