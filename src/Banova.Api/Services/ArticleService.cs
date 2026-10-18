using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Banova.Api.Lib;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface IArticleService
{
    Task<(IReadOnlyList<ArticleDto> Items, int Total, int Page, int PageSize)> ListAsync(ArticleQuery query, CancellationToken ct = default);
    Task<ArticleDto> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;

    public ArticleService(AppDbContext db) => _db = db;

    /// <summary>Khách chỉ được thấy bài đã PUBLISHED và chưa xoá mềm.</summary>
    private IQueryable<Article> PublishedArticles() => _db.Articles
        .AsNoTracking()
        .Where(a => a.Status == PublishState.PUBLISHED && a.DeletedAt == null);

    public async Task<(IReadOnlyList<ArticleDto> Items, int Total, int Page, int PageSize)> ListAsync(
        ArticleQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize > Constants.MaxPageSize ? Constants.MaxPageSize : (query.PageSize < 1 ? Constants.DefaultPageSize : query.PageSize);

        var q = PublishedArticles();

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<ArticleType>(query.Type.Trim(), ignoreCase: true, out var type))
        {
            q = q.Where(a => a.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(a => a.Title.Contains(term) || (a.Excerpt != null && a.Excerpt.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(a => a.ToDto()).ToList(), total, page, pageSize);
    }

    public async Task<ArticleDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw ApiException.Validation("Thiếu slug bài viết");
        }

        var article = await PublishedArticles().FirstOrDefaultAsync(a => a.Slug == slug, ct);
        if (article is null)
        {
            throw ApiException.NotFound($"Không tìm thấy bài viết '{slug}'");
        }

        return article.ToDto();
    }
}
