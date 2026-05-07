using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CapFinLoan.Auth.Application.DTOs;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Application.Options;
using CapFinLoan.Auth.Application.Services;
using CapFinLoan.Auth.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace CapFinLoan.Auth.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<ISignupOtpRepository> _signupOtpRepository = null!;
    private Mock<IPasswordHasher> _passwordHasher = null!;
    private Mock<ITokenService> _tokenService = null!;
    private Mock<IPublishEndpoint> _publishEndpoint = null!;
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private IOptions<GoogleOptions> _googleOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        _signupOtpRepository = new Mock<ISignupOtpRepository>(MockBehavior.Strict);
        _passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        _tokenService = new Mock<ITokenService>(MockBehavior.Strict);
        _publishEndpoint = new Mock<IPublishEndpoint>(MockBehavior.Loose);
        _httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        _googleOptions = Options.Create(new GoogleOptions { ClientId = "test-client" });
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = true, Role = UserRoles.Applicant };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyPassword("P@ssword1", "hash")).Returns(true);
        _tokenService.Setup(x => x.GenerateToken(user)).Returns("jwt-token");

        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "P@ssword1" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Token, Is.EqualTo("jwt-token"));

        _userRepository.VerifyAll();
        _passwordHasher.VerifyAll();
        _tokenService.VerifyAll();
    }

    [Test]
    public async Task Login_WithInvalidPassword_ReturnsFailure()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = true, Role = UserRoles.Applicant };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyPassword("Bad", "hash")).Returns(false);

        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "Bad" });

        Assert.That(result.Success, Is.False);

        _userRepository.VerifyAll();
        _passwordHasher.VerifyAll();
    }

    [Test]
    public async Task Login_WithNonExistentEmail_ReturnsFailure()
    {
        _userRepository.Setup(x => x.GetByEmailAsync("missing@example.com")).ReturnsAsync((User?)null);

        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto { Email = "missing@example.com", Password = "P@ssword1" });

        Assert.That(result.Success, Is.False);

        _userRepository.VerifyAll();
    }

    [Test]
    public async Task Login_WithInactiveAccount_ReturnsFailure()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = false };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto { Email = "user@example.com", Password = "P@ssword1" });

        Assert.That(result.Success, Is.False);

        _userRepository.VerifyAll();
    }

    [Test]
    public async Task Signup_WithValidOtp_CreatesUserAndReturnsSuccess()
    {
        var otpCode = "123456";
        var otpHash = HashOtp(otpCode);
        var otp = new SignupOtp
        {
            Email = "user@example.com",
            OtpHash = otpHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        User? createdUser = null;

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        _signupOtpRepository.Setup(x => x.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(otp);
        _signupOtpRepository.Setup(x => x.Update(otp));
        _passwordHasher.Setup(x => x.HashPassword("P@ssword1")).Returns("hashed");
        _userRepository.Setup(x => x.AddAsync(It.IsAny<User>())).Callback<User>(user => createdUser = user).Returns(Task.CompletedTask);
        _userRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tokenService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("jwt-token");

        var service = CreateService();

        var result = await service.SignupAsync(new SignupRequestDto
        {
            Name = "Test User",
            Email = "user@example.com",
            PhoneNumber = "9999999999",
            Password = "P@ssword1",
            Role = "APPLICANT",
            OtpCode = otpCode
        });

        Assert.That(result.Success, Is.True);
        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser!.AuthProvider, Is.EqualTo(AuthProviders.Local));

        _userRepository.VerifyAll();
        _signupOtpRepository.VerifyAll();
        _passwordHasher.VerifyAll();
        _tokenService.VerifyAll();
    }

    [Test]
    public async Task Signup_WithExpiredOtp_ReturnsFailure()
    {
        var otp = new SignupOtp
        {
            Email = "user@example.com",
            OtpHash = HashOtp("123456"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        _signupOtpRepository.Setup(x => x.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(otp);

        var service = CreateService();

        var result = await service.SignupAsync(new SignupRequestDto
        {
            Name = "Test User",
            Email = "user@example.com",
            PhoneNumber = "9999999999",
            Password = "P@ssword1",
            Role = "APPLICANT",
            OtpCode = "123456"
        });

        Assert.That(result.Success, Is.False);

        _userRepository.VerifyAll();
        _signupOtpRepository.VerifyAll();
    }

    [Test]
    public async Task Signup_WithInvalidOtp_ReturnsFailure()
    {
        var otp = new SignupOtp
        {
            Email = "user@example.com",
            OtpHash = HashOtp("999999"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        _signupOtpRepository.Setup(x => x.GetLatestByEmailAsync("user@example.com")).ReturnsAsync(otp);

        var service = CreateService();

        var result = await service.SignupAsync(new SignupRequestDto
        {
            Name = "Test User",
            Email = "user@example.com",
            PhoneNumber = "9999999999",
            Password = "P@ssword1",
            Role = "APPLICANT",
            OtpCode = "123456"
        });

        Assert.That(result.Success, Is.False);

        _userRepository.VerifyAll();
        _signupOtpRepository.VerifyAll();
    }

    [Test]
    public async Task Signup_WithAlreadyRegisteredEmail_ReturnsFailure()
    {
        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(new User());

        var service = CreateService();

        var result = await service.SignupAsync(new SignupRequestDto
        {
            Name = "Test User",
            Email = "user@example.com",
            PhoneNumber = "9999999999",
            Password = "P@ssword1",
            Role = "APPLICANT",
            OtpCode = "123456"
        });

        Assert.That(result.Success, Is.False);

        _userRepository.VerifyAll();
    }

    [Test]
    public async Task GoogleLogin_WithNewUser_CreatesAccountAndReturnsToken()
    {
        var tokenInfo = new { sub = "google-sub", email = "user@example.com", name = "Google User", aud = "test-client" };
        ConfigureHttpClient(tokenInfo);

        User? createdUser = null;

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        _userRepository.Setup(x => x.AddAsync(It.IsAny<User>())).Callback<User>(user => createdUser = user).Returns(Task.CompletedTask);
        _userRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tokenService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("jwt-token");

        var service = CreateService();

        var result = await service.GoogleLoginAsync(new GoogleAuthRequestDto { IdToken = "token" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.AuthResponse?.Token, Is.EqualTo("jwt-token"));
        Assert.That(createdUser?.AuthProvider, Is.EqualTo(AuthProviders.Google));

        _userRepository.VerifyAll();
        _tokenService.VerifyAll();
    }

    [Test]
    public async Task GoogleLogin_WithExistingGoogleUser_ReturnsToken()
    {
        var tokenInfo = new { sub = "google-sub", email = "user@example.com", name = "Google User", aud = "test-client" };
        ConfigureHttpClient(tokenInfo);

        var existingUser = new User
        {
            Email = "user@example.com",
            AuthProvider = AuthProviders.Google,
            Role = UserRoles.Applicant,
            IsActive = true
        };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(existingUser);
        _userRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tokenService.Setup(x => x.GenerateToken(existingUser)).Returns("jwt-token");

        var service = CreateService();

        var result = await service.GoogleLoginAsync(new GoogleAuthRequestDto { IdToken = "token" });

        Assert.That(result.Success, Is.True);
        Assert.That(result.AuthResponse?.Token, Is.EqualTo("jwt-token"));

        _userRepository.VerifyAll();
        _tokenService.VerifyAll();
    }

    [Test]
    public async Task GoogleLogin_WithExistingLocalUser_ReturnsFailure()
    {
        var tokenInfo = new { sub = "google-sub", email = "user@example.com", name = "Google User", aud = "test-client" };
        ConfigureHttpClient(tokenInfo);

        var existingUser = new User
        {
            Email = "user@example.com",
            AuthProvider = AuthProviders.Local,
            Role = UserRoles.Applicant,
            IsActive = true
        };

        _userRepository.Setup(x => x.GetByEmailAsync("user@example.com")).ReturnsAsync(existingUser);

        var service = CreateService();

        var result = await service.GoogleLoginAsync(new GoogleAuthRequestDto { IdToken = "token" });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorType, Is.EqualTo(GoogleAuthErrorType.LocalAccountExists));

        _userRepository.VerifyAll();
    }

    private AuthService CreateService()
    {
        return new AuthService(
            _userRepository.Object,
            _signupOtpRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _publishEndpoint.Object,
            _httpClientFactory.Object,
            _googleOptions);
    }

    private void ConfigureHttpClient(object tokenInfo)
    {
        var json = JsonSerializer.Serialize(tokenInfo);
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(handler.Object);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    private static string HashOtp(string otpCode)
    {
        var bytes = Encoding.UTF8.GetBytes(otpCode);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
