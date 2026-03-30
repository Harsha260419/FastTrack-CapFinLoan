using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Admin.Persistence.Repositories;

public class StatusHistoryRepository : IStatusHistoryRepository
{
    private readonly AdminDbContext _dbContext;

    public StatusHistoryRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ApplicationStatusHistory historyItem)
    {
        await _dbContext.ApplicationStatusHistories.AddAsync(historyItem);
    }

    public async Task<IReadOnlyList<ApplicationStatusHistory>> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _dbContext.ApplicationStatusHistories
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
