using CapFinLoan.Auth.Application.DTOs;

namespace CapFinLoan.Auth.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> SendSignupOtpAsync(SendSignupOtpRequestDto request);
    Task<AuthResponseDto> SignupAsync(SignupRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<GoogleAuthResultDto> GoogleLoginAsync(GoogleAuthRequestDto request);
    Task<ProfileResponseDto?> GetProfileAsync(Guid userId);
    Task<SimpleResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
    Task<SimpleResponseDto> UpdatePasswordAsync(Guid userId, UpdatePasswordRequestDto request);
}