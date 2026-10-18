using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Banova.Api.Lib;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface IProductService
{
    Task<(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize)> ListAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    /// <summary>Chỉ trả sản phẩm đang bán và chưa bị xoá mềm.</summary>
    private IQueryable<Product> VisibleProducts() => _db.Products
        .AsNoTracking()
        .Include(p => p.Category)
        .Where(p => p.IsActive && p.DeletedAt == null);

    public async Task<(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize)> ListAsync(
        ProductQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        // API.md: pageSize mặc định 12, tối đa 50
        var pageSize = query.PageSize < 1 ? Constants.DefaultPageSize : Math.Min(query.PageSize, Constants.MaxPageSize);

        var q = VisibleProducts();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var slug = query.Category.Trim().ToLowerInvariant();
            q = q.Where(p => p.Category!.Slug == slug);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(p => p.Name.Contains(term) || (p.ShortDescription != null && p.ShortDescription.Contains(term)));
        }

        q = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items.Select(p => p.ToDto()).ToList(), total, page, pageSize);
    }

    public async Task<ProductDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw ApiException.Validation("Thiếu slug sản phẩm");
        }

        var product = await VisibleProducts().FirstOrDefaultAsync(p => p.Slug == slug, ct);
        if (product is null)
        {
            throw ApiException.NotFound($"Không tìm thấy sản phẩm '{slug}'");
        }

        return product.ToDto();
    }
}
