using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Admin.API.Consumers;

public class DocumentsVerifiedEventFaultConsumer : IConsumer<Fault<DocumentsVerifiedEvent>>
{
    private readonly ILogger<DocumentsVerifiedEventFaultConsumer> _logger;

    public DocumentsVerifiedEventFaultConsumer(ILogger<DocumentsVerifiedEventFaultConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<DocumentsVerifiedEvent>> context)
    {
        var message = context.Message.Message;

        _logger.LogWarning(
            "DocumentsVerifiedEvent moved to DLQ after retries for application {ApplicationId}. FaultId: {FaultId}",
            message.ApplicationId,
            context.Message.FaultId);

        return Task.CompletedTask;
    }
}