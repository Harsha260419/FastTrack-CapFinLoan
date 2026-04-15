using MassTransit;

namespace CapFinLoan.Admin.API.Consumers;

public class DocumentsVerifiedEventConsumerDefinition : ConsumerDefinition<DocumentsVerifiedEventConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DocumentsVerifiedEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}