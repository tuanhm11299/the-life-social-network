using TheLife.Application.Common.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TheLife.Api.Common;

/// <summary>Reads the signed-in user's id from the "sub" claim of the validated access token.</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(subject, out var id)
                ? id
                : throw new UnauthorizedAccessException("No signed-in user.");
        }
    }
}
