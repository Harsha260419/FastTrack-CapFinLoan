using CapFinLoan.Auth.Application.DTOs;

namespace CapFinLoan.Auth.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> SignupAsync(SignupRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
}