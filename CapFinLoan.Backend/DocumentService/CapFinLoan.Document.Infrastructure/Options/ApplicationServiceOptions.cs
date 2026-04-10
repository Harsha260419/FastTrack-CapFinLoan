namespace CapFinLoan.Document.Infrastructure.Options;

public class ApplicationServiceOptions
{
    public const string SectionName = "ApplicationService";

    public const string HttpOnlyMode = "HttpOnly";
    public const string DualWriteMode = "DualWrite";
    public const string RabbitMqPrimaryMode = "RabbitMqPrimary";

    public string BaseUrl { get; set; } = string.Empty;
    public string ValidationPath { get; set; } = "/api/applications/{applicationId}/status";
    public string DocumentStatusPath { get; set; } = "/internal/applications/{applicationId}/status";
    public bool RequireValidation { get; set; } = true;
    public bool StatusSyncEnabled { get; set; } = true;
    public string StatusSyncMode { get; set; } = HttpOnlyMode;
}
