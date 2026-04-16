using CapFinLoan.Messaging.Contracts;
using CapFinLoan.Document.Application.Interfaces;
using MassTransit;

namespace CapFinLoan.Document.API.Consumers;

public class ApplicationSubmittedEventConsumer : IConsumer<ApplicationSubmittedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDocumentService _documentService;
    private readonly ILogger<ApplicationSubmittedEventConsumer> _logger;

    public ApplicationSubmittedEventConsumer(
        IPublishEndpoint publishEndpoint,
        IDocumentService documentService,
        ILogger<ApplicationSubmittedEventConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ApplicationSubmittedEvent> context)
    {
        var message = context.Message;
        var correlationId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : message.CorrelationId;

        try
        {
            await _documentService.TriggerInitialStatusSyncAsync(
                message.ApplicationId,
                correlationId,
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Initial status sync failed for application {ApplicationId}. Continuing with DocumentsRequestedEvent. CorrelationId: {CorrelationId}",
                message.ApplicationId,
                correlationId);
        }

        await _publishEndpoint.Publish(new DocumentsRequestedEvent
        {
            ApplicationId = message.ApplicationId,
            CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow,
            Source = "DocumentService"
        });

        _logger.LogInformation(
            "Published DocumentsRequestedEvent for application {ApplicationId}. CorrelationId: {CorrelationId}",
            message.ApplicationId,
            correlationId);
    }
}