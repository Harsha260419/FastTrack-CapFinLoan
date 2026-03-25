namespace CapFinLoan.Application.Application.DTOs;

public class CreateApplicationRequestDto
{
    public PersonalDetailsDto PersonalDetails { get; set; } = new();
    public EmploymentDetailsDto EmploymentDetails { get; set; } = new();
    public LoanDetailsDto LoanDetails { get; set; } = new();
}

public class PersonalDetailsDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class EmploymentDetailsDto
{
    public string EmployerName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public decimal AnnualIncome { get; set; }
    public decimal ExistingEmiAmount { get; set; }
}

public class LoanDetailsDto
{
    public decimal RequestedAmount { get; set; }
    public int RequestedTenureMonths { get; set; }
    public string LoanPurpose { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
