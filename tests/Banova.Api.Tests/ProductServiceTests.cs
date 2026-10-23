using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;

namespace Banova.Api.Tests;

public class ProductServiceTests
{
    private static ProductUpsertRequest CreateRequest(Guid categoryId, string slug = "bot-moi-500g") => new()
    {
        Name = "Bột mới 500g",
        Slug = slug,
        Description = "Mô tả",
        CategoryId = categoryId,
        Price = 159000,
        Unit = "hũ",
        ServingSize = "15g",
        RsContent = 5.8m,
        RsType = "RS2",
        Stock = 20,
        IsActive = true
    };

    [Theory]
    [InlineData("Bột Chuối Xanh 500g", "bot-chuoi-xanh-500g")]
    [InlineData("  Hộp 10 gói (150g) ", "hop-10-goi-150g")]
    [InlineData("Bánh quy Prebiotic — Banova", "banh-quy-prebiotic-banova")]
    public void NormalizeSlug_TaoSlugChuan(string input, string expected)
        => Assert.Equal(expected, ProductService.NormalizeSlug(input));

    [Fact]
    public async Task CreateAsync_HopLe_TraSanPhamMoi()
    {
        var (db, category, _) = await TestDb.CreateWithProductAsync();
        var service = new ProductService(db);

        var product = await service.CreateAsync(CreateRequest(category.Id));

        Assert.Equal("bot-moi-500g", product.Slug);
        Assert.Equal("Bột", product.Category.Name);
        Assert.Equal(159000, product.Price);
    }

    [Fact]
    public async Task CreateAsync_TrungSlug_TraConflict()
    {
        var (db, category, _) = await TestDb.CreateWithProductAsync();
        var service = new ProductService(db);

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => service.CreateAsync(CreateRequest(category.Id, slug: "bot-chuoi-xanh-250g")));

        Assert.Equal("CONFLICT", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_ThieuTruongBatBuoc_TraValidation()
    {
        var (db, category, _) = await TestDb.CreateWithProductAsync();
        var service = new ProductService(db);

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => service.CreateAsync(new ProductUpsertRequest { CategoryId = category.Id }));

        Assert.Equal("VALIDATION", ex.Code);
        var details = Assert.IsType<Dictionary<string, string>>(ex.Details);
        Assert.Contains("name", details.Keys);
        Assert.Contains("price", details.Keys);
    }

    [Fact]
    public async Task UpdateAsync_ChiCapNhatTruongDuocGui()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync();
        var service = new ProductService(db);

        var updated = await service.UpdateAsync(product.Id, new ProductUpsertRequest
        {
            Price = 99000,
            Stock = 33
        });

        Assert.Equal(99000, updated.Price);
        Assert.Equal(33, updated.Stock);
        Assert.Equal("Bột chuối xanh 250g", updated.Name);   // giữ nguyên
        Assert.Equal("bot-chuoi-xanh-250g", updated.Slug);   // giữ nguyên
    }

    [Fact]
    public async Task SoftDelete_AnKhoiDanhSachCongKhai_NhungGiuTrongDb()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync();
        var service = new ProductService(db);

        await service.SoftDeleteAsync(product.Id);

        var publicList = await service.ListAsync(new ProductQuery());
        var adminList = await service.ListAdminAsync(new ProductQuery());

        Assert.Equal(0, publicList.Total);
        Assert.Equal(0, adminList.Total);
        Assert.NotNull(db.Products.Single(p => p.Id == product.Id).DeletedAt);
    }

    [Fact]
    public async Task ListAdminAsync_ThayCaSanPhamTamAn()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(isActive: false);
        var service = new ProductService(db);

        var publicList = await service.ListAsync(new ProductQuery());
        var adminList = await service.ListAdminAsync(new ProductQuery());

        Assert.Equal(0, publicList.Total);
        Assert.Equal(1, adminList.Total);
        Assert.False(adminList.Items[0].IsActive);
        Assert.Equal(product.Id, adminList.Items[0].Id);
    }
}
