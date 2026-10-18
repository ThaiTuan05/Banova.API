using System.Text.Json.Serialization;

namespace Banova.Api.Common;

/// <summary>Meta phân trang — khớp API.md mục 0.1.</summary>
public sealed class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>Vỏ bọc thành công: { "data": ..., "meta": ... } — meta chỉ xuất hiện khi phân trang.</summary>
public sealed class ApiSuccess<T>
{
    public T Data { get; init; } = default!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Meta { get; init; }
}

public sealed record ApiErrorBody(string Code, string Message)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; init; }
}

/// <summary>Vỏ bọc lỗi: { "error": { "code", "message", "details" } }.</summary>
public sealed class ApiErrorResponse
{
    public ApiErrorBody Error { get; init; } = default!;
}

/// <summary>Helper tạo response đúng chuẩn — thay cho ok()/fail() của bản Next.js.</summary>
public static class ApiResult
{
    public static ApiSuccess<T> Ok<T>(T data) => new() { Data = data };

    public static ApiSuccess<IReadOnlyList<T>> OkPaged<T>(
        IReadOnlyList<T> items, int page, int pageSize, int total)
        => new()
        {
            Data = items,
            Meta = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
            }
        };

    public static ApiErrorResponse Fail(string code, string message, object? details = null)
        => new() { Error = new ApiErrorBody(code, message) { Details = details } };
}
