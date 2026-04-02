using CapFinLoan.Auth.Domain.Entities;

namespace CapFinLoan.Auth.Application.Interfaces;

public interface ISignupOtpRepository
{
    Task<SignupOtp?> GetLatestByEmailAsync(string email);
    Task AddAsync(SignupOtp signupOtp);
    void Update(SignupOtp signupOtp);
}
