using MassTransit;

namespace CapFinLoan.Application.API.Consumers;

public class UpdateApplicationStatusCommandConsumerDefinition : ConsumerDefinition<UpdateApplicationStatusCommandConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UpdateApplicationStatusCommandConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
