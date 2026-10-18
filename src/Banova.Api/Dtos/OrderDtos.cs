using Banova.Api.Entities;

namespace Banova.Api.Dtos;

/// <summary>Một dòng sản phẩm trong đơn — FE chỉ gửi productId + quantity, KHÔNG gửi giá (NFR-09).</summary>
public sealed class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>Body của POST /api/orders (Guest checkout — không cần tài khoản).</summary>
public sealed class OrderCreateRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Note { get; set; }

    /// <summary>"COD" | "DEMO". Để dạng chuỗi rồi tự parse để trả lỗi VALIDATION rõ ràng
    /// thay vì để System.Text.Json ném lỗi khó hiểu.</summary>
    public string? PaymentMethod { get; set; }

    public List<OrderItemRequest> Items { get; set; } = new();
}

public sealed class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int UnitPrice { get; init; }
    public int Quantity { get; init; }
    public int SubTotal { get; init; }
}

public sealed class OrderDto
{
    public Guid Id { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Note { get; init; }
    public int SubTotal { get; init; }
    public int ShippingFee { get; init; }
    public int TotalAmount { get; init; }
    public OrderStatus Status { get; init; }

    /// <summary>Nhãn tiếng Việt cho FE hiển thị trực tiếp.</summary>
    public string StatusLabel { get; init; } = string.Empty;

    public PaymentMethod PaymentMethod { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
    public DateTime CreatedAt { get; init; }
}
