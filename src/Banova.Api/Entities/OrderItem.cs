namespace Banova.Api.Entities;

/// <summary>Chi tiết món trong đơn. Snapshot tên + giá tại thời điểm mua (NFR-10).</summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Order? Order { get; set; }

    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    /// <summary>Tên sản phẩm lúc mua — không đổi dù sản phẩm bị sửa tên sau này.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Đơn giá lúc mua (VND).</summary>
    public int UnitPrice { get; set; }

    public int Quantity { get; set; }

    public int SubTotal { get; set; }
}
