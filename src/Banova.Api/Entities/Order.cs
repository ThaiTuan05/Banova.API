namespace Banova.Api.Entities;

/// <summary>Đơn hàng mô phỏng (Guest checkout — không cần tài khoản).</summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Mã đơn hiển thị cho khách, định dạng BNV-yyyyMMdd-XXX. UNIQUE.</summary>
    public string OrderCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? Note { get; set; }

    /// <summary>Toàn bộ tiền do server tính từ DB, KHÔNG nhận từ client (NFR-09).</summary>
    public int SubTotal { get; set; }

    public int ShippingFee { get; set; }

    public int TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PENDING;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.DEMO;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
