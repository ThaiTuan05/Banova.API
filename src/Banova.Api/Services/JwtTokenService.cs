using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Banova.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Banova.Api.Services;

/// <summary>Cấu hình JWT — bind từ section "Jwt" trong appsettings (API.md mục 6).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Banova";
    public string Audience { get; set; } = "BanovaAdmin";
    public string Secret { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 480;
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) Create(User user);
}

/// <summary>
/// Ký JWT bằng HMAC-SHA256. Token được gửi lại cho client qua cookie httpOnly
/// <c>banova_token</c> (và vẫn chấp nhận header <c>Authorization: Bearer</c> khi test Scalar).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) Create(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes <= 0 ? 480 : _options.ExpireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
