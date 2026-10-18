namespace Banova.Api.Entities;

/// <summary>Tài khoản quản trị. Mật khẩu luôn hash bằng BCrypt, không lưu plain-text (NFR-07).</summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.CUSTOMER;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
