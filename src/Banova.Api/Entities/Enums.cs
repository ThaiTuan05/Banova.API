namespace Banova.Api.Entities;

/// <summary>Vai trò người dùng. Demo chỉ dùng ADMIN (bỏ đăng nhập khách hàng).</summary>
public enum Role
{
    ADMIN,
    CUSTOMER
}

/// <summary>Phân loại bài viết: Khoa học | Lối sống - Blog.</summary>
public enum ArticleType
{
    SCIENCE,
    BLOG
}

/// <summary>Trạng thái xuất bản bài viết.</summary>
public enum PublishState
{
    DRAFT,
    PUBLISHED
}

/// <summary>Trạng thái đơn hàng (nhãn tiếng Việt xem Constants.OrderStatusLabels).</summary>
public enum OrderStatus
{
    PENDING,
    CONFIRMED,
    DELIVERING,
    COMPLETED,
    CANCELLED
}

/// <summary>Phương thức thanh toán. Demo không có thanh toán thật.</summary>
public enum PaymentMethod
{
    COD,
    DEMO
}

/// <summary>Loại tinh bột kháng: RS2 (chuối xanh tự nhiên) | RS3 (hồi tinh).</summary>
public enum RsType
{
    RS2,
    RS3
}
