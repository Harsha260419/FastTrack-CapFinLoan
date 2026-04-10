using CapFinLoan.Notification.API.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CapFinLoan.Notification.API.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _emailOptions;

    public SmtpEmailSender(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public async Task SendDecisionEmailAsync(
        string applicantName,
        string applicantEmail,
        string decision,
        string remarks,
        decimal sanctionAmount,
        double interestRate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.SmtpHost)
            || string.IsNullOrWhiteSpace(_emailOptions.SenderEmail)
            || string.IsNullOrWhiteSpace(_emailOptions.AppPassword))
        {
            throw new InvalidOperationException("Email configuration is incomplete.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.SenderName, _emailOptions.SenderEmail));
        message.To.Add(MailboxAddress.Parse(applicantEmail));

        if (string.Equals(decision, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            message.Subject = "Your loan application has been approved";
            message.Body = new TextPart("plain")
            {
                Text = $"Hello {applicantName},\n\n" +
                       "Congratulations. Your loan application has been approved.\n" +
                       $"Sanction Amount: {sanctionAmount:C}\n" +
                       $"Interest Rate: {interestRate:0.##}%\n" +
                       $"Remarks: {remarks}\n\n" +
                       "Thank you for choosing CapFinLoan."
            };
        }
        else
        {
            message.Subject = "Update on your loan application";
            message.Body = new TextPart("plain")
            {
                Text = $"Hello {applicantName},\n\n" +
                       "Thank you for your interest in CapFinLoan.\n" +
                       "After careful review, we are unable to approve your loan application at this time.\n" +
                       $"Remarks: {remarks}\n\n" +
                       "You may reapply in the future with updated details."
            };
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailOptions.SmtpHost, _emailOptions.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_emailOptions.SenderEmail, _emailOptions.AppPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendOtpEmailAsync(
        string applicantName,
        string email,
        string otpCode,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.SmtpHost)
            || string.IsNullOrWhiteSpace(_emailOptions.SenderEmail)
            || string.IsNullOrWhiteSpace(_emailOptions.AppPassword))
        {
            throw new InvalidOperationException("Email configuration is incomplete.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.SenderName, _emailOptions.SenderEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Your CapFinLoan OTP Code";
        message.Body = new TextPart("plain")
        {
            Text = $"Hello {applicantName},\n\n" +
                   "Use the OTP below to complete your signup:\n" +
                   $"OTP: {otpCode}\n" +
                   $"Expires At (UTC): {expiresAt:yyyy-MM-dd HH:mm:ss}\n\n" +
                   "If you did not request this OTP, you can safely ignore this email."
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailOptions.SmtpHost, _emailOptions.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_emailOptions.SenderEmail, _emailOptions.AppPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
