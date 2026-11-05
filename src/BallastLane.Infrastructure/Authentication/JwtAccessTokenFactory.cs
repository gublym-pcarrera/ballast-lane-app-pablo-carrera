using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BallastLane.Application.Abstractions;
using BallastLane.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BallastLane.Infrastructure.Authentication;

public sealed class JwtAccessTokenFactory(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenFactory
{
    public AccessToken Create(User user)
    {
        var jwt = options.Value;
        var expires = clock.GetUtcNow().UtcDateTime.AddMinutes(jwt.ExpiresMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: clock.GetUtcNow().UtcDateTime,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
