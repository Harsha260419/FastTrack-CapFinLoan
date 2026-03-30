namespace CapFinLoan.Document.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string UploadRootPath { get; set; } = "Uploads";
    public int MaxFileSizeInMb { get; set; } = 5;
    public List<string> AllowedExtensions { get; set; } = [".pdf", ".jpg", ".jpeg", ".png"];
}
