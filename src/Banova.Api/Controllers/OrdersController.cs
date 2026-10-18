using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>
/// API đặt hàng mô phỏng (API.md mục 5.1, 5.2). Public — Guest checkout, không cần đăng nhập.
/// FE không gửi giá tiền lên; server tự tính từ DB (NFR-09).
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>POST /api/orders → 201 { data: Order }</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderCreateRequest request, CancellationToken ct)
    {
        var order = await _orders.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResult.Ok(order));
    }

    /// <summary>GET /api/orders/{orderCode} → xem lại đơn vừa đặt</summary>
    [HttpGet("{orderCode}")]
    public async Task<IActionResult> DetailByCode(string orderCode, CancellationToken ct)
        => Ok(ApiResult.Ok(await _orders.GetByCodeAsync(orderCode, ct)));
}
