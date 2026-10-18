namespace Banova.Api.Dtos;

/// <summary>Body của POST /api/contact.</summary>
public sealed class ContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
