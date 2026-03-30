using CapFinLoan.Admin.Domain.Entities;

namespace CapFinLoan.Admin.Application.Interfaces;

public interface IStatusHistoryRepository
{
    Task AddAsync(ApplicationStatusHistory historyItem);
    Task<IReadOnlyList<ApplicationStatusHistory>> GetByApplicationIdAsync(Guid applicationId);
    Task SaveChangesAsync();
}
