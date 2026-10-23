using Banova.Api.Data;
using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Banova.Api.Tests;

/// <summary>DbContext InMemory cho unit test — mỗi test một database riêng.</summary>
internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"banova-test-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    public static async Task<(AppDbContext Db, Category Category, Product Product)> CreateWithProductAsync(
        int stock = 10, int price = 89000, bool isActive = true)
    {
        var db = Create();

        var category = new Category { Name = "Bột", Slug = "bot", SortOrder = 1 };

        var product = new Product
        {
            Name = "Bột chuối xanh 250g",
            Slug = "bot-chuoi-xanh-250g",
            Description = "Mô tả sản phẩm",
            CategoryId = category.Id,
            Price = price,
            Unit = "hũ",
            ServingSize = "15g",
            RsContent = 5.8m,
            RsType = RsType.RS2,
            Gi = 39,
            Fiber = 3.2m,
            Stock = stock,
            IsActive = isActive
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (db, category, product);
    }
}
