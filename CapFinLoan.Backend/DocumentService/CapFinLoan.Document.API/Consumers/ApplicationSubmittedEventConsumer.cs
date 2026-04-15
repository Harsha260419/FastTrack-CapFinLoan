using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Document.API.Consumers;

public class ApplicationSubmittedEventConsumer : IConsumer<ApplicationSubmittedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ApplicationSubmittedEventConsumer> _logger;

    public ApplicationSubmittedEventConsumer(
        IPublishEndpoint publishEndpoint,
        ILogger<ApplicationSubmittedEventConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ApplicationSubmittedEvent> context)
    {
        var message = context.Message;
        var correlationId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : message.CorrelationId;

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