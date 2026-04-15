using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Document.API.Consumers;

public class ApplicationSubmittedEventFaultConsumer : IConsumer<Fault<ApplicationSubmittedEvent>>
{
    private readonly ILogger<ApplicationSubmittedEventFaultConsumer> _logger;

    public ApplicationSubmittedEventFaultConsumer(ILogger<ApplicationSubmittedEventFaultConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<ApplicationSubmittedEvent>> context)
    {
        var message = context.Message.Message;

        _logger.LogWarning(
            "ApplicationSubmittedEvent moved to DLQ after retries for application {ApplicationId}. FaultId: {FaultId}",
            message.ApplicationId,
            context.Message.FaultId);

        return Task.CompletedTask;
    }
}