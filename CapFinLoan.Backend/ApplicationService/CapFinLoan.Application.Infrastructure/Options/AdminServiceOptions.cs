namespace CapFinLoan.Application.Infrastructure.Options;

public class AdminServiceOptions
{
    public const string SectionName = "AdminService";

    public string BaseUrl { get; set; } = string.Empty;
    public string StatusHistoryPath { get; set; } = "/internal/admin/applications/{id}/history";
}
