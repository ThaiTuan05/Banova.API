using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>API quản trị sản phẩm (API.md mục 7) — toàn bộ yêu cầu quyền ADMIN.</summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "ADMIN")]
public class AdminProductsController : ControllerBase
{
    private readonly IProductService _products;

    public AdminProductsController(IProductService products) => _products = products;

    /// <summary>GET /api/admin/products — danh sách cho trang quản trị (gồm cả sản phẩm đang tạm ẩn).</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProductQuery query, CancellationToken ct)
    {
        var (items, total, page, pageSize) = await _products.ListAdminAsync(query, ct);
        return Ok(ApiResult.OkPaged(items, page, pageSize, total));
    }

    /// <summary>POST /api/admin/products → 201 { data: Product }</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductUpsertRequest request, CancellationToken ct)
    {
        var product = await _products.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResult.Ok(product));
    }

    /// <summary>PUT /api/admin/products/{id} → 200 { data: Product }</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpsertRequest request, CancellationToken ct)
        => Ok(ApiResult.Ok(await _products.UpdateAsync(id, request, ct)));

    /// <summary>DELETE /api/admin/products/{id} → xoá mềm → 200 { data: { success: true } }</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _products.SoftDeleteAsync(id, ct);
        return Ok(ApiResult.Ok(new { success = true }));
    }
}
