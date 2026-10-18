namespace Banova.Api.Dtos;

/// <summary>Một câu trả lời dạng { questionId, value } — theo API.md mục 4.1.</summary>
public sealed class QuizAnswerItem
{
    public string QuestionId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Body của POST /api/quiz/submit. FE hiện gửi answers dạng object
/// ({ goal, morning_routine, rs_experience, sensitivity }) nhưng API.md quy định dạng mảng,
/// nên nhận cả hai bằng JsonElement rồi chuẩn hoá trong service.
/// </summary>
public sealed class QuizSubmitRequest
{
    public System.Text.Json.JsonElement Answers { get; set; }
}

public sealed class QuizSubmitResponse
{
    public int Score { get; init; }
    public string Message { get; init; } = string.Empty;
    public ProductDto RecommendedProduct { get; init; } = new();
}

public sealed class ContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class NewsletterRequest
{
    public string Email { get; set; } = string.Empty;
}
