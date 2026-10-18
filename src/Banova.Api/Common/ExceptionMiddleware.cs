using System.Text.Json;

namespace Banova.Api.Common;

/// <summary>
/// Bắt mọi lỗi và trả JSON đúng chuẩn API.md thay vì HTML/ProblemDetails mặc định của ASP.NET.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message, ex.Details);
        }
        catch (Exception ex)
        {
            // Không lộ chi tiết hệ thống ra ngoài, chỉ log ở server (mục 4.6)
            _logger.LogError(ex, "Unhandled exception at {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "SERVER_ERROR", "Lỗi hệ thống", null);
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message, object? details)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = ApiResult.Fail(code, message, details);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
