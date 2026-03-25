namespace CapFinLoan.Application.Domain.Enums;

public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    DocsPending = 3,
    DocsVerified = 4,
    UnderReview = 5,
    Approved = 6,
    Rejected = 7,
    Closed = 8
}
