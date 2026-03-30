using CapFinLoan.Admin.Application.Interfaces;
using CapFinLoan.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Admin.Persistence.Repositories;

public class DecisionRepository : IDecisionRepository
{
    private readonly AdminDbContext _dbContext;

    public DecisionRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Decision?> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _dbContext.Decisions
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
    }

    public async Task AddAsync(Decision decision)
    {
        await _dbContext.Decisions.AddAsync(decision);
    }

    public Task UpdateAsync(Decision decision)
    {
        _dbContext.Decisions.Update(decision);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
