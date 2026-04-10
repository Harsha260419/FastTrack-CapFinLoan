using CapFinLoan.Messaging.Contracts;
using CapFinLoan.Notification.API.Services;
using MassTransit;

namespace CapFinLoan.Notification.API.Consumers;

public class DecisionMadeEventConsumer : IConsumer<DecisionMadeEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DecisionMadeEventConsumer> _logger;

    public DecisionMadeEventConsumer(IEmailSender emailSender, ILogger<DecisionMadeEventConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DecisionMadeEvent> context)
    {
        var message = context.Message;

        try
        {
            await _emailSender.SendDecisionEmailAsync(
                message.ApplicantName,
                message.ApplicantEmail,
                message.Decision,
                message.Remarks,
                message.SanctionAmount,
                message.InterestRate,
                context.CancellationToken);

            _logger.LogInformation(
                "Email sent to {ApplicantEmail} for application {ApplicationId} decision {Decision}",
                message.ApplicantEmail,
                message.ApplicationId,
                message.Decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {ApplicantEmail} for application {ApplicationId}",
                message.ApplicantEmail,
                message.ApplicationId);
        }
    }
}
