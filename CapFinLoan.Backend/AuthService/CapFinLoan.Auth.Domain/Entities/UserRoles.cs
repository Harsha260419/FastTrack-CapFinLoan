namespace CapFinLoan.Auth.Domain.Entities;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Applicant = "Applicant";

    public static readonly string[] All = [Admin, Applicant];
}