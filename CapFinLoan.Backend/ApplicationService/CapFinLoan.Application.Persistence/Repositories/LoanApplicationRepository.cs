using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Domain.Entities;
using CapFinLoan.Application.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Application.Persistence.Repositories;

public class LoanApplicationRepository : ILoanApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public LoanApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoanApplication application)
    {
        await _context.LoanApplications.AddAsync(application);
    }

    public Task UpdateAsync(LoanApplication application)
    {
        _context.LoanApplications.Update(application);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(LoanApplication application)
    {
        _context.LoanApplications.Remove(application);
        return Task.CompletedTask;
    }

    public async Task<LoanApplication?> GetByIdAsync(Guid applicationId)
    {
        return await _context.LoanApplications
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
    }

    public async Task<List<LoanApplication>> GetByUserIdAsync(Guid userId)
    {
        return await _context.LoanApplications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<LoanApplication>, int TotalCount)> GetByUserIdPaginatedAsync(Guid userId, int pageNumber, int pageSize)
    {
        var query = _context.LoanApplications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsByIdAndUserIdAsync(Guid applicationId, Guid userId)
    {
        return await _context.LoanApplications
            .AnyAsync(x => x.ApplicationId == applicationId && x.UserId == userId);
    }

    public async Task<List<LoanApplication>> GetByStatusAsync(ApplicationStatus status)
    {
        return await _context.LoanApplications
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
