using CapFinLoan.Messaging.Contracts.ApplicationStatus;
using MassTransit;

namespace CapFinLoan.Application.API.Consumers;

public class UpdateApplicationStatusFaultConsumer : IConsumer<Fault<UpdateApplicationStatusCommand>>
{
    private readonly ILogger<UpdateApplicationStatusFaultConsumer> _logger;

    public UpdateApplicationStatusFaultConsumer(ILogger<UpdateApplicationStatusFaultConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<UpdateApplicationStatusCommand>> context)
    {
        var message = context.Message.Message;

        _logger.LogWarning(
            "UpdateApplicationStatusCommand moved to DLQ after retries for application {ApplicationId}. FaultId: {FaultId}",
            message.ApplicationId,
            context.Message.FaultId);

        return Task.CompletedTask;
    }
}
