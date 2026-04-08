using CapFinLoan.Document.API.Extensions;
using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CapFinLoan.Document.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [Authorize(Roles = "APPLICANT")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.UploadDocumentAsync(userId, request, token, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "APPLICANT")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Replace(Guid id, [FromForm] UploadDocumentDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.ReplaceDocumentAsync(userId, id, request, token, cancellationToken);
        return Ok(result);
    }

    [HttpGet("application/{applicationId:guid}")]
    [Authorize(Roles = "APPLICANT,ADMIN")]
    public async Task<IActionResult> GetByApplicationId(Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = string.Equals(User.FindFirstValue("Role"), "ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(User.FindFirstValue(ClaimTypes.Role), "ADMIN", StringComparison.OrdinalIgnoreCase);
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.GetDocumentsByApplicationIdAsync(userId, isAdmin, applicationId, token, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/file")]
    [Authorize(Roles = "APPLICANT,ADMIN")]
    public async Task<IActionResult> GetDocumentFile(Guid id, CancellationToken cancellationToken)
    {
        DocumentResponseDto? document;

        try
        {
            document = await _documentService.GetDocumentByIdAsync(id, cancellationToken);
            Console.WriteLine($"[DocumentFileDebug] DocumentFoundInDb=True DocumentId={id}");
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine($"[DocumentFileDebug] DocumentFoundInDb=False DocumentId={id}");
            throw;
        }

        var filePath = document.FilePath;
        Console.WriteLine($"[DocumentFileDebug] DocumentId={id} FilePathFromDb='{filePath}'");

        var fileExists = !string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath);
        Console.WriteLine($"[DocumentFileDebug] DocumentId={id} FileExists={fileExists}");

        if (!fileExists)
        {
            return NotFound(new { message = "Stored document file not found." });
        }

        var contentType = GetContentTypeFromExtension(filePath);
        return PhysicalFile(filePath, contentType);
    }

    private static string GetContentTypeFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
