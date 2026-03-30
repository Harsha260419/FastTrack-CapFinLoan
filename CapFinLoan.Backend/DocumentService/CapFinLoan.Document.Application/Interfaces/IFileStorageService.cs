using Microsoft.AspNetCore.Http;

namespace CapFinLoan.Document.Application.Interfaces;

public interface IFileStorageService
{
    void ValidateFile(IFormFile file);
    Task<(string SavedFileName, string SavedFilePath)> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteFileIfExistsAsync(string filePath, CancellationToken cancellationToken = default);
}
