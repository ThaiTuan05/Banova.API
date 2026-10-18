using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Banova.Api.Services;

public interface IQuizService
{
    Task<QuizSubmitResponse> SubmitAsync(QuizSubmitRequest request, CancellationToken ct = default);
}

/// <summary>
/// Chấm điểm quiz theo bộ luật đơn giản (mục 11 API.md) rồi lưu QuizResult để thống kê thị hiếu.
/// </summary>
public class QuizService : IQuizService
{
    private readonly AppDbContext _db;

    public QuizService(AppDbContext db) => _db = db;

    public async Task<QuizSubmitResponse> SubmitAsync(QuizSubmitRequest request, CancellationToken ct = default)
    {
        var answers = Normalize(request.Answers);
        if (answers.Count == 0)
        {
            throw ApiException.Validation("Thiếu dữ liệu câu trả lời quiz", new { field = "answers" });
        }

        var (score, preferredSlug, preferredCategorySlug, message) = Evaluate(answers);

        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.DeletedAt == null)
            .OrderBy(p => p.Price)
            .ToListAsync(ct);

        if (products.Count == 0)
        {
            throw ApiException.Server("Chưa có sản phẩm nào để gợi ý");
        }

        // Ưu tiên: đúng slug gợi ý -> đúng danh mục gợi ý -> sản phẩm rẻ nhất còn bán
        var recommended =
            products.FirstOrDefault(p => p.Slug == preferredSlug)
            ?? products.FirstOrDefault(p => p.Category?.Slug == preferredCategorySlug)
            ?? products[0];

        _db.QuizResults.Add(new QuizResult
        {
            AnswersJson = JsonSerializer.Serialize(answers, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Score = score,
            RecommendedProductId = recommended.Id
        });
        await _db.SaveChangesAsync(ct);

        return new QuizSubmitResponse
        {
            Score = score,
            Message = message,
            RecommendedProduct = recommended.ToDto()
        };
    }

    /// <summary>
    /// Nhận cả 2 định dạng: mảng [{ questionId, value }] (API.md) và object { goal: "..." } (FE hiện tại).
    /// </summary>
    private static Dictionary<string, string> Normalize(JsonElement answers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (answers.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in answers.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var id = item.TryGetProperty("questionId", out var qid) ? qid.GetString() : null;
                var value = item.TryGetProperty("value", out var val) ? val.GetString() : null;
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(value))
                {
                    result[id!] = value!;
                }
            }
        }
        else if (answers.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in answers.EnumerateObject())
            {
                // Bỏ qua trường hợp FE gửi lồng { answers: {...} } hoặc giá trị không phải chuỗi
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    result[prop.Name] = prop.Value.GetString()!;
                }
            }
        }

        return result;
    }

    private static (int Score, string? PreferredSlug, string PreferredCategorySlug, string Message) Evaluate(
        IReadOnlyDictionary<string, string> answers)
    {
        var score = 6;
        string? preferredSlug = null;
        var category = "bot";
        var message = "Phù hợp nhất với bạn là bột tinh bột kháng Banova.";

        if (answers.TryGetValue("goal", out var goal))
        {
            switch (goal)
            {
                case "blood-sugar":
                    score += 2;
                    category = "bot";
                    message = "Với mục tiêu kiểm soát đường huyết, bột tinh bột kháng nguyên chất là lựa chọn phù hợp nhất.";
                    break;
                case "gut-health":
                    score += 2;
                    category = "bot";
                    message = "Với mục tiêu chăm sóc hệ vi sinh đường ruột, bột tinh bột kháng nguyên chất là lựa chọn phù hợp nhất.";
                    break;
                case "weight-control":
                    score += 1;
                    category = "banh";
                    message = "Với mục tiêu kiểm soát cân nặng, nhóm bánh dinh dưỡng ít đường sẽ giúp bạn duy trì cảm giác no lâu.";
                    break;
                case "family-breakfast":
                    score += 1;
                    category = "bua-sang";
                    message = "Với nhu cầu bữa sáng cho cả gia đình, hộp bữa sáng dinh dưỡng là gợi ý phù hợp.";
                    break;
            }
        }

        if (answers.TryGetValue("morning_routine", out var morning))
        {
            switch (morning)
            {
                case "speed":
                    score += 1;
                    preferredSlug ??= "bot-banova-hop-10-goi-15g";
                    break;
                case "smoothie":
                    score += 1;
                    preferredSlug ??= "bot-tinh-bot-khang-banova-250g";
                    break;
                case "bakery":
                    score += 1;
                    category = "banh";
                    break;
            }
        }

        if (answers.TryGetValue("rs_experience", out var experience))
        {
            switch (experience)
            {
                case "new":
                    score += 1;
                    preferredSlug ??= "bot-tinh-bot-khang-banova-250g";
                    break;
                case "known":
                    score += 1;
                    break;
                case "experienced":
                    score += 1;
                    preferredSlug ??= "bot-tinh-bot-khang-banova-500g";
                    break;
            }
        }

        if (answers.TryGetValue("sensitivity", out var sensitivity))
        {
            switch (sensitivity)
            {
                case "normal":
                    score += 1;
                    break;
                // Nhạy cảm tiêu hoá: gợi ý nhóm dễ dung nạp hơn, chia liều nhỏ
                case "bloating":
                    score += 1;
                    preferredSlug ??= "bot-banova-hop-10-goi-15g";
                    break;
                case "ibs":
                    category = "bua-sang";
                    break;
            }
        }

        return (Math.Clamp(score, 0, 10), preferredSlug, category, message);
    }
}
