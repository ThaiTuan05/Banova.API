using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface IAuthService
{
    Task<(AuthUserDto User, string Token, DateTime ExpiresAtUtc)> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthUserDto> GetCurrentAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Xác thực admin (API.md mục 6). Mật khẩu kiểm bằng BCrypt (NFR-07),
/// sai tài khoản hoặc sai mật khẩu đều trả cùng một thông điệp để không lộ email tồn tại.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _tokens;

    public AuthService(AppDbContext db, IJwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<(AuthUserDto User, string Token, DateTime ExpiresAtUtc)> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = "Vui lòng nhập email";
        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = "Vui lòng nhập mật khẩu";

        if (errors.Count > 0)
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // BCrypt.Verify tự chống timing attack; khi user null vẫn verify với hash giả để thời gian phản hồi tương đương.
        var hash = user?.PasswordHash ?? "$2a$11$0000000000000000000000000000000000000000000000000000";
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, hash);

        if (user is null || !passwordOk)
            throw ApiException.Unauthorized("Email hoặc mật khẩu không đúng");

        var (token, expiresAt) = _tokens.Create(user);

        return (new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        }, token, expiresAt);
    }

    public async Task<AuthUserDto> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            throw ApiException.Unauthorized("Tài khoản không còn tồn tại");

        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
