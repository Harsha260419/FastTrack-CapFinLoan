namespace CapFinLoan.Auth.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? AuthProvider { get; set; }
    public string? GoogleId { get; set; }
    public string Role { get; set; } = UserRoles.Applicant;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}