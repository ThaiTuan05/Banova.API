namespace Banova.Api.Entities;

/// <summary>Kết quả quiz tư vấn — lưu lại để thống kê thị hiếu người dùng (FR-04).</summary>
public class QuizResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Câu trả lời nguyên bản, lưu dạng JSON.</summary>
    public string AnswersJson { get; set; } = string.Empty;

    public int Score { get; set; }

    public Guid? RecommendedProductId { get; set; }

    public Product? RecommendedProduct { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
