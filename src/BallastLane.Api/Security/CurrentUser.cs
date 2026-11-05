using System.Security.Claims;
using BallastLane.Application.Common;

namespace BallastLane.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var id))
            {
                throw new AuthenticationFailedException("Sign in again.");
            }

            return id;
        }
    }
}
