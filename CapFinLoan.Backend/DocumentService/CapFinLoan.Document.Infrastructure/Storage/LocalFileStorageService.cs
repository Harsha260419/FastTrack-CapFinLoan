using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Document.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly string _uploadRoot;

    public LocalFileStorageService(IOptions<FileStorageOptions> options, IHostEnvironment hostEnvironment)
    {
        _options = options.Value;

        var configuredPath = _options.UploadRootPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = "Uploads";
        }

        _uploadRoot = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(hostEnvironment.ContentRootPath, configuredPath);

        Directory.CreateDirectory(_uploadRoot);
    }

    public void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ArgumentException("Uploaded file cannot be empty.");
        }

        var extension = Path.GetExtension(file.FileName);
        var allowedExtensions = _options.AllowedExtensions
            .Select(x => x.ToLowerInvariant())
            .ToHashSet();

        if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            throw new ArgumentException("Unsupported file type. Allowed types: PDF, JPG, JPEG, PNG.");
        }

        var maxAllowedBytes = _options.MaxFileSizeInMb * 1024L * 1024L;
        if (file.Length > maxAllowedBytes)
        {
            throw new ArgumentException($"File size exceeded. Maximum allowed size is {_options.MaxFileSizeInMb} MB.");
        }
    }

    public async Task<(string SavedFileName, string SavedFilePath)> SaveFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var generatedName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_uploadRoot, generatedName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return (generatedName, fullPath);
    }

    public Task DeleteFileIfExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
