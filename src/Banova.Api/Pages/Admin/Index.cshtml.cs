using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Banova.Api.Pages.Admin;

/// <summary>Dashboard quản trị (/Admin) — số liệu tổng hợp + 6 đơn mới nhất.</summary>
public class IndexModel : PageModel
{
    private readonly IStatsService _stats;
    private readonly IOrderService _orders;

    public IndexModel(IStatsService stats, IOrderService orders)
    {
        _stats = stats;
        _orders = orders;
    }

    public AdminStatsDto Stats { get; private set; } = new();
    public IReadOnlyList<OrderDto> RecentOrders { get; private set; } = Array.Empty<OrderDto>();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Stats = await _stats.GetAsync(ct);

        var (items, _, _, _) = await _orders.ListAsync(new OrderQuery { Page = 1, PageSize = 6 }, ct);
        RecentOrders = items;
    }
}
