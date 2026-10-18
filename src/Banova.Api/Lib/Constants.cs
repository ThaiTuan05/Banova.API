using Banova.Api.Entities;

namespace Banova.Api.Lib;

/// <summary>Các hằng số + nhãn tiếng Việt dùng chung. Muốn sửa phải báo cả nhóm (mục 12.3).</summary>
public static class Constants
{
    /// <summary>pageSize mặc định theo API.md mục 0.4.</summary>
    public const int DefaultPageSize = 12;

    /// <summary>pageSize tối đa cho phép — chặn client xin quá nhiều dữ liệu một lúc.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Phí giao hàng demo = 0 (Freeship tài trợ).</summary>
    public const int DemoShipFee = 0;

    public static readonly IReadOnlyDictionary<OrderStatus, string> OrderStatusLabels = new Dictionary<OrderStatus, string>
    {
        [OrderStatus.PENDING] = "Chờ xử lý",
        [OrderStatus.CONFIRMED] = "Đã xác nhận",
        [OrderStatus.DELIVERING] = "Đang giao",
        [OrderStatus.COMPLETED] = "Hoàn thành",
        [OrderStatus.CANCELLED] = "Đã hủy"
    };

    public static readonly IReadOnlyDictionary<ArticleType, string> ArticleTypeLabels = new Dictionary<ArticleType, string>
    {
        [ArticleType.SCIENCE] = "Khoa học",
        [ArticleType.BLOG] = "Lối sống"
    };

    public static readonly IReadOnlyDictionary<PublishState, string> PublishStateLabels = new Dictionary<PublishState, string>
    {
        [PublishState.DRAFT] = "Bản nháp",
        [PublishState.PUBLISHED] = "Đã đăng"
    };

    public static string OrderStatusLabel(OrderStatus status)
        => OrderStatusLabels.TryGetValue(status, out var label) ? label : status.ToString();
}
