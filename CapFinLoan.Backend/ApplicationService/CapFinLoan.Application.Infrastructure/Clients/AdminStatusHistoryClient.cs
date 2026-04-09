using System.Net.Http.Headers;
using System.Net.Http.Json;
using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Application.Infrastructure.Clients;

public class AdminStatusHistoryClient : IAdminStatusHistoryClient
{
    private readonly HttpClient _httpClient;
    private readonly AdminServiceOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminStatusHistoryClient(
        HttpClient httpClient,
        IOptions<AdminServiceOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<(string ToStatus, DateTime ChangedAt, string? Remarks)>> GetStatusHistoryAsync(Guid applicationId)
    {
        var path = _options.StatusHistoryPath.Replace("{id}", applicationId.ToString());

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);

            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var parsedHeader))
            {
                request.Headers.Authorization = parsedHeader;
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var payload = await response.Content.ReadFromJsonAsync<List<AdminStatusHistoryItemDto>>();
            if (payload is null || payload.Count == 0)
            {
                return [];
            }

            return payload
                .Select(x => (x.ToStatus, x.ChangedAt, x.Remarks))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private sealed class AdminStatusHistoryItemDto
    {
        public string ToStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}
