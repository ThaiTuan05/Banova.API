using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Lib;
using Banova.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Banova.Api.Controllers;

/// <summary>
/// API đặt hàng mô phỏng (API.md mục 5) + quản trị đơn hàng (mục 9).
/// POST/GET theo mã đơn là public (Guest checkout); danh sách &amp; đổi trạng thái yêu cầu ADMIN.
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>POST /api/orders → 201 { data: Order }</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> Create([FromBody] OrderCreateRequest request, CancellationToken ct)
    {
        var order = await _orders.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResult.Ok(order));
    }

    /// <summary>GET /api/orders?page=&amp;pageSize=&amp;status=&amp;q= — danh sách cho admin.</summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> List([FromQuery] OrderQuery query, CancellationToken ct)
    {
        var (items, total, page, pageSize) = await _orders.ListAsync(query, ct);
        return Ok(ApiResult.OkPaged(items, page, pageSize, total));
    }

    /// <summary>GET /api/orders/{orderCode} → xem lại đơn vừa đặt.</summary>
    [HttpGet("{orderCode}")]
    public async Task<IActionResult> DetailByCode(string orderCode, CancellationToken ct)
        => Ok(ApiResult.Ok(await _orders.GetByCodeAsync(orderCode, ct)));

    /// <summary>PATCH /api/orders/{id}/status — đổi trạng thái đơn.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] OrderStatusUpdateRequest request, CancellationToken ct)
        => Ok(ApiResult.Ok(await _orders.UpdateStatusAsync(id, request.Status, ct)));
}
