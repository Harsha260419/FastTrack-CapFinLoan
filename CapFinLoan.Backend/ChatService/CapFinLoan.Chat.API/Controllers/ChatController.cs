using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CapFinLoan.Chat.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CapFinLoan.Chat.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LMStudioOptions _lmStudioOptions;
    private readonly ApplicationServiceOptions _applicationServiceOptions;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ChatController(
        IHttpClientFactory httpClientFactory,
        IOptions<LMStudioOptions> lmStudioOptions,
        IOptions<ApplicationServiceOptions> applicationServiceOptions)
    {
        _httpClientFactory = httpClientFactory;
        _lmStudioOptions = lmStudioOptions.Value;
        _applicationServiceOptions = applicationServiceOptions.Value;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ChatResponse { Reply = "Please provide a message." });
        }

        string? applicationDataJson = null;
        var applicationFetchFailed = false;
        List<ChatApplicationSummary>? responseApplications = null;

        var intent = DetectIntent(request.Message, out var statusFilter, out var applicationId);
        if (intent != ChatIntent.General)
        {
            if (string.IsNullOrWhiteSpace(request.AuthToken))
            {
                applicationFetchFailed = true;
            }
            else
            {
                try
                {
                    switch (intent)
                    {
                        case ChatIntent.SpecificApplication:
                        {
                            var appJson = await FetchApplicationByIdAsync(applicationId!, request.AuthToken, cancellationToken);
                            if (!string.IsNullOrWhiteSpace(appJson))
                            {
                                applicationDataJson = appJson;
                                var summary = BuildApplicationSummaryFromJson(appJson);
                                if (summary != null)
                                {
                                    responseApplications = [summary];
                                }
                            }
                            else
                            {
                                applicationFetchFailed = true;
                            }

                            break;
                        }
                        case ChatIntent.LatestApplication:
                        {
                            var appsJson = await FetchApplicationsAsync(request.AuthToken, cancellationToken);
                            var summaries = BuildApplicationSummariesFromJson(appsJson);
                            var latest = summaries
                                .OrderByDescending(summary => ParseCreatedAt(summary.CreatedAt) ?? DateTimeOffset.MinValue)
                                .FirstOrDefault();

                            if (latest != null)
                            {
                                responseApplications = [latest];
                                applicationDataJson = JsonSerializer.Serialize(latest, _jsonOptions);
                            }
                            else
                            {
                                applicationFetchFailed = true;
                            }

                            break;
                        }
                        case ChatIntent.AllApplications:
                        {
                            var appsJson = await FetchApplicationsAsync(request.AuthToken, cancellationToken);
                            var summaries = BuildApplicationSummariesFromJson(appsJson)
                                .OrderByDescending(summary => ParseCreatedAt(summary.CreatedAt) ?? DateTimeOffset.MinValue)
                                .Take(5)
                                .ToList();

                            if (summaries.Count > 0)
                            {
                                responseApplications = summaries;
                                applicationDataJson = JsonSerializer.Serialize(summaries, _jsonOptions);
                            }
                            else
                            {
                                applicationFetchFailed = true;
                            }

                            break;
                        }
                        case ChatIntent.ApplicationsByStatus:
                        {
                            var appsJson = await FetchApplicationsAsync(request.AuthToken, cancellationToken);
                            var summaries = BuildApplicationSummariesFromJson(appsJson)
                                .Where(summary => IsStatusMatch(summary.Status, statusFilter))
                                .OrderByDescending(summary => ParseCreatedAt(summary.CreatedAt) ?? DateTimeOffset.MinValue)
                                .Take(5)
                                .ToList();

                            if (summaries.Count > 0)
                            {
                                responseApplications = summaries;
                                applicationDataJson = JsonSerializer.Serialize(summaries, _jsonOptions);
                            }
                            else
                            {
                                applicationFetchFailed = true;
                            }

                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    applicationFetchFailed = true;
                }
            }
        }

        var systemPrompt = BuildSystemPrompt(applicationDataJson);

        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        if (request.ConversationHistory.Count > 0)
        {
            messages.AddRange(request.ConversationHistory);
        }

        messages.Add(new ChatMessage { Role = "user", Content = request.Message });

        var lmStudioPayload = new
        {
            model = _lmStudioOptions.Model,
            messages,
            max_tokens = 500,
            temperature = 0.7,
            stream = false
        };

        try
        {
            var lmClient = _httpClientFactory.CreateClient();
            lmClient.BaseAddress = new Uri(_lmStudioOptions.BaseUrl);

            var content = new StringContent(JsonSerializer.Serialize(lmStudioPayload, _jsonOptions), Encoding.UTF8, "application/json");
            var lmResponse = await lmClient.PostAsync("/v1/chat/completions", content, cancellationToken);

            if (!lmResponse.IsSuccessStatusCode)
            {
                return Ok(new ChatResponse { Reply = "I'm currently unavailable. Please try again later." });
            }

            var responseJson = await lmResponse.Content.ReadAsStringAsync(cancellationToken);
            var lmStudioResponse = JsonSerializer.Deserialize<LMStudioResponse>(responseJson, _jsonOptions);
            var reply = lmStudioResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(reply))
            {
                reply = "I'm currently unavailable. Please try again later.";
            }

            if (applicationFetchFailed && intent != ChatIntent.General)
            {
                reply += "\n\nNote: I couldn't access your application data right now, so this is general guidance.";
            }

            return Ok(new ChatResponse { Reply = reply, Applications = responseApplications });
        }
        catch (HttpRequestException)
        {
            return Ok(new ChatResponse { Reply = "I'm currently unavailable. Please try again later." });
        }
    }

    private static string BuildSystemPrompt(string? applicationDataJson)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are CapFinLoan Assistant, a helpful chatbot for the CapFinLoan loan processing platform. You help applicants understand their loan applications, explain the process, and answer questions about their application status.");
        prompt.AppendLine();
        prompt.AppendLine("You know the following about the CapFinLoan platform:");
        prompt.AppendLine("- Applicants can apply for home, personal, or business loans");
        prompt.AppendLine("- The application goes through these stages in order:");
        prompt.AppendLine("  Draft → Submitted → DocsPending → DocsVerified → UnderReview → Approved/Rejected");
        prompt.AppendLine("- Draft: Application is saved but not submitted yet");
        prompt.AppendLine("- Submitted: Application has been submitted and is awaiting document upload");
        prompt.AppendLine("- DocsPending: Documents have been requested, applicant needs to upload ID Proof, Address Proof, Bank Statement, and Income Proof");
        prompt.AppendLine("- DocsVerified: All documents have been verified by admin");
        prompt.AppendLine("- UnderReview: Application is being reviewed by the loan officer");
        prompt.AppendLine("- Approved: Loan has been approved. Applicant will receive an email with sanction details");
        prompt.AppendLine("- Rejected: Loan application was not approved. Applicant will receive an email with remarks");
        prompt.AppendLine();
        prompt.AppendLine("Required documents:");
        prompt.AppendLine("- ID Proof: Government issued photo ID");
        prompt.AppendLine("- Address Proof: Utility bill, rental agreement, or passport");
        prompt.AppendLine("- Bank Statement: Last 3-6 months bank statements");
        prompt.AppendLine("- Income Proof: Salary slips or IT returns");
        prompt.AppendLine();
        prompt.AppendLine("Tips for approval:");
        prompt.AppendLine("- Ensure all documents are clear and legible");
        prompt.AppendLine("- Monthly income should be sufficient to cover EMI");
        prompt.AppendLine("- Provide accurate employment and income details");
        prompt.AppendLine();
        prompt.AppendLine("If the user asks about their specific application status and applicationData is provided in the context below, use that data to answer precisely.");
        prompt.AppendLine("If no applicationData is provided, give general answers.");
        prompt.AppendLine("Keep responses concise, friendly, and helpful.");
        prompt.AppendLine("Do not make up application data. Only use what is provided.");

        if (!string.IsNullOrWhiteSpace(applicationDataJson))
        {
            prompt.AppendLine();
            prompt.AppendLine("REAL APPLICATION DATA FOR THIS USER (use this to answer their question):");
            prompt.AppendLine(applicationDataJson);
        }

        return prompt.ToString();
    }

    private async Task<string?> FetchApplicationsAsync(string authToken, CancellationToken cancellationToken)
    {
        var appClient = _httpClientFactory.CreateClient();
        appClient.BaseAddress = new Uri(_applicationServiceOptions.BaseUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/applications/my");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var response = await appClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<string?> FetchApplicationByIdAsync(string applicationId, string authToken, CancellationToken cancellationToken)
    {
        var appClient = _httpClientFactory.CreateClient();
        appClient.BaseAddress = new Uri(_applicationServiceOptions.BaseUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/applications/{applicationId}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var response = await appClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static List<ChatApplicationSummary> BuildApplicationSummariesFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var list = new List<ChatApplicationSummary>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                var summary = BuildApplicationSummary(element);
                if (summary != null)
                {
                    list.Add(summary);
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var items))
        {
            foreach (var element in items.EnumerateArray())
            {
                var summary = BuildApplicationSummary(element);
                if (summary != null)
                {
                    list.Add(summary);
                }
            }
        }

        return list;
    }

    private static ChatApplicationSummary? BuildApplicationSummaryFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return BuildApplicationSummary(root);
    }

    private static ChatApplicationSummary? BuildApplicationSummary(JsonElement element)
    {
        var applicationId = ReadString(element, "applicationId") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return null;
        }

        var loanAmount = ReadDecimal(element, "loanAmount")
            ?? ReadDecimalFromNested(element, "loanDetails", "requestedAmount")
            ?? 0m;
        var loanPurpose = ReadString(element, "loanPurpose")
            ?? ReadStringFromNested(element, "loanDetails", "loanPurpose")
            ?? string.Empty;
        var status = ReadString(element, "status")
            ?? ReadString(element, "currentStatus")
            ?? string.Empty;
        var createdAt = ReadString(element, "createdAt")
            ?? ReadString(element, "submittedAt")
            ?? string.Empty;

        return new ChatApplicationSummary
        {
            ApplicationId = applicationId,
            ShortId = applicationId.Length >= 8 ? applicationId[..8] : applicationId,
            LoanAmount = loanAmount,
            LoanPurpose = loanPurpose,
            Status = status,
            CreatedAt = createdAt,
        };
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out var stringValue))
        {
            return stringValue;
        }

        return null;
    }

    private static decimal? ReadDecimalFromNested(JsonElement element, string parentProperty, string propertyName)
    {
        if (!element.TryGetProperty(parentProperty, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadDecimal(parent, propertyName);
    }

    private static string? ReadStringFromNested(JsonElement element, string parentProperty, string propertyName)
    {
        if (!element.TryGetProperty(parentProperty, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(parent, propertyName);
    }

    private static DateTimeOffset? ParseCreatedAt(string? createdAt)
    {
        return DateTimeOffset.TryParse(createdAt, out var parsed) ? parsed : null;
    }

    private static bool IsStatusMatch(string status, string? expectedStatus)
    {
        if (string.IsNullOrWhiteSpace(expectedStatus))
        {
            return false;
        }

        return NormalizeStatus(status) == NormalizeStatus(expectedStatus);
    }

    private static string NormalizeStatus(string status)
    {
        return new string(status
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static ChatIntent DetectIntent(string message, out string? statusFilter, out string? applicationId)
    {
        statusFilter = null;
        applicationId = FindApplicationId(message);
        if (!string.IsNullOrWhiteSpace(applicationId))
        {
            return ChatIntent.SpecificApplication;
        }

        var normalized = message.ToLowerInvariant();

        if (normalized.Contains("latest") || normalized.Contains("recent") || normalized.Contains("newest") || normalized.Contains("last application"))
        {
            return ChatIntent.LatestApplication;
        }

        statusFilter = GetStatusFilter(normalized);
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            return ChatIntent.ApplicationsByStatus;
        }

        if (normalized.Contains("all my applications") || normalized.Contains("my applications") || normalized.Contains("list applications"))
        {
            return ChatIntent.AllApplications;
        }

        return ChatIntent.General;
    }

    private static string? GetStatusFilter(string normalizedMessage)
    {
        if (normalizedMessage.Contains("docs pending") || normalizedMessage.Contains("document pending"))
        {
            return "DocsPending";
        }

        if (normalizedMessage.Contains("docs verified") || normalizedMessage.Contains("document verified"))
        {
            return "DocsVerified";
        }

        if (normalizedMessage.Contains("under review"))
        {
            return "UnderReview";
        }

        if (normalizedMessage.Contains("approved"))
        {
            return "Approved";
        }

        if (normalizedMessage.Contains("rejected"))
        {
            return "Rejected";
        }

        if (normalizedMessage.Contains("submitted"))
        {
            return "Submitted";
        }

        if (normalizedMessage.Contains("draft"))
        {
            return "Draft";
        }

        if (normalizedMessage.Contains("pending"))
        {
            return "DocsPending";
        }

        return null;
    }

    private static string? FindApplicationId(string message)
    {
        var match = Regex.Match(message, "\\b[a-fA-F0-9-]{8,}\\b");
        return match.Success ? match.Value : null;
    }

    private enum ChatIntent
    {
        LatestApplication,
        ApplicationsByStatus,
        AllApplications,
        SpecificApplication,
        General,
    }

    private sealed class LMStudioResponse
    {
        public List<LMStudioChoice>? Choices { get; set; }
    }

    private sealed class LMStudioChoice
    {
        public LMStudioMessage? Message { get; set; }
    }

    private sealed class LMStudioMessage
    {
        public string? Content { get; set; }
    }
}
