using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Firmeza.Application.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
namespace Firmeza.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public AuthResponse Create(ApplicationUser user, IReadOnlyList<string> roles, Guid? customerId)
    {
        var config = options.Value;
        var expires = DateTimeOffset.UtcNow.AddMinutes(config.ExpirationMinutes);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), new("stamp", user.SecurityStamp ?? ""), new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var token = new JwtSecurityToken(config.Issuer, config.Audience, claims, DateTime.UtcNow, expires.UtcDateTime, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, roles, customerId);
    }
}
