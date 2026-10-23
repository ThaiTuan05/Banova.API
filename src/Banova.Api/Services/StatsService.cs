using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface IStatsService
{
    Task<AdminStatsDto> GetAsync(CancellationToken ct = default);
}

/// <summary>Thống kê dashboard admin (API.md mục 9.3).</summary>
public class StatsService : IStatsService
{
    private readonly AppDbContext _db;

    public StatsService(AppDbContext db) => _db = db;

    public async Task<AdminStatsDto> GetAsync(CancellationToken ct = default)
    {
        return new AdminStatsDto
        {
            TotalOrders = await _db.Orders.CountAsync(ct),
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.PENDING, ct),
            CompletedOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.COMPLETED, ct),
            TotalProducts = await _db.Products.CountAsync(p => p.DeletedAt == null, ct),
            TotalArticles = await _db.Articles.CountAsync(a => a.DeletedAt == null, ct),
            TotalSubscribers = await _db.NewsletterSubscribers.CountAsync(ct),
            TotalContacts = await _db.ContactMessages.CountAsync(ct),
            TotalRevenue = await _db.Orders
                .Where(o => o.Status != OrderStatus.CANCELLED)
                .SumAsync(o => (int?)o.TotalAmount, ct) ?? 0
        };
    }
}
