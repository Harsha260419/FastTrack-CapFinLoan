namespace CapFinLoan.Notification.API.Services;

public interface IEmailSender
{
    Task SendOtpEmailAsync(
        string applicantName,
        string email,
        string otpCode,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task SendDecisionEmailAsync(
        string applicantName,
        string applicantEmail,
        string decision,
        string remarks,
        decimal sanctionAmount,
        double interestRate,
        CancellationToken cancellationToken = default);
}
