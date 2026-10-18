namespace Banova.Api.Entities;

/// <summary>Email đăng ký nhận tin ở Footer. UNIQUE — trùng email trả 409 CONFLICT.</summary>
public class NewsletterSubscriber
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
