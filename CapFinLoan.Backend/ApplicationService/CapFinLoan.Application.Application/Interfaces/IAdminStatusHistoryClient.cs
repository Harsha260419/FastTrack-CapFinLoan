namespace CapFinLoan.Application.Application.Interfaces;

public interface IAdminStatusHistoryClient
{
    Task<IReadOnlyList<(string ToStatus, DateTime ChangedAt, string? Remarks)>> GetStatusHistoryAsync(Guid applicationId);
}
