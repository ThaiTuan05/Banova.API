using Banova.Api.Common;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>API công khai — Danh mục sản phẩm (API.md mục 2.3).</summary>
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    /// <summary>GET /api/categories</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(ApiResult.Ok(await _categories.ListAsync(ct)));
}
