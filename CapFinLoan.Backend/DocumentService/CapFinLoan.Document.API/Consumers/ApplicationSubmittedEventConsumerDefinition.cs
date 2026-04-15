using MassTransit;

namespace CapFinLoan.Document.API.Consumers;

public class ApplicationSubmittedEventConsumerDefinition : ConsumerDefinition<ApplicationSubmittedEventConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ApplicationSubmittedEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}