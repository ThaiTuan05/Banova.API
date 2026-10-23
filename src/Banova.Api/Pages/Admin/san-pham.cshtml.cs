using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Banova.Api.Pages.Admin;

/// <summary>
/// Quản lý sản phẩm (/Admin/san-pham): danh sách + thêm/sửa + xoá mềm.
/// Toàn bộ nghiệp vụ nằm trong <see cref="IProductService"/> — page chỉ điều phối.
/// </summary>
public class ProductsModel : PageModel
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;

    public ProductsModel(IProductService products, ICategoryService categories)
    {
        _products = products;
        _categories = categories;
    }

    public IReadOnlyList<ProductDto> Products { get; private set; } = Array.Empty<ProductDto>();
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = Array.Empty<CategoryDto>();

    [BindProperty]
    public ProductUpsertRequest Input { get; set; } = new();

    [BindProperty]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public async Task OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is null)
        {
            // Giá trị mặc định cho form thêm mới
            Input = new ProductUpsertRequest
            {
                IsActive = true,
                RsType = "RS2",
                Unit = "hũ",
                ServingSize = "15g",
                Stock = 0
            };
        }
        else
        {
            var product = await _products.GetByIdAsync(id.Value, ct);
            Id = id;

            Input = new ProductUpsertRequest
            {
                Name = product.Name,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                CategoryId = product.Category.Id,
                Price = product.Price,
                Unit = product.Unit,
                ServingSize = product.ServingSize,
                RsContent = product.RsContent,
                RsType = product.RsType.ToString(),
                Gi = product.Gi,
                Fiber = product.Fiber,
                Ingredients = product.Ingredients,
                Usage = product.Usage,
                ImageUrl = product.ImageUrl,
                Stock = product.Stock,
                IsActive = product.IsActive
            };
        }

        await LoadListsAsync(ct);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        try
        {
            if (Id is not null)
            {
                await _products.UpdateAsync(Id.Value, Input, ct);
                TempData["Message"] = "Đã cập nhật sản phẩm.";
            }
            else
            {
                await _products.CreateAsync(Input, ct);
                TempData["Message"] = "Đã thêm sản phẩm mới.";
            }

            return RedirectToPage("/Admin/san-pham");
        }
        catch (ApiException ex)
        {
            TempData["Error"] = BuildErrorMessage(ex);
            await LoadListsAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await _products.SoftDeleteAsync(id, ct);
            TempData["Message"] = "Đã xoá mềm sản phẩm (vẫn giữ trong lịch sử đơn hàng).";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("/Admin/san-pham");
    }

    private async Task LoadListsAsync(CancellationToken ct)
    {
        var (items, _, _, _) = await _products.ListAdminAsync(
            new ProductQuery { Page = 1, PageSize = 50, Q = Q }, ct);

        Products = items;
        Categories = await _categories.ListAsync(ct);
    }

    private static string BuildErrorMessage(ApiException ex)
    {
        if (ex.Details is Dictionary<string, string> fieldErrors && fieldErrors.Count > 0)
        {
            return $"{ex.Message}: {string.Join(" · ", fieldErrors.Select(kv => $"{kv.Key} — {kv.Value}"))}";
        }

        return ex.Message;
    }
}
