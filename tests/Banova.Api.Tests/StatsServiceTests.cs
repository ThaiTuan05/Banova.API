using Banova.Api.Entities;
using Banova.Api.Services;

namespace Banova.Api.Tests;

public class StatsServiceTests
{
    [Fact]
    public async Task GetAsync_DemDungVaTruDonHuyKhoiDoanhThu()
    {
        var db = TestDb.Create();

        db.Products.Add(new Product { Name = "SP 1", Slug = "sp-1", Description = "x", Unit = "hũ", ServingSize = "15g" });
        db.Products.Add(new Product { Name = "SP 2", Slug = "sp-2", Description = "x", Unit = "hũ", ServingSize = "15g" });
        db.Products.Add(new Product { Name = "SP xoá", Slug = "sp-3", Description = "x", Unit = "hũ", ServingSize = "15g", DeletedAt = DateTime.UtcNow });

        db.Articles.Add(new Article { Title = "A", Slug = "a", Content = "x" });
        db.Articles.Add(new Article { Title = "B", Slug = "b", Content = "x", DeletedAt = DateTime.UtcNow });

        db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = "a@test.vn" });
        db.ContactMessages.Add(new ContactMessage { Name = "A", Email = "a@test.vn", Phone = "0905", Message = "hi" });

        db.Orders.Add(new Order { OrderCode = "BNV-1", CustomerName = "A", Phone = "0905", Address = "x", TotalAmount = 100000, Status = OrderStatus.PENDING });
        db.Orders.Add(new Order { OrderCode = "BNV-2", CustomerName = "B", Phone = "0905", Address = "x", TotalAmount = 200000, Status = OrderStatus.COMPLETED });
        db.Orders.Add(new Order { OrderCode = "BNV-3", CustomerName = "C", Phone = "0905", Address = "x", TotalAmount = 500000, Status = OrderStatus.CANCELLED });

        await db.SaveChangesAsync();

        var stats = await new StatsService(db).GetAsync();

        Assert.Equal(3, stats.TotalOrders);
        Assert.Equal(1, stats.PendingOrders);
        Assert.Equal(1, stats.CompletedOrders);
        Assert.Equal(2, stats.TotalProducts);      // bỏ sản phẩm đã xoá mềm
        Assert.Equal(1, stats.TotalArticles);      // bỏ bài đã xoá mềm
        Assert.Equal(1, stats.TotalSubscribers);
        Assert.Equal(1, stats.TotalContacts);
        Assert.Equal(300000, stats.TotalRevenue);  // không tính đơn CANCELLED
    }
}
