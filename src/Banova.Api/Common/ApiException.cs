namespace Banova.Api.Common;

/// <summary>
/// Lỗi nghiệp vụ có mã + HTTP status chuẩn. Service ném lỗi này,
/// ExceptionMiddleware bắt và trả về đúng format { error: { code, message, details } }.
/// </summary>
public class ApiException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public object? Details { get; }

    public ApiException(string code, string message, int statusCode = 400, object? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    public static ApiException Validation(string message = "Dữ liệu không hợp lệ", object? details = null)
        => new("VALIDATION", message, StatusCodes.Status400BadRequest, details);

    public static ApiException Unauthorized(string message = "Chưa đăng nhập", object? details = null)
        => new("UNAUTHORIZED", message, StatusCodes.Status401Unauthorized, details);

    public static ApiException Forbidden(string message = "Đã đăng nhập nhưng không đủ quyền", object? details = null)
        => new("FORBIDDEN", message, StatusCodes.Status403Forbidden, details);

    public static ApiException NotFound(string message = "Không tìm thấy tài nguyên", object? details = null)
        => new("NOT_FOUND", message, StatusCodes.Status404NotFound, details);

    public static ApiException Conflict(string message = "Dữ liệu đã tồn tại", object? details = null)
        => new("CONFLICT", message, StatusCodes.Status409Conflict, details);

    public static ApiException Server(string message = "Lỗi hệ thống", object? details = null)
        => new("SERVER_ERROR", message, StatusCodes.Status500InternalServerError, details);
}
