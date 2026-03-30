using CapFinLoan.Document.API.Extensions;
using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Document.API.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/admin/documents")]
[Authorize(Roles = "ADMIN")]
public class InternalAdminDocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public InternalAdminDocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyDocumentDto request, CancellationToken cancellationToken)
    {
        var adminUserId = User.GetUserId();
        var token = Request.Headers.Authorization.ToString();
        var result = await _documentService.VerifyDocumentAsync(adminUserId, id, request, token, cancellationToken);
        return Ok(result);
    }
}
