namespace Banova.Api.Dtos;

/// <summary>Body của POST /api/newsletter.</summary>
public sealed class NewsletterRequest
{
    public string Email { get; set; } = string.Empty;
}
