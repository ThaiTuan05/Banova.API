using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Banova.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banova.Api.Tests;

public class OrderServiceTests
{
    private static OrderService CreateService(Data.AppDbContext db)
        => new(db, NullLogger<OrderService>.Instance);

    private static OrderCreateRequest Request(Guid productId, int quantity = 1, string payment = "DEMO") => new()
    {
        CustomerName = "Nguyễn Văn A",
        Phone = "0905123456",
        Address = "123 Lê Lợi, Quy Nhơn",
        PaymentMethod = payment,
        Items = new List<OrderItemRequest> { new() { ProductId = productId, Quantity = quantity } }
    };

    [Fact]
    public async Task CreateAsync_TinhTienTuDb_VaTruTonKho()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(stock: 10, price: 89000);
        var service = CreateService(db);

        var order = await service.CreateAsync(Request(product.Id, quantity: 2));

        Assert.Equal(178000, order.SubTotal);
        Assert.Equal(0, order.ShippingFee);
        Assert.Equal(178000, order.TotalAmount);
        Assert.Equal(OrderStatus.PENDING, order.Status);
        Assert.Equal("Chờ xử lý", order.StatusLabel);
        Assert.Single(order.Items);
        Assert.Equal(89000, order.Items[0].UnitPrice);
        Assert.Equal(2, order.Items[0].Quantity);
        Assert.Matches(@"^BNV-\d{8}-\d{3}$", order.OrderCode);

        var saved = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        Assert.Equal(8, saved.Stock);
    }

    [Fact]
    public async Task CreateAsync_GoiYTrungSanPham_TruKhoDungMotLan()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(stock: 5);
        var service = CreateService(db);

        var request = Request(product.Id, quantity: 1);
        request.Items.Add(new OrderItemRequest { ProductId = product.Id, Quantity = 2 });

        var order = await service.CreateAsync(request);

        Assert.Single(order.Items);
        Assert.Equal(3, order.Items[0].Quantity);

        var saved = await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id);
        Assert.Equal(2, saved.Stock);
    }

    [Fact]
    public async Task CreateAsync_KhongDuHang_TraConflict()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(stock: 2);

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => CreateService(db).CreateAsync(Request(product.Id, quantity: 3)));

        Assert.Equal("CONFLICT", ex.Code);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_SanPhamKhongTonTai_TraNotFound()
    {
        var (db, _, _) = await TestDb.CreateWithProductAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => CreateService(db).CreateAsync(Request(Guid.NewGuid())));

        Assert.Equal("NOT_FOUND", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_PhuongThucThanhToanSai_TraValidation()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => CreateService(db).CreateAsync(Request(product.Id, payment: "VNPAY")));

        Assert.Equal("VALIDATION", ex.Code);
    }

    [Fact]
    public async Task UpdateStatus_HuyDon_HoanTonKho()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(stock: 10);
        var service = CreateService(db);

        var order = await service.CreateAsync(Request(product.Id, quantity: 3));
        Assert.Equal(7, (await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id)).Stock);

        var cancelled = await service.UpdateStatusAsync(order.Id, "CANCELLED");

        Assert.Equal(OrderStatus.CANCELLED, cancelled.Status);
        Assert.Equal("Đã hủy", cancelled.StatusLabel);
        Assert.Equal(10, (await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id)).Stock);
    }

    [Fact]
    public async Task UpdateStatus_MoLaiDonDaHuy_TruKhoLai()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(stock: 10);
        var service = CreateService(db);

        var order = await service.CreateAsync(Request(product.Id, quantity: 2));
        await service.UpdateStatusAsync(order.Id, "CANCELLED");

        var reopened = await service.UpdateStatusAsync(order.Id, "CONFIRMED");

        Assert.Equal(OrderStatus.CONFIRMED, reopened.Status);
        Assert.Equal(8, (await db.Products.AsNoTracking().FirstAsync(p => p.Id == product.Id)).Stock);
    }

    [Fact]
    public async Task UpdateStatus_TrangThaiSai_TraValidation()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync();
        var service = CreateService(db);
        var order = await service.CreateAsync(Request(product.Id));

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => service.UpdateStatusAsync(order.Id, "DANG_GIAO"));

        Assert.Equal("VALIDATION", ex.Code);
    }

    [Fact]
    public async Task ListAsync_LocTheoTrangThai_VaTimKiem()
    {
        var (db, _, product) = await TestDb.CreateWithProductAsync(stock: 20);
        var service = CreateService(db);

        var first = await service.CreateAsync(Request(product.Id));
        await service.CreateAsync(Request(product.Id));

        var pending = await service.ListAsync(new OrderQuery { Status = "PENDING" });
        Assert.Equal(2, pending.Total);

        var byCode = await service.ListAsync(new OrderQuery { Q = first.OrderCode });
        Assert.Equal(1, byCode.Total);
        Assert.Equal(first.OrderCode, byCode.Items[0].OrderCode);

        var byPhone = await service.ListAsync(new OrderQuery { Q = "0905" });
        Assert.Equal(2, byPhone.Total);

        var cancelled = await service.ListAsync(new OrderQuery { Status = "CANCELLED" });
        Assert.Equal(0, cancelled.Total);
    }
}
