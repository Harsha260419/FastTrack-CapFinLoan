namespace CapFinLoan.Admin.Infrastructure.Options;

public class DocumentServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string GetByIdPath { get; set; } = "/internal/admin/documents/{id}";
    public string VerifyPath { get; set; } = "/internal/admin/documents/{id}/verify";
}