using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Auth.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}