namespace CapFinLoan.Admin.Application.DTOs;

public class AdminDashboardDto
{
    public int TotalApplications { get; set; }
    public int SubmittedCount { get; set; }
    public int DocsPendingCount { get; set; }
    public int DocsVerifiedCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}
