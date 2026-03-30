namespace CapFinLoan.Auth.Domain.Entities;

public static class UserRoles
{
    public const string Admin = "ADMIN";
    public const string Applicant = "APPLICANT";

    public static readonly string[] All = [Admin, Applicant];
}