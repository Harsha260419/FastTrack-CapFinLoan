using CapFinLoan.Messaging.Contracts;
using MassTransit;

namespace CapFinLoan.Notification.API.Consumers;

public class OtpRequestedEventFaultConsumer : IConsumer<Fault<OtpRequestedEvent>>
{
    private readonly ILogger<OtpRequestedEventFaultConsumer> _logger;

    public OtpRequestedEventFaultConsumer(ILogger<OtpRequestedEventFaultConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<OtpRequestedEvent>> context)
    {
        _logger.LogWarning(
            "OtpRequestedEvent moved to DLQ after retries for email {Email}. FaultId: {FaultId}",
            context.Message.Message.Email,
            context.Message.FaultId);

        return Task.CompletedTask;
    }
}
