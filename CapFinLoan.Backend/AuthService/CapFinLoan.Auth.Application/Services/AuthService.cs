using System.Text.RegularExpressions;
using CapFinLoan.Auth.Application.DTOs;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace CapFinLoan.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ISignupOtpRepository _signupOtpRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;

    private const int SignupOtpValidityMinutes = 10;
    private const int SignupOtpResendCooldownSeconds = 60;

    public AuthService(
        IUserRepository userRepository,
        ISignupOtpRepository signupOtpRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _signupOtpRepository = signupOtpRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailSender = emailSender;
    }

    public async Task<AuthResponseDto> SendSignupOtpAsync(SendSignupOtpRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email is required."
            };
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!IsValidEmail(normalizedEmail))
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email format." };
        }

        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            return new AuthResponseDto { Success = false, Message = "Email already registered." };
        }

        var latestOtp = await _signupOtpRepository.GetLatestByEmailAsync(normalizedEmail);
        if (latestOtp is not null)
        {
            var cooldownThreshold = DateTime.UtcNow.AddSeconds(-SignupOtpResendCooldownSeconds);
            if (latestOtp.CreatedAtUtc > cooldownThreshold)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Please wait before requesting another OTP."
                };
            }
        }

        var otpCode = GenerateOtpCode();
        var otpHash = HashOtp(otpCode);
        var otp = new SignupOtp
        {
            Email = normalizedEmail,
            OtpHash = otpHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(SignupOtpValidityMinutes),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _signupOtpRepository.AddAsync(otp);
        await _userRepository.SaveChangesAsync();

        var subject = "Your CapFinLoan signup OTP";
        var body = $"Your OTP is {otpCode}. It is valid for {SignupOtpValidityMinutes} minutes.";

        try
        {
            await _emailSender.SendAsync(normalizedEmail, subject, body);
        }
        catch
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Failed to send OTP email. Please try again later."
            };
        }

        return new AuthResponseDto
        {
            Success = true,
            Message = "OTP sent to your email."
        };
    }

    public async Task<AuthResponseDto> SignupAsync(SignupRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Name, email, phone number, password, and OTP are required."
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

        var latestOtp = await _signupOtpRepository.GetLatestByEmailAsync(normalizedEmail);
        if (latestOtp is null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "No OTP found for this email. Request a new OTP first."
            };
        }

        if (latestOtp.IsUsed)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "OTP already used. Request a new OTP."
            };
        }

        if (latestOtp.ExpiresAtUtc < DateTime.UtcNow)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "OTP expired. Request a new OTP."
            };
        }

        var submittedOtpHash = HashOtp(request.OtpCode.Trim());
        if (!string.Equals(latestOtp.OtpHash, submittedOtpHash, StringComparison.Ordinal))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid OTP code."
            };
        }

        latestOtp.IsUsed = true;
        _signupOtpRepository.Update(latestOtp);

        var role = NormalizeRole(request.Role);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
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

    private static string GenerateOtpCode()
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000);
        return otp.ToString();
    }

    private static string HashOtp(string otpCode)
    {
        var bytes = Encoding.UTF8.GetBytes(otpCode);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}