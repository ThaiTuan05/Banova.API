namespace Banova.Api.Entities;

/// <summary>Tin nhắn khách gửi từ trang Liên hệ.</summary>
public class ContactMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsHandled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
