using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LanguageLearning.Domain;
using Microsoft.IdentityModel.Tokens;

namespace LanguageLearning.API.Common;

public sealed class JwtSettings
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public int AccessTokenHours { get; init; } = 8;
}

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult Create(User user, string sessionToken);
}

public sealed class JwtTokenService(JwtSettings settings) : IJwtTokenService
{
    public AccessTokenResult Create(User user, string sessionToken)
    {
        var expiresAt = DateTime.UtcNow.AddHours(settings.AccessTokenHours);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("session_token", sessionToken)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Issuer,
            claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
