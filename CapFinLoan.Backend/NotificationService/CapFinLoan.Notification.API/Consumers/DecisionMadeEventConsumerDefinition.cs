using MassTransit;

namespace CapFinLoan.Notification.API.Consumers;

public class DecisionMadeEventConsumerDefinition : ConsumerDefinition<DecisionMadeEventConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DecisionMadeEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
