namespace Banova.Api.Entities;

/// <summary>Sản phẩm dinh dưỡng tinh bột kháng. Soft delete qua DeletedAt.</summary>
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Slug dùng cho URL chi tiết sản phẩm. UNIQUE.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string Description { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }

    /// <summary>Đơn giá VND — lưu số nguyên, không dùng số thực (mục 4.8).</summary>
    public int Price { get; set; }

    /// <summary>Quy cách đóng gói: hũ | gói | ổ | hộp | cái.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Kích cỡ khẩu phần, ví dụ "15g".</summary>
    public string ServingSize { get; set; } = string.Empty;

    /// <summary>Hàm lượng tinh bột kháng (gam / khẩu phần).</summary>
    public decimal RsContent { get; set; }

    public RsType RsType { get; set; } = RsType.RS2;

    /// <summary>Chỉ số đường huyết — chỉ số tham khảo, có thể null nếu chưa kiểm nghiệm.</summary>
    public int? Gi { get; set; }

    /// <summary>Chất xơ (gam / khẩu phần).</summary>
    public decimal? Fiber { get; set; }

    public string? Ingredients { get; set; }

    public string? Usage { get; set; }

    public string? ImageUrl { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
