using CapFinLoan.Admin.Application.DTOs;

namespace CapFinLoan.Admin.Application.Interfaces;

public interface IAdminService
{
    Task<IReadOnlyList<AdminApplicationQueueItemDto>> GetApplicationsAsync();
    Task<AdminApplicationDetailsDto> GetApplicationByIdAsync(Guid applicationId);
    Task<ApplicationStatusResponseDto> GetApplicationStatusAsync(Guid applicationId);
    Task<IReadOnlyList<ApplicationStatusHistoryItemDto>> GetApplicationStatusHistoryAsync(Guid applicationId);
    Task<DecisionResponseDto> CreateDecisionAsync(Guid applicationId, CreateDecisionRequestDto request, Guid adminUserId, string adminIdentity);
    Task<AdminDashboardDto> GetDashboardAsync();
}
