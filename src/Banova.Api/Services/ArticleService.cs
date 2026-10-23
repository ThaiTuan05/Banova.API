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

    // ---------- Admin (API.md mục 8) ----------
    Task<(IReadOnlyList<ArticleDto> Items, int Total, int Page, int PageSize)> ListAdminAsync(ArticleQuery query, CancellationToken ct = default);
    Task<ArticleDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ArticleDto> CreateAsync(ArticleUpsertRequest request, CancellationToken ct = default);
    Task<ArticleDto> UpdateAsync(Guid id, ArticleUpsertRequest request, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
}

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;

    public ArticleService(AppDbContext db) => _db = db;

    /// <summary>Khách chỉ được thấy bài đã PUBLISHED và chưa xoá mềm.</summary>
    private IQueryable<Article> PublishedArticles() => _db.Articles
        .AsNoTracking()
        .Where(a => a.Status == PublishState.PUBLISHED && a.DeletedAt == null);

    /// <summary>Admin thấy cả bản nháp, nhưng không thấy bài đã xoá mềm.</summary>
    private IQueryable<Article> AdminArticles() => _db.Articles
        .AsNoTracking()
        .Where(a => a.DeletedAt == null);

    public async Task<(IReadOnlyList<ArticleDto> Items, int Total, int Page, int PageSize)> ListAsync(
        ArticleQuery query, CancellationToken ct = default)
        => await QueryAsync(PublishedArticles(), query, ct);

    public async Task<(IReadOnlyList<ArticleDto> Items, int Total, int Page, int PageSize)> ListAdminAsync(
        ArticleQuery query, CancellationToken ct = default)
        => await QueryAsync(AdminArticles(), query, ct, filterStatus: true);

    private static async Task<(IReadOnlyList<ArticleDto> Items, int Total, int Page, int PageSize)> QueryAsync(
        IQueryable<Article> source, ArticleQuery query, CancellationToken ct, bool filterStatus = false)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize > Constants.MaxPageSize
            ? Constants.MaxPageSize
            : (query.PageSize < 1 ? Constants.DefaultPageSize : query.PageSize);

        var q = source;

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<ArticleType>(query.Type.Trim(), ignoreCase: true, out var type))
        {
            q = q.Where(a => a.Type == type);
        }

        if (filterStatus && !string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<PublishState>(query.Status.Trim(), ignoreCase: true, out var status))
        {
            q = q.Where(a => a.Status == status);
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

    public async Task<ArticleDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var article = await AdminArticles().FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw ApiException.NotFound("Không tìm thấy bài viết");

        return article.ToDto();
    }

    public async Task<ArticleDto> CreateAsync(ArticleUpsertRequest request, CancellationToken ct = default)
    {
        var errors = await ValidateAsync(request, null, ct);
        if (errors.Count > 0)
        {
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);
        }

        await EnsureSlugAvailableAsync(ArticleSlug(request, null), null, ct);

        var article = new Article();
        Apply(article, request, isCreate: true);

        if (article.Status == PublishState.PUBLISHED)
        {
            article.PublishedAt = DateTime.UtcNow;
        }

        _db.Articles.Add(article);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw ApiException.Conflict("Slug bài viết đã tồn tại");
        }

        return article.ToDto();
    }

    public async Task<ArticleDto> UpdateAsync(Guid id, ArticleUpsertRequest request, CancellationToken ct = default)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, ct)
            ?? throw ApiException.NotFound("Không tìm thấy bài viết");

        var errors = await ValidateAsync(request, article, ct);
        if (errors.Count > 0)
        {
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);
        }

        await EnsureSlugAvailableAsync(ArticleSlug(request, article), article.Id, ct);

        var wasPublished = article.Status == PublishState.PUBLISHED;
        Apply(article, request, isCreate: false);

        // Xuất bản lần đầu -> set publishedAt; bỏ xuất bản giữ nguyên mốc cũ để không mất lịch sử.
        if (!wasPublished && article.Status == PublishState.PUBLISHED && article.PublishedAt is null)
        {
            article.PublishedAt = DateTime.UtcNow;
        }

        article.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw ApiException.Conflict("Slug bài viết đã tồn tại");
        }

        return article.ToDto();
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, ct)
            ?? throw ApiException.NotFound("Không tìm thấy bài viết");

        article.DeletedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static string? ArticleSlug(ArticleUpsertRequest request, Article? existing)
        => request.Slug is null ? existing?.Slug : ProductService.NormalizeSlug(request.Slug);

    private async Task EnsureSlugAvailableAsync(string? slug, Guid? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return; // thiếu slug đã được ValidateAsync báo lỗi
        }

        var duplicated = await _db.Articles
            .AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId), ct);

        if (duplicated)
        {
            throw ApiException.Conflict("Slug đã được dùng cho bài viết khác", new { field = "slug" });
        }
    }

    private async Task<Dictionary<string, string>> ValidateAsync(
        ArticleUpsertRequest request, Article? existing, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        var title = request.Title ?? existing?.Title;
        var slug = request.Slug is null ? existing?.Slug : ProductService.NormalizeSlug(request.Slug);
        var content = request.Content ?? existing?.Content;
        var typeText = request.Type ?? existing?.Type.ToString();
        var statusText = request.Status ?? existing?.Status.ToString();
        var references = request.References ?? existing?.References;

        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = "Vui lòng nhập tiêu đề";
        else if (title!.Length > 300)
            errors["title"] = "Tiêu đề tối đa 300 ký tự";

        if (string.IsNullOrWhiteSpace(slug))
            errors["slug"] = "Vui lòng nhập slug";
        else if (slug!.Length > 300)
            errors["slug"] = "Slug tối đa 300 ký tự";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            errors["slug"] = "Slug chỉ gồm chữ thường, số và dấu gạch nối";

        if (string.IsNullOrWhiteSpace(content))
            errors["content"] = "Vui lòng nhập nội dung bài viết";

        if (string.IsNullOrWhiteSpace(typeText) ||
            !Enum.TryParse<ArticleType>(typeText, ignoreCase: true, out var type) ||
            !Enum.IsDefined(type))
            errors["type"] = "Loại bài viết chỉ nhận SCIENCE hoặc BLOG";

        if (string.IsNullOrWhiteSpace(statusText) ||
            !Enum.TryParse<PublishState>(statusText, ignoreCase: true, out var status) ||
            !Enum.IsDefined(status))
            errors["status"] = "Trạng thái chỉ nhận DRAFT hoặc PUBLISHED";

        // NFR-03/NFR-09: bài SCIENCE bắt buộc có nguồn trích dẫn
        if (string.IsNullOrWhiteSpace(typeText) ||
            (Enum.TryParse<ArticleType>(typeText, ignoreCase: true, out var parsedType) && parsedType == ArticleType.SCIENCE))
        {
            if (string.IsNullOrWhiteSpace(references))
                errors["references"] = "Bài khoa học (SCIENCE) bắt buộc có nguồn trích dẫn";
        }

        if (request.Excerpt is { Length: > 1000 })
            errors["excerpt"] = "Tóm tắt tối đa 1000 ký tự";

        if (request.Thumbnail is { Length: > 1000 })
            errors["thumbnail"] = "URL ảnh tối đa 1000 ký tự";

        if (request.Author is { Length: > 200 })
            errors["author"] = "Tác giả tối đa 200 ký tự";

        return errors;
    }

    private static void Apply(Article article, ArticleUpsertRequest request, bool isCreate)
    {
        if (isCreate || request.Title is not null) article.Title = request.Title!.Trim();
        if (isCreate || request.Slug is not null) article.Slug = ProductService.NormalizeSlug(request.Slug!);
        if (request.Excerpt is not null) article.Excerpt = NullIfEmpty(request.Excerpt);
        if (isCreate || request.Content is not null) article.Content = request.Content!.Trim();
        if (request.Thumbnail is not null) article.Thumbnail = NullIfEmpty(request.Thumbnail);
        if (isCreate || request.Type is not null) article.Type = Enum.Parse<ArticleType>(request.Type!, ignoreCase: true);
        if (request.References is not null) article.References = NullIfEmpty(request.References);
        if (request.Author is not null) article.Author = NullIfEmpty(request.Author);
        if (isCreate || request.Status is not null) article.Status = Enum.Parse<PublishState>(request.Status!, ignoreCase: true);

        article.UpdatedAt = DateTime.UtcNow;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
