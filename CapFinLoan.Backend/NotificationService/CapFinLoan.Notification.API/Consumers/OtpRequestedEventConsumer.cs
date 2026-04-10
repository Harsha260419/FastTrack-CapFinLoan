using CapFinLoan.Messaging.Contracts;
using CapFinLoan.Notification.API.Services;
using MassTransit;

namespace CapFinLoan.Notification.API.Consumers;

public class OtpRequestedEventConsumer : IConsumer<OtpRequestedEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<OtpRequestedEventConsumer> _logger;

    public OtpRequestedEventConsumer(IEmailSender emailSender, ILogger<OtpRequestedEventConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OtpRequestedEvent> context)
    {
        var message = context.Message;

        try
        {
            await _emailSender.SendOtpEmailAsync(
                message.ApplicantName,
                message.Email,
                message.OtpCode,
                message.ExpiresAt,
                context.CancellationToken);

            _logger.LogInformation("OTP email sent to {Email}", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", message.Email);
        }
    }
}
