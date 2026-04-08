using CapFinLoan.Application.Domain.Enums;

namespace CapFinLoan.Application.Application.Interfaces;

public interface ILoanApplicationRepository
{
    Task AddAsync(Domain.Entities.LoanApplication application);
    Task UpdateAsync(Domain.Entities.LoanApplication application);
    Task DeleteAsync(Domain.Entities.LoanApplication application);
    Task<Domain.Entities.LoanApplication?> GetByIdAsync(Guid applicationId);
    Task<List<Domain.Entities.LoanApplication>> GetByUserIdAsync(Guid userId);
    Task<(List<Domain.Entities.LoanApplication>, int TotalCount)> GetByUserIdPaginatedAsync(Guid userId, int pageNumber, int pageSize);
    Task<bool> ExistsByIdAndUserIdAsync(Guid applicationId, Guid userId);
    Task<List<Domain.Entities.LoanApplication>> GetByStatusAsync(ApplicationStatus status);
    Task<List<(string ToStatus, DateTime ChangedAt, string? Remarks)>> GetStatusHistoryByApplicationIdAsync(Guid applicationId);
    Task SaveChangesAsync();
}
