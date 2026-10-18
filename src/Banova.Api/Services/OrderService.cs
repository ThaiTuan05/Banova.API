using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Banova.Api.Lib;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Banova.Api.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(OrderCreateRequest request, CancellationToken ct = default);
    Task<OrderDto> GetByCodeAsync(string orderCode, CancellationToken ct = default);
}

/// <summary>
/// Nghiệp vụ đặt hàng mô phỏng (Guest checkout).
/// Nguyên tắc bắt buộc: server tự tra giá từ DB, KHÔNG tin giá do client gửi lên (NFR-09);
/// mỗi OrderItem lưu snapshot tên + giá tại thời điểm mua (NFR-10).
/// </summary>
public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;

    /// <summary>Mã đơn hiển thị cho khách nên tính theo giờ Việt Nam (UTC+7), không theo UTC.</summary>
    private const int VietnamUtcOffsetHours = 7;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(OrderCreateRequest request, CancellationToken ct = default)
    {
        // ---------- 1. Validate đầu vào ----------
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            errors["customerName"] = "Vui lòng nhập họ tên";

        if (string.IsNullOrWhiteSpace(request.Phone))
            errors["phone"] = "Vui lòng nhập số điện thoại";
        else if (!Regex.IsMatch(request.Phone.Trim(), @"^[0-9+\-\s]{9,15}$"))
            errors["phone"] = "Số điện thoại không hợp lệ";

        if (string.IsNullOrWhiteSpace(request.Address))
            errors["address"] = "Vui lòng nhập địa chỉ giao hàng";

        if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@'))
            errors["email"] = "Email không hợp lệ";

        var payment = PaymentMethod.DEMO;
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod) &&
            !Enum.TryParse(request.PaymentMethod.Trim(), ignoreCase: true, out payment))
        {
            errors["paymentMethod"] = "Phương thức thanh toán không hợp lệ (COD | DEMO)";
        }

        if (errors.Count > 0)
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);

        if (request.Items is null || request.Items.Count == 0)
            throw ApiException.Validation("Đơn hàng phải có ít nhất 1 sản phẩm", new { field = "items" });

        // Gộp các dòng trùng sản phẩm để kiểm tra tồn kho chính xác
        var lines = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (lines.Any(l => l.Quantity <= 0))
            throw ApiException.Validation("Số lượng sản phẩm phải lớn hơn 0", new { field = "items" });

        // ---------- 2. Tra giá từ DB (KHÔNG tin giá từ client) ----------
        var productIds = lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.Id, ct);

        var missingIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw ApiException.NotFound(
                "Sản phẩm không tồn tại hoặc đã ngừng bán",
                new { productIds = missingIds });
        }

        // ---------- 3. Kiểm tra tồn kho ----------
        var outOfStock = lines
            .Where(l => products[l.ProductId].Stock < l.Quantity)
            .Select(l => new
            {
                productId = l.ProductId,
                name = products[l.ProductId].Name,
                stock = products[l.ProductId].Stock,
                requested = l.Quantity
            })
            .ToList();

        if (outOfStock.Count > 0)
            throw ApiException.Conflict("Một số sản phẩm không đủ hàng", outOfStock);

        // ---------- 4. Tạo đơn + snapshot tên/giá ----------
        var order = new Order
        {
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = request.Address.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            PaymentMethod = payment,
            Status = OrderStatus.PENDING,
            ShippingFee = Constants.DemoShipFee
        };

        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,      // snapshot (NFR-10)
                UnitPrice = product.Price,       // snapshot (NFR-10)
                Quantity = line.Quantity,
                SubTotal = product.Price * line.Quantity
            });
        }

        order.SubTotal = order.Items.Sum(i => i.SubTotal);
        order.TotalAmount = order.SubTotal + order.ShippingFee;

        // ---------- 5. Sinh mã đơn BNV-yyyyMMdd-XXX ----------
        order.OrderCode = await GenerateOrderCodeAsync(ct);

        // ---------- 6. Trừ tồn kho ----------
        foreach (var line in lines)
        {
            products[line.ProductId].Stock -= line.Quantity;
        }

        _db.Orders.Add(order);

        try
        {
            // Một SaveChanges = một transaction ngầm: hoặc lưu cả đơn + trừ kho, hoặc không lưu gì
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Chỉ số UNIQUE duy nhất có thể vỡ ở đây là orderCode (tranh chấp đồng thời)
            _logger.LogWarning(ex, "Không lưu được đơn hàng mới, mã đơn {OrderCode}", order.OrderCode);
            throw ApiException.Conflict("Mã đơn vừa bị trùng, vui lòng đặt lại");
        }

        return order.ToDto();
    }

    public async Task<OrderDto> GetByCodeAsync(string orderCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
            throw ApiException.Validation("Thiếu mã đơn hàng");

        var code = orderCode.Trim();
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderCode == code, ct);

        if (order is null)
            throw ApiException.NotFound($"Không tìm thấy đơn hàng '{code}'");

        return order.ToDto();
    }

    /// <summary>
    /// Mã đơn dạng BNV-yyyyMMdd-XXX, XXX là số thứ tự trong ngày theo giờ Việt Nam.
    /// Dò tuần tự tới khi tìm được mã chưa tồn tại; chỉ số UNIQUE ở DB là chốt chặn cuối.
    /// </summary>
    private async Task<string> GenerateOrderCodeAsync(CancellationToken ct)
    {
        var vietnamNow = DateTime.UtcNow.AddHours(VietnamUtcOffsetHours);
        var vietnamDay = vietnamNow.Date;

        // Đổi mốc nửa đêm giờ VN về UTC để truy vấn cho đúng
        var utcDayStart = vietnamDay.AddHours(-VietnamUtcOffsetHours);
        var utcDayEnd = utcDayStart.AddDays(1);

        var countToday = await _db.Orders
            .CountAsync(o => o.CreatedAt >= utcDayStart && o.CreatedAt < utcDayEnd, ct);

        var sequence = countToday + 1;
        string code;

        do
        {
            code = $"BNV-{vietnamDay:yyyyMMdd}-{sequence:D3}";
            sequence++;
        }
        while (await _db.Orders.AnyAsync(o => o.OrderCode == code, ct));

        return code;
    }
}
