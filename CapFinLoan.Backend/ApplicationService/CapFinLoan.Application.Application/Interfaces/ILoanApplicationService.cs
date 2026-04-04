using CapFinLoan.Application.Application.DTOs;

namespace CapFinLoan.Application.Application.Interfaces;

public interface ILoanApplicationService
{
    Task<ApplicationResponseDto> CreateApplicationAsync(Guid userId, CreateApplicationRequestDto request);
    Task<ApplicationResponseDto> UpdateApplicationAsync(Guid userId, Guid applicationId, UpdateApplicationRequestDto request);
    Task<ApplicationResponseDto> SubmitApplicationAsync(Guid userId, Guid applicationId, SubmitApplicationRequestDto request);
    Task<ApplicationResponseDto> DeleteApplicationAsync(Guid userId, Guid applicationId);
    Task<(List<ApplicationResponseDto> Applications, int TotalCount)> GetMyApplicationsAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
    Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid userId, Guid applicationId);
    Task<ApplicationDetailsResponseDto> GetApplicationDetailsAsync(Guid requesterUserId, bool isAdmin, Guid applicationId);
    Task<IReadOnlyList<ApplicationResponseDto>> GetApplicationsForAdminAsync(string? status);
    Task<ApplicationResponseDto> GetApplicationByIdForAdminAsync(Guid applicationId);
    Task<ApplicationResponseDto> UpdateApplicationStatusInternalAsync(Guid applicationId, UpdateApplicationStatusInternalRequestDto request);
}
