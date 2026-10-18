namespace Banova.Api.Entities;

/// <summary>Danh mục sản phẩm: Bột · Bánh · Bữa sáng · Hộp quà.</summary>
public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Slug dùng cho bộ lọc ?category=bot. UNIQUE.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
