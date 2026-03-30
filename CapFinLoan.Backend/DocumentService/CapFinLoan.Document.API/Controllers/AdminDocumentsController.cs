using CapFinLoan.Document.API.Extensions;
using CapFinLoan.Document.Application.DTOs;
using CapFinLoan.Document.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Document.API.Controllers;

[ApiController]
[Route("admin/documents")]
[Authorize(Roles = "ADMIN")]
public class AdminDocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public AdminDocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
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
