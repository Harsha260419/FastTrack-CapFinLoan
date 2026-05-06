namespace CapFinLoan.Auth.Application.DTOs;

public enum GoogleAuthErrorType
{
    None,
    InvalidToken,
    LocalAccountExists,
    Unknown,
}

public class GoogleAuthResultDto
{
    public bool Success { get; set; }
    public GoogleAuthErrorType ErrorType { get; set; } = GoogleAuthErrorType.None;
    public string Message { get; set; } = string.Empty;
    public AuthResponseDto? AuthResponse { get; set; }
}
