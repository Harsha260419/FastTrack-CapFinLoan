using MassTransit;

namespace CapFinLoan.Notification.API.Consumers;

public class OtpRequestedEventConsumerDefinition : ConsumerDefinition<OtpRequestedEventConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OtpRequestedEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
