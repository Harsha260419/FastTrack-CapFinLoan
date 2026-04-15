using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CapFinLoan.Messaging.Contracts.ApplicationStatus;
using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Infrastructure.Options;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Document.Infrastructure.Clients;

public class ApplicationServiceClient : IApplicationServiceClient
{
    private static readonly TimeSpan MessagingPublishTimeout = TimeSpan.FromSeconds(4);

    private readonly HttpClient _httpClient;
    private readonly ApplicationServiceOptions _options;
    private readonly IRequestClient<UpdateApplicationStatusCommand> _updateStatusRequestClient;
    private readonly ILogger<ApplicationServiceClient> _logger;

    public ApplicationServiceClient(
        HttpClient httpClient,
        IOptions<ApplicationServiceOptions> options,
        IRequestClient<UpdateApplicationStatusCommand> updateStatusRequestClient,
        ILogger<ApplicationServiceClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _updateStatusRequestClient = updateStatusRequestClient;
        _logger = logger;
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
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.StatusSyncEnabled)
        {
            return;
        }

        var mode = string.IsNullOrWhiteSpace(_options.StatusSyncMode)
            ? ApplicationServiceOptions.HttpOnlyMode
            : _options.StatusSyncMode.Trim();

        if (mode.Equals(ApplicationServiceOptions.HttpOnlyMode, StringComparison.OrdinalIgnoreCase))
        {
            await UpdateStatusOverHttpAsync(request, bearerToken, cancellationToken);
            return;
        }

        if (mode.Equals(ApplicationServiceOptions.DualWriteMode, StringComparison.OrdinalIgnoreCase))
        {
            await UpdateStatusOverHttpAsync(request, bearerToken, cancellationToken);

            try
            {
                await UpdateStatusOverMessagingAsync(request, correlationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ dual-write status sync failed for application {ApplicationId}; HTTP path succeeded.",
                    request.ApplicationId);
            }

            return;
        }

        if (mode.Equals(ApplicationServiceOptions.RabbitMqPrimaryMode, StringComparison.OrdinalIgnoreCase))
        {
            await UpdateStatusOverMessagingAsync(request, correlationId, cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported ApplicationService:StatusSyncMode '{_options.StatusSyncMode}'. Supported values: " +
            $"{ApplicationServiceOptions.HttpOnlyMode}, {ApplicationServiceOptions.DualWriteMode}, {ApplicationServiceOptions.RabbitMqPrimaryMode}.");
    }

    private async Task UpdateStatusOverHttpAsync(
        ApplicationDocumentStatusUpdateDto request,
        string? bearerToken,
        CancellationToken cancellationToken)
    {
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

    private async Task UpdateStatusOverMessagingAsync(
        ApplicationDocumentStatusUpdateDto request,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(MessagingPublishTimeout);

        var command = new UpdateApplicationStatusCommand
        {
            ApplicationId = request.ApplicationId,
            Status = request.Status,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? Guid.NewGuid().ToString("N")
                : correlationId
        };

        Response<UpdateApplicationStatusResult> response;

        try
        {
            response = await _updateStatusRequestClient.GetResponse<UpdateApplicationStatusResult>(command, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"RabbitMQ publish timed out after {MessagingPublishTimeout.TotalSeconds:0} seconds.",
                ex);
        }

        if (!response.Message.Success)
        {
            throw new InvalidOperationException($"RabbitMQ status sync failed: {response.Message.Message}");
        }

        _logger.LogInformation(
            "RabbitMQ publish succeeded for application {ApplicationId}",
            request.ApplicationId);
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
