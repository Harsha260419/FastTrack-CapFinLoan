namespace CapFinLoan.Chat.API.Models;

public class LMStudioOptions
{
    public const string SectionName = "LMStudio";

    public string BaseUrl { get; set; } = "http://localhost:1234";
    public string Model { get; set; } = "qwen3-1.7b";
}

public class ApplicationServiceOptions
{
    public const string SectionName = "ApplicationService";

    public string BaseUrl { get; set; } = "http://applicationservice:5256";
}
