using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Auth.Persistence.Repositories;

public class SignupOtpRepository : ISignupOtpRepository
{
    private readonly AuthDbContext _context;

    public SignupOtpRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<SignupOtp?> GetLatestByEmailAsync(string email)
    {
        return await _context.SignupOtps
            .Where(x => x.Email == email)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(SignupOtp signupOtp)
    {
        await _context.SignupOtps.AddAsync(signupOtp);
    }

    public void Update(SignupOtp signupOtp)
    {
        _context.SignupOtps.Update(signupOtp);
    }
}
