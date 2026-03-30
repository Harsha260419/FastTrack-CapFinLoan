namespace CapFinLoan.Admin.Infrastructure.Options;

public class ApplicationServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string QueuePath { get; set; } = "/internal/applications";
    public string DetailsPath { get; set; } = "/internal/applications/{id}";
    public string StatusPath { get; set; } = "/internal/applications/{id}/status";
}
