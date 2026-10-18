using Banova.Api.Entities;
using Banova.Api.Lib;

namespace Banova.Api.Dtos;

public sealed class CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
}

public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string Description { get; init; } = string.Empty;
    public CategoryDto Category { get; init; } = new();
    public int Price { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string ServingSize { get; init; } = string.Empty;
    public decimal RsContent { get; init; }
    public RsType RsType { get; init; }
    public int? Gi { get; init; }
    public decimal? Fiber { get; init; }
    public string? Ingredients { get; init; }
    public string? Usage { get; init; }
    public string? ImageUrl { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Tham số lọc danh sách sản phẩm — khớp API.md mục 2.1.</summary>
public sealed class ProductQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Constants.DefaultPageSize;
    public string? Category { get; set; }
    public string? Q { get; set; }
    public string? Sort { get; set; }
}
