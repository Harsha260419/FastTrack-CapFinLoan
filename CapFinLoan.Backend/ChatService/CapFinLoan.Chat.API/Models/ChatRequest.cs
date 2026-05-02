namespace CapFinLoan.Chat.API.Models;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? AuthToken { get; set; }
    public List<ChatMessage> ConversationHistory { get; set; } = [];
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public List<ChatApplicationSummary>? Applications { get; set; }
}

public class ChatApplicationSummary
{
    public string ApplicationId { get; set; } = string.Empty;
    public string ShortId { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public string LoanPurpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
