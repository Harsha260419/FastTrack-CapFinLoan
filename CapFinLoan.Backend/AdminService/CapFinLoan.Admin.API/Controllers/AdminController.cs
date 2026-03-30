using CapFinLoan.Admin.Application.DTOs;
using CapFinLoan.Admin.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Admin.API.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications()
    {
        try
        {
            var result = await _adminService.GetApplicationsAsync();
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("applications/{id:guid}")]
    public async Task<IActionResult> GetApplicationById(Guid id)
    {
        try
        {
            var result = await _adminService.GetApplicationByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpPost("applications/{id:guid}/decision")]
    public async Task<IActionResult> CreateDecision(Guid id, [FromBody] CreateDecisionRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var adminUserId))
            {
                return Unauthorized(new { message = "Invalid or missing admin user identifier in token." });
            }

            var adminIdentity = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "ADMIN";
            var result = await _adminService.CreateDecisionAsync(id, request, adminUserId, adminIdentity);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var result = await _adminService.GetDashboardAsync();
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("applications/{id:guid}/status")]
    public async Task<IActionResult> GetApplicationStatus(Guid id)
    {
        try
        {
            var result = await _adminService.GetApplicationStatusAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("applications/{id:guid}/history")]
    public async Task<IActionResult> GetApplicationHistory(Guid id)
    {
        try
        {
            var result = await _adminService.GetApplicationStatusHistoryAsync(id);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpPut("documents/{id:guid}/verify")]
    public async Task<IActionResult> VerifyDocument(Guid id, [FromBody] VerifyDocumentRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var adminUserId))
            {
                return Unauthorized(new { message = "Invalid or missing admin user identifier in token." });
            }

            var adminIdentity = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "ADMIN";
            var result = await _adminService.VerifyDocumentAsync(id, request, adminUserId, adminIdentity);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
