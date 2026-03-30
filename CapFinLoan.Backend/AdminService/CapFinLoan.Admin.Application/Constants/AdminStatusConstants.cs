namespace CapFinLoan.Admin.Application.Constants;

public static class AdminStatusConstants
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string DocsPending = "DOCS_PENDING";
    public const string DocsVerified = "DOCS_VERIFIED";
    public const string UnderReview = "UNDER_REVIEW";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Closed = "CLOSED";

    public static readonly HashSet<string> DecisionAllowed = new(StringComparer.Ordinal)
    {
        UnderReview,
        Approved,
        Rejected
    };

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var compact = status.Trim().Replace("-", "_").Replace(" ", "_");
        var upper = compact.ToUpperInvariant();

        return upper switch
        {
            "DRAFT" => Draft,
            "SUBMITTED" => Submitted,
            "DOCSPENDING" or "DOCS_PENDING" => DocsPending,
            "DOCSVERIFIED" or "DOCS_VERIFIED" => DocsVerified,
            "UNDERREVIEW" or "UNDER_REVIEW" => UnderReview,
            "APPROVED" => Approved,
            "REJECTED" => Rejected,
            "CLOSED" => Closed,
            _ => upper
        };
    }

    public static string ToApplicationServiceStatus(string normalizedStatus)
    {
        return normalizedStatus switch
        {
            Draft => "Draft",
            Submitted => "Submitted",
            DocsPending => "DocsPending",
            DocsVerified => "DocsVerified",
            UnderReview => "UnderReview",
            Approved => "Approved",
            Rejected => "Rejected",
            Closed => "Closed",
            _ => throw new ArgumentException($"Unsupported status: {normalizedStatus}")
        };
    }
}
