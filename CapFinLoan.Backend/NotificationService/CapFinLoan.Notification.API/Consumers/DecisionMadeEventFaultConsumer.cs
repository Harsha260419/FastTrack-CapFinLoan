using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Notification.API.Consumers;

public class DecisionMadeEventFaultConsumer : IConsumer<Fault<DecisionMadeEvent>>
{
    private readonly ILogger<DecisionMadeEventFaultConsumer> _logger;

    public DecisionMadeEventFaultConsumer(ILogger<DecisionMadeEventFaultConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<DecisionMadeEvent>> context)
    {
        var message = context.Message.Message;

        _logger.LogWarning(
            "DecisionMadeEvent moved to DLQ after retries for application {ApplicationId}. FaultId: {FaultId}",
            message.ApplicationId,
            context.Message.FaultId);

        return Task.CompletedTask;
    }
}
