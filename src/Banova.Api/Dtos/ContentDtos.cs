using Banova.Api.Entities;

namespace Banova.Api.Dtos;

public sealed class ArticleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Excerpt { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? Thumbnail { get; init; }
    public ArticleType Type { get; init; }
    public string? References { get; init; }
    public string? Author { get; init; }
    public PublishState Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Tham số lọc danh sách bài viết — khớp API.md mục 3.1.</summary>
public sealed class ArticleQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Lib.Constants.DefaultPageSize;
    public string? Type { get; set; }
    public string? Q { get; set; }
}
