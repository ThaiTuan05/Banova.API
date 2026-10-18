namespace Banova.Api.Entities;

/// <summary>Bài viết Khoa học / Blog. Bài SCIENCE bắt buộc có References (NFR-09).</summary>
public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Excerpt { get; set; }

    /// <summary>Nội dung HTML/Markdown do admin nhập.</summary>
    public string Content { get; set; } = string.Empty;

    public string? Thumbnail { get; set; }

    public ArticleType Type { get; set; } = ArticleType.SCIENCE;

    /// <summary>Nguồn trích dẫn — bắt buộc với bài SCIENCE.</summary>
    public string? References { get; set; }

    public string? Author { get; set; }

    public PublishState Status { get; set; } = PublishState.DRAFT;

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}
