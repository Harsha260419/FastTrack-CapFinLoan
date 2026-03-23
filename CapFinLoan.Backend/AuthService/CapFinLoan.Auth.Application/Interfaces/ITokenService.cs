using CapFinLoan.Auth.Domain.Entities;

namespace CapFinLoan.Auth.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}