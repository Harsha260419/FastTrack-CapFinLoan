using CapFinLoan.Document.API.Extensions;
using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Document.API.Controllers;

[ApiController]
[Route("documents")]
[Authorize(Roles = "APPLICANT")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.UploadDocumentAsync(userId, request, token, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Replace(Guid id, [FromForm] UploadDocumentDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.ReplaceDocumentAsync(userId, id, request, token, cancellationToken);
        return Ok(result);
    }

    [HttpGet("application/{applicationId:guid}")]
    public async Task<IActionResult> GetByApplicationId(Guid applicationId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.GetDocumentsByApplicationIdAsync(userId, applicationId, token, cancellationToken);
        return Ok(result);
    }
}
