using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CapFinLoan.Application.Application.DTOs;
using CapFinLoan.Application.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Application.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly ILoanApplicationService _loanApplicationService;

    public ApplicationsController(ILoanApplicationService loanApplicationService)
    {
        _loanApplicationService = loanApplicationService;
    }

    [HttpPost]
    [Authorize(Roles = "APPLICANT")]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _loanApplicationService.CreateApplicationAsync(userId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "APPLICANT")]
    public async Task<IActionResult> UpdateApplication(Guid id, [FromBody] UpdateApplicationRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _loanApplicationService.UpdateApplicationAsync(userId, id, request);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "APPLICANT")]
    public async Task<IActionResult> SubmitApplication(Guid id, [FromBody] SubmitApplicationRequestDto request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _loanApplicationService.SubmitApplicationAsync(userId, id, request);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "APPLICANT")]
    public async Task<IActionResult> DeleteApplication(Guid id)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _loanApplicationService.DeleteApplicationAsync(userId, id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("my")]
    [Authorize(Roles = "APPLICANT")]
    public async Task<IActionResult> GetMyApplications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var (applications, totalCount) = await _loanApplicationService.GetMyApplicationsAsync(userId, pageNumber, pageSize);

            return Ok(new
            {
                pageNumber,
                pageSize,
                totalCount,
                items = applications
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/status")]
    [Authorize(Roles = "APPLICANT")]
    public async Task<IActionResult> GetApplicationStatus(Guid id)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var result = await _loanApplicationService.GetApplicationStatusAsync(userId, id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    private Guid GetUserIdFromToken()
    {
        var claimValue = User.FindFirstValue("UserId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(claimValue, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user identifier in token.");
        }

        return userId;
    }
}
