namespace CapFinLoan.Notification.API.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "CapFinLoan";
    public string AppPassword { get; set; } = string.Empty;
}
