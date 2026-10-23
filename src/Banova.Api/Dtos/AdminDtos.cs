using Banova.Api.Lib;

namespace Banova.Api.Dtos;

/// <summary>
/// Body của POST/PUT /api/admin/products (API.md mục 7.1, 7.2).
/// Field null = "không cung cấp" → khi PUT sẽ giữ nguyên giá trị cũ (hỗ trợ partial update).
/// </summary>
public sealed class ProductUpsertRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public int? Price { get; set; }
    public string? Unit { get; set; }
    public string? ServingSize { get; set; }
    public decimal? RsContent { get; set; }

    /// <summary>"RS2" | "RS3" — để dạng chuỗi rồi parse để trả VALIDATION rõ ràng.</summary>
    public string? RsType { get; set; }

    public int? Gi { get; set; }
    public decimal? Fiber { get; set; }
    public string? Ingredients { get; set; }
    public string? Usage { get; set; }
    public string? ImageUrl { get; set; }
    public int? Stock { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Body của POST/PUT /api/admin/articles (API.md mục 8.1, 8.2).</summary>
public sealed class ArticleUpsertRequest
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? Thumbnail { get; set; }

    /// <summary>"SCIENCE" | "BLOG".</summary>
    public string? Type { get; set; }

    public string? References { get; set; }
    public string? Author { get; set; }

    /// <summary>"DRAFT" | "PUBLISHED".</summary>
    public string? Status { get; set; }
}

/// <summary>Body của PATCH /api/orders/{id}/status (API.md mục 9.2).</summary>
public sealed class OrderStatusUpdateRequest
{
    public string? Status { get; set; }
}

/// <summary>Thống kê dashboard (API.md mục 9.3).</summary>
public sealed class AdminStatsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int TotalProducts { get; init; }
    public int TotalArticles { get; init; }
    public int TotalSubscribers { get; init; }
    public int TotalContacts { get; init; }

    /// <summary>Tổng doanh thu demo, không tính đơn đã huỷ (VND).</summary>
    public int TotalRevenue { get; init; }
}

/// <summary>Query của GET /api/orders (admin — API.md mục 9.1).</summary>
public sealed class OrderQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Constants.DefaultPageSize;

    /// <summary>PENDING | CONFIRMED | DELIVERING | COMPLETED | CANCELLED.</summary>
    public string? Status { get; set; }

    /// <summary>Tìm theo mã đơn / tên khách / số điện thoại.</summary>
    public string? Q { get; set; }
}
