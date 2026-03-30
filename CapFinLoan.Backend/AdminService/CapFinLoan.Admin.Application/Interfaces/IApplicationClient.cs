using CapFinLoan.Admin.Application.DTOs;

namespace CapFinLoan.Admin.Application.Interfaces;

public interface IApplicationClient
{
    Task<IReadOnlyList<ApplicationServiceApplicationDto>> GetApplicationsAsync(string? status);
    Task<ApplicationServiceApplicationDto?> GetApplicationByIdAsync(Guid applicationId);
    Task<string?> GetCurrentStatusAsync(Guid applicationId);
    Task<bool> UpdateStatusAsync(Guid applicationId, string status, string? remarks);
}
