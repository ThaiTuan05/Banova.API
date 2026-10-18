using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>API công khai — Danh sách &amp; chi tiết sản phẩm (API.md mục 2.1, 2.2).</summary>
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    /// <summary>GET /api/products?page=&amp;pageSize=&amp;category=&amp;q=&amp;sort=</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProductQuery query, CancellationToken ct)
    {
        var (items, total, page, pageSize) = await _products.ListAsync(query, ct);
        return Ok(ApiResult.OkPaged(items, page, pageSize, total));
    }

    /// <summary>GET /api/products/{slug}</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
        => Ok(ApiResult.Ok(await _products.GetBySlugAsync(slug, ct)));
}
