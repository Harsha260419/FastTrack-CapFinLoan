using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Admin.API.Consumers;

public class DocumentsVerifiedEventConsumer : IConsumer<DocumentsVerifiedEvent>
{
    private readonly ILogger<DocumentsVerifiedEventConsumer> _logger;

    public DocumentsVerifiedEventConsumer(ILogger<DocumentsVerifiedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DocumentsVerifiedEvent> context)
    {
        var message = context.Message;
        var correlationId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : message.CorrelationId;

        _logger.LogInformation(
            "DocumentsVerifiedEvent received for application {ApplicationId}. Awaiting admin action to move to UnderReview. CorrelationId: {CorrelationId}",
            message.ApplicationId,
            correlationId);

        return Task.CompletedTask;
    }
}