using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CapFinLoan.Document.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue("UserId")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(claimValue, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user identifier in token.");
        }

        return userId;
    }
}
