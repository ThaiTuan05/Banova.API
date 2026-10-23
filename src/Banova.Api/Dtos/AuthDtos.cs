using Banova.Api.Entities;

namespace Banova.Api.Dtos;

/// <summary>Body của POST /api/auth/login (API.md mục 6.1).</summary>
public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// AdminUser trả cho FE (API.md mục 6.1) — KHÔNG bao giờ chứa PasswordHash (NFR-07).
/// </summary>
public sealed class AuthUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public Role Role { get; init; }
}
