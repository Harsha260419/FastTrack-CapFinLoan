namespace CapFinLoan.Application.Application.DTOs;

public class UpdateApplicationRequestDto
{
    public PersonalDetailsDto PersonalDetails { get; set; } = new();
    public EmploymentDetailsDto EmploymentDetails { get; set; } = new();
    public LoanDetailsDto LoanDetails { get; set; } = new();
}
