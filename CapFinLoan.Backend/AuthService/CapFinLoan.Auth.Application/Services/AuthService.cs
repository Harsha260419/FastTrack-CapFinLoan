using System.Text.RegularExpressions;
using CapFinLoan.Auth.Application.DTOs;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Entities;

namespace CapFinLoan.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> SignupAsync(SignupRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Name, email, and password are required."
            };
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!IsValidEmail(normalizedEmail))
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email format." };
        }

        if (!IsStrongPassword(request.Password))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Password must be at least 8 characters and include upper, lower, number, and special character."
            };
        }

        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            return new AuthResponseDto { Success = false, Message = "Email already registered." };
        }

        var role = NormalizeRole(request.Role);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Account created successfully.",
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = role
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResponseDto { Success = false, Message = "Email and password are required." };
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user is null)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }

        if (!user.IsActive)
        {
            return new AuthResponseDto { Success = false, Message = "User account is inactive." };
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
        }

        var normalizedRole = NormalizeRole(user.Role);
        var token = _tokenService.GenerateToken(user);
        return new AuthResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = normalizedRole
        };
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    }

    private static bool IsStrongPassword(string password)
    {
        var hasMinLength = password.Length >= 8;
        var hasUpper = Regex.IsMatch(password, "[A-Z]");
        var hasLower = Regex.IsMatch(password, "[a-z]");
        var hasDigit = Regex.IsMatch(password, "[0-9]");
        var hasSpecial = Regex.IsMatch(password, "[^A-Za-z0-9]");

        return hasMinLength && hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static string NormalizeRole(string? requestedRole)
    {
        if (string.Equals(requestedRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return UserRoles.Admin;
        }

        return UserRoles.Applicant;
    }
}