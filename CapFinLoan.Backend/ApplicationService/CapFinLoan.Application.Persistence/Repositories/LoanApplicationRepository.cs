using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Domain.Entities;
using CapFinLoan.Application.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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

    public async Task<List<(string ToStatus, DateTime ChangedAt, string? Remarks)>> GetStatusHistoryByApplicationIdAsync(Guid applicationId)
    {
        const string sql = @"
SELECT ToStatus, ChangedAt, Remarks
FROM [admin].[ApplicationStatusHistory]
WHERE ApplicationId = @applicationId
ORDER BY ChangedAt ASC";

        try
        {
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var applicationIdParameter = new SqlParameter("@applicationId", applicationId);
            command.Parameters.Add(applicationIdParameter);

            var result = new List<(string ToStatus, DateTime ChangedAt, string? Remarks)>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var toStatus = reader.GetString(0);
                var changedAt = reader.GetDateTime(1);
                var remarks = reader.IsDBNull(2) ? null : reader.GetString(2);
                result.Add((toStatus, changedAt, remarks));
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
