using CapFinLoan.Application.Application.DTOs;
using CapFinLoan.Application.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Application.API.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/applications")]
[Authorize]
public class InternalApplicationsController : ControllerBase
{
    private readonly ILoanApplicationService _loanApplicationService;

    public InternalApplicationsController(ILoanApplicationService loanApplicationService)
    {
        _loanApplicationService = loanApplicationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications([FromQuery] string? status)
    {
        try
        {
            var result = await _loanApplicationService.GetApplicationsForAdminAsync(status);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetApplicationById(Guid id)
    {
        try
        {
            var result = await _loanApplicationService.GetApplicationByIdForAdminAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetApplicationCurrentStatus(Guid id)
    {
        try
        {
            var result = await _loanApplicationService.GetApplicationByIdForAdminAsync(id);
            return Ok(new { currentStatus = result.Status });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(Guid id, [FromBody] UpdateApplicationStatusInternalRequestDto request)
    {
        try
        {
            var result = await _loanApplicationService.UpdateApplicationStatusInternalAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
