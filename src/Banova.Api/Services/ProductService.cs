using System.Text.RegularExpressions;
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

    // ---------- Admin (API.md mục 7) ----------
    Task<(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize)> ListAdminAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(ProductUpsertRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, ProductUpsertRequest request, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    /// <summary>Chỉ trả sản phẩm đang bán và chưa bị xoá mềm.</summary>
    private IQueryable<Product> VisibleProducts() => _db.Products
        .AsNoTracking()
        .Include(p => p.Category)
        .Where(p => p.IsActive && p.DeletedAt == null);

    /// <summary>Admin thấy cả sản phẩm đang tạm ẩn, nhưng không thấy sản phẩm đã xoá mềm.</summary>
    private IQueryable<Product> AdminProducts() => _db.Products
        .Include(p => p.Category)
        .Where(p => p.DeletedAt == null);

    public async Task<(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize)> ListAsync(
        ProductQuery query, CancellationToken ct = default)
        => await QueryAsync(VisibleProducts().AsNoTracking(), query, ct);

    public async Task<(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize)> ListAdminAsync(
        ProductQuery query, CancellationToken ct = default)
        => await QueryAsync(AdminProducts().AsNoTracking(), query, ct);

    private static async Task<(IReadOnlyList<ProductDto> Items, int Total, int Page, int PageSize)> QueryAsync(
        IQueryable<Product> source, ProductQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        // API.md: pageSize mặc định 12, tối đa 50
        var pageSize = query.PageSize < 1 ? Constants.DefaultPageSize : Math.Min(query.PageSize, Constants.MaxPageSize);

        var q = source;

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
            "name_asc" => q.OrderBy(p => p.Name),
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

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await AdminProducts().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw ApiException.NotFound("Không tìm thấy sản phẩm");

        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(ProductUpsertRequest request, CancellationToken ct = default)
    {
        var errors = await ValidateAsync(request, null, ct);
        if (errors.Count > 0)
        {
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);
        }

        await EnsureSlugAvailableAsync(ProductSlug(request, null), null, ct);

        var product = new Product();
        Apply(product, request, isCreate: true);

        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw ApiException.Conflict("Slug sản phẩm đã tồn tại");
        }

        await _db.Entry(product).Reference(p => p.Category).LoadAsync(ct);
        return product.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(Guid id, ProductUpsertRequest request, CancellationToken ct = default)
    {
        var product = await AdminProducts().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw ApiException.NotFound("Không tìm thấy sản phẩm");

        var errors = await ValidateAsync(request, product, ct);
        if (errors.Count > 0)
        {
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);
        }

        await EnsureSlugAvailableAsync(ProductSlug(request, product), product.Id, ct);

        Apply(product, request, isCreate: false);
        product.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw ApiException.Conflict("Slug sản phẩm đã tồn tại");
        }

        await _db.Entry(product).Reference(p => p.Category).LoadAsync(ct);
        return product.ToDto();
    }

    /// <summary>Xoá mềm: chỉ set DeletedAt + IsActive=false, giữ nguyên dữ liệu cho lịch sử đơn hàng (NFR-10).</summary>
    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct)
            ?? throw ApiException.NotFound("Không tìm thấy sản phẩm");

        product.DeletedAt = DateTime.UtcNow;
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Chuẩn hoá slug về dạng URL: chữ thường, gạch nối, bỏ dấu tiếng Việt.</summary>
    public static string NormalizeSlug(string input)
    {
        var normalized = input.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark) continue;

            if (ch is 'đ' or 'Đ') { builder.Append('d'); continue; }
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }

        var slug = Regex.Replace(builder.ToString(), "-{2,}", "-").Trim('-');
        return slug;
    }

    private static string? ProductSlug(ProductUpsertRequest request, Product? existing)
        => request.Slug is null ? existing?.Slug : NormalizeSlug(request.Slug);

    private async Task EnsureSlugAvailableAsync(string? slug, Guid? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return; // thiếu slug đã được ValidateAsync báo lỗi
        }

        var duplicated = await _db.Products
            .AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), ct);

        if (duplicated)
        {
            throw ApiException.Conflict("Slug đã được dùng cho sản phẩm khác", new { field = "slug" });
        }
    }

    private async Task<Dictionary<string, string>> ValidateAsync(
        ProductUpsertRequest request, Product? existing, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        var name = request.Name ?? existing?.Name;
        var slug = request.Slug is null ? existing?.Slug : NormalizeSlug(request.Slug);
        var description = request.Description ?? existing?.Description;
        var unit = request.Unit ?? existing?.Unit;
        var servingSize = request.ServingSize ?? existing?.ServingSize;
        var price = request.Price ?? existing?.Price;
        var stock = request.Stock ?? existing?.Stock;
        var categoryId = request.CategoryId ?? existing?.CategoryId;
        var rsContent = request.RsContent ?? existing?.RsContent;
        var rsTypeText = request.RsType ?? existing?.RsType.ToString();

        if (string.IsNullOrWhiteSpace(name))
            errors["name"] = "Vui lòng nhập tên sản phẩm";
        else if (name!.Length > 200)
            errors["name"] = "Tên sản phẩm tối đa 200 ký tự";

        if (string.IsNullOrWhiteSpace(slug))
            errors["slug"] = "Vui lòng nhập slug";
        else if (slug!.Length > 200)
            errors["slug"] = "Slug tối đa 200 ký tự";
        else if (!SlugPattern.IsMatch(slug))
            errors["slug"] = "Slug chỉ gồm chữ thường, số và dấu gạch nối";

        if (string.IsNullOrWhiteSpace(description))
            errors["description"] = "Vui lòng nhập mô tả sản phẩm";

        if (string.IsNullOrWhiteSpace(unit))
            errors["unit"] = "Vui lòng nhập đơn vị (hũ, gói, hộp...)";
        else if (unit!.Length > 20)
            errors["unit"] = "Đơn vị tối đa 20 ký tự";

        if (string.IsNullOrWhiteSpace(servingSize))
            errors["servingSize"] = "Vui lòng nhập khẩu phần";
        else if (servingSize!.Length > 50)
            errors["servingSize"] = "Khẩu phần tối đa 50 ký tự";

        if (price is null or <= 0)
            errors["price"] = "Giá phải lớn hơn 0";
        else if (price > 1_000_000_000)
            errors["price"] = "Giá quá lớn";

        if (stock is null or < 0)
            errors["stock"] = "Tồn kho phải >= 0";

        if (categoryId is null || categoryId == Guid.Empty)
            errors["categoryId"] = "Vui lòng chọn danh mục";
        else if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            errors["categoryId"] = "Danh mục không tồn tại";

        if (rsContent is null or < 0)
            errors["rsContent"] = "Hàm lượng RS phải >= 0";

        if (string.IsNullOrWhiteSpace(rsTypeText) ||
            !Enum.TryParse<RsType>(rsTypeText, ignoreCase: true, out var rsType) ||
            !Enum.IsDefined(rsType))
            errors["rsType"] = "Loại RS chỉ nhận RS2 hoặc RS3";

        if (request.Gi is < 0)
            errors["gi"] = "Chỉ số GI phải >= 0";

        if (request.Fiber is < 0)
            errors["fiber"] = "Chất xơ phải >= 0";

        if (request.ShortDescription is { Length: > 500 })
            errors["shortDescription"] = "Mô tả ngắn tối đa 500 ký tự";

        if (request.ImageUrl is { Length: > 1000 })
            errors["imageUrl"] = "URL ảnh tối đa 1000 ký tự";

        if (request.Ingredients is { Length: > 2000 })
            errors["ingredients"] = "Thành phần tối đa 2000 ký tự";

        if (request.Usage is { Length: > 2000 })
            errors["usage"] = "Hướng dẫn sử dụng tối đa 2000 ký tự";

        return errors;
    }

    private static void Apply(Product product, ProductUpsertRequest request, bool isCreate)
    {
        if (isCreate || request.Name is not null) product.Name = request.Name!.Trim();
        if (isCreate || request.Slug is not null) product.Slug = NormalizeSlug(request.Slug!);
        if (request.ShortDescription is not null) product.ShortDescription = NullIfEmpty(request.ShortDescription);
        if (isCreate || request.Description is not null) product.Description = request.Description!.Trim();
        if (isCreate || request.CategoryId is not null) product.CategoryId = request.CategoryId!.Value;
        if (isCreate || request.Price is not null) product.Price = request.Price!.Value;
        if (isCreate || request.Unit is not null) product.Unit = request.Unit!.Trim();
        if (isCreate || request.ServingSize is not null) product.ServingSize = request.ServingSize!.Trim();
        if (isCreate || request.RsContent is not null) product.RsContent = request.RsContent!.Value;
        if (isCreate || request.RsType is not null)
            product.RsType = Enum.Parse<RsType>(request.RsType!, ignoreCase: true);
        if (request.Gi is not null) product.Gi = request.Gi;
        if (request.Fiber is not null) product.Fiber = request.Fiber;
        if (request.Ingredients is not null) product.Ingredients = NullIfEmpty(request.Ingredients);
        if (request.Usage is not null) product.Usage = NullIfEmpty(request.Usage);
        if (request.ImageUrl is not null) product.ImageUrl = NullIfEmpty(request.ImageUrl);
        if (isCreate || request.Stock is not null) product.Stock = request.Stock!.Value;
        if (request.IsActive is not null) product.IsActive = request.IsActive.Value;

        product.UpdatedAt = DateTime.UtcNow;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
