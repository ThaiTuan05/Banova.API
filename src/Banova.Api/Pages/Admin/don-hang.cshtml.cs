using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Banova.Api.Pages.Admin;

/// <summary>
/// Quản lý đơn hàng (/Admin/don-hang): lọc theo trạng thái + đổi trạng thái.
/// Huỷ đơn sẽ tự hoàn tồn kho (xử lý trong <see cref="IOrderService"/>).
/// </summary>
public class OrdersModel : PageModel
{
    private readonly IOrderService _orders;

    public OrdersModel(IOrderService orders) => _orders = orders;

    public IReadOnlyList<OrderDto> Orders { get; private set; } = Array.Empty<OrderDto>();
    public int Total { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostStatusAsync(Guid id, string? status, CancellationToken ct)
    {
        try
        {
            var order = await _orders.UpdateStatusAsync(id, status, ct);
            TempData["Message"] = $"Đơn {order.OrderCode} đã chuyển sang “{order.StatusLabel}”.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("/Admin/don-hang", new { status = Status, q = Q });
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var (items, total, _, _) = await _orders.ListAsync(new OrderQuery
        {
            Page = 1,
            PageSize = 50,
            Status = Status,
            Q = Q
        }, ct);

        Orders = items;
        Total = total;
    }
}
