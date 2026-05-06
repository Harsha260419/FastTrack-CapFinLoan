using System.Text.Json;
using System.Text.RegularExpressions;
using CapFinLoan.Auth.Application.DTOs;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Application.Options;
using CapFinLoan.Auth.Domain.Entities;
using CapFinLoan.Messaging.Contracts;
using MassTransit;
using System.Security.Cryptography;
using System.Text;

namespace CapFinLoan.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ISignupOtpRepository _signupOtpRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleOptions _googleOptions;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private const int SignupOtpValidityMinutes = 10;
    private const int SignupOtpResendCooldownSeconds = 60;

    public AuthService(
        IUserRepository userRepository,
        ISignupOtpRepository signupOtpRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IPublishEndpoint publishEndpoint,
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Options.IOptions<GoogleOptions> googleOptions)
    {
        _userRepository = userRepository;
        _signupOtpRepository = signupOtpRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _publishEndpoint = publishEndpoint;
        _httpClientFactory = httpClientFactory;
        _googleOptions = googleOptions.Value;
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

        await _publishEndpoint.Publish(new OtpRequestedEvent
        {
            Email = normalizedEmail,
            OtpCode = otpCode,
            ApplicantName = BuildApplicantName(normalizedEmail),
            ExpiresAt = otp.ExpiresAtUtc
        });

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
            AuthProvider = AuthProviders.Local,
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

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email or password." };
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

    public async Task<GoogleAuthResultDto> GoogleLoginAsync(GoogleAuthRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return new GoogleAuthResultDto
            {
                Success = false,
                ErrorType = GoogleAuthErrorType.InvalidToken,
                Message = "Invalid Google token."
            };
        }

        var tokenInfo = await GetGoogleTokenInfoAsync(request.IdToken.Trim());
        if (tokenInfo is null || string.IsNullOrWhiteSpace(tokenInfo.Aud))
        {
            return new GoogleAuthResultDto
            {
                Success = false,
                ErrorType = GoogleAuthErrorType.InvalidToken,
                Message = "Invalid Google token."
            };
        }

        if (!string.Equals(tokenInfo.Aud, _googleOptions.ClientId, StringComparison.Ordinal))
        {
            return new GoogleAuthResultDto
            {
                Success = false,
                ErrorType = GoogleAuthErrorType.InvalidToken,
                Message = "Invalid Google token."
            };
        }

        if (string.IsNullOrWhiteSpace(tokenInfo.Email) || string.IsNullOrWhiteSpace(tokenInfo.Sub))
        {
            return new GoogleAuthResultDto
            {
                Success = false,
                ErrorType = GoogleAuthErrorType.InvalidToken,
                Message = "Invalid Google token."
            };
        }

        var normalizedEmail = tokenInfo.Email.Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            var provider = NormalizeAuthProvider(existingUser.AuthProvider);
            if (provider == AuthProviders.Google)
            {
                existingUser.GoogleId = tokenInfo.Sub;
                await _userRepository.SaveChangesAsync();

                var token = _tokenService.GenerateToken(existingUser);
                return new GoogleAuthResultDto
                {
                    Success = true,
                    AuthResponse = new AuthResponseDto
                    {
                        Success = true,
                        Message = "Login successful.",
                        Token = token,
                        UserId = existingUser.Id,
                        Email = existingUser.Email,
                        Role = NormalizeRole(existingUser.Role)
                    }
                };
            }

            return new GoogleAuthResultDto
            {
                Success = false,
                ErrorType = GoogleAuthErrorType.LocalAccountExists,
                Message = "An account with this email already exists. Please login with password."
            };
        }

        var newUser = new User
        {
            Name = string.IsNullOrWhiteSpace(tokenInfo.Name) ? BuildApplicantName(normalizedEmail) : tokenInfo.Name.Trim(),
            Email = normalizedEmail,
            PhoneNumber = string.Empty,
            PasswordHash = null,
            Role = UserRoles.Applicant,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            AuthProvider = AuthProviders.Google,
            GoogleId = tokenInfo.Sub
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        var newToken = _tokenService.GenerateToken(newUser);
        return new GoogleAuthResultDto
        {
            Success = true,
            AuthResponse = new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = newToken,
                UserId = newUser.Id,
                Email = newUser.Email,
                Role = UserRoles.Applicant
            }
        };
    }

    public async Task<ProfileResponseDto?> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        return new ProfileResponseDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = NormalizeRole(user.Role),
            CreatedAt = user.CreatedAt,
            AuthProvider = NormalizeAuthProvider(user.AuthProvider)
        };
    }

    public async Task<SimpleResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "Name is required."
            };
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "User not found."
            };
        }

        user.Name = request.Name.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        await _userRepository.SaveChangesAsync();

        return new SimpleResponseDto
        {
            Success = true,
            Message = "Profile updated successfully"
        };
    }

    public async Task<SimpleResponseDto> UpdatePasswordAsync(Guid userId, UpdatePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "Current password and new password are required."
            };
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "User not found."
            };
        }

        if (NormalizeAuthProvider(user.AuthProvider) == AuthProviders.Google)
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "Password change not available for Google accounts"
            };
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "Password change not available for this account."
            };
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!isPasswordValid)
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "Current password is incorrect."
            };
        }

        if (!IsStrongPassword(request.NewPassword))
        {
            return new SimpleResponseDto
            {
                Success = false,
                Message = "Password must be at least 8 characters and include upper, lower, number, and special character."
            };
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        await _userRepository.SaveChangesAsync();

        return new SimpleResponseDto
        {
            Success = true,
            Message = "Password updated successfully"
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

    private static string NormalizeAuthProvider(string? provider)
    {
        if (string.Equals(provider, AuthProviders.Google, StringComparison.OrdinalIgnoreCase))
        {
            return AuthProviders.Google;
        }

        return AuthProviders.Local;
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

    private static string BuildApplicantName(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return "Applicant";
        }

        var localPart = email[..atIndex].Trim();
        if (string.IsNullOrWhiteSpace(localPart))
        {
            return "Applicant";
        }

        return localPart;
    }

    private async Task<GoogleTokenInfo?> GetGoogleTokenInfoAsync(string idToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GoogleTokenInfo>(payload, _jsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed class GoogleTokenInfo
    {
        public string? Sub { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Aud { get; set; }
    }
}