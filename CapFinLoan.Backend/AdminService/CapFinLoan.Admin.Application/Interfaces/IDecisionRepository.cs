using CapFinLoan.Admin.Domain.Entities;

namespace CapFinLoan.Admin.Application.Interfaces;

public interface IDecisionRepository
{
    Task<Decision?> GetByApplicationIdAsync(Guid applicationId);
    Task AddAsync(Decision decision);
    Task UpdateAsync(Decision decision);
    Task SaveChangesAsync();
}
