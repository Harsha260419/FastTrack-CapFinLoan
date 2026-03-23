using CapFinLoan.Auth.Domain.Entities;

namespace CapFinLoan.Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}