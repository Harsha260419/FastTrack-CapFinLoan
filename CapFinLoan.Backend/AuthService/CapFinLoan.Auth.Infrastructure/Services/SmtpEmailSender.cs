using System.Net;
using System.Net.Mail;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Auth.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;

    public SmtpEmailSender(IOptions<SmtpSettings> smtpOptions)
    {
        _smtpSettings = smtpOptions.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.Host) ||
            string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
        {
            throw new InvalidOperationException("SMTP settings are missing. Configure SmtpSettings in appsettings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(toEmail);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
        };

        await client.SendMailAsync(message);
    }
}
