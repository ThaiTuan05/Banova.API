using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Lib;
using Banova.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "BanovaFe";
const string JsonContentType = "application/json; charset=utf-8";

// ---------- Controllers & JSON ----------
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Enum trả về chuỗi ("PENDING", "SCIENCE", "RS2") đúng như API.md quy định
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Field null được lược bỏ cho payload gọn (meta chỉ hiện khi phân trang)
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    // Giữ tiếng Việt đọc được trong response thay vì \u00F4... (dễ debug cho FE)
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// Model binding lỗi của ASP.NET mặc định trả ProblemDetails (RFC 7807),
// phải ép về format { error: { code, message, details } } cho khớp API.md.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(kv => kv.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(ApiResult.Fail("VALIDATION", "Dữ liệu không hợp lệ", details));
    };
});

// ---------- JWT (API.md mục 6) ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret phải dài ít nhất 32 ký tự. Khi deploy hãy đặt biến môi trường Jwt__Secret.");
}

if (builder.Environment.IsProduction() &&
    jwtOptions.Secret.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Production chưa cấu hình Jwt__Secret thật (đang dùng giá trị REPLACE_ME trong appsettings.Production.json).");
}

builder.Services.Configure<JwtOptions>(jwtSection);

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("Default");

if (builder.Environment.IsProduction() &&
    (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException(
        "Production cần biến môi trường ConnectionStrings__Default trỏ tới SQL Server thật (không dùng LocalDB).");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// ---------- Nghiệp vụ (DI) ----------
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IStatsService, StatsService>();

// ---------- Xác thực: JWT trong header hoặc cookie httpOnly ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            // Cho phép trình duyệt gửi token qua cookie (trang admin Razor) hoặc header (FE/Scalar)
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(Constants.AuthCookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },

            // 401: API trả JSON đúng chuẩn; trang Razor chuyển về trang đăng nhập
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = JsonContentType;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(
                        ApiResult.Fail("UNAUTHORIZED", "Chưa đăng nhập hoặc phiên đã hết hạn"),
                        new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                    return;
                }

                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/Admin/dang-nhap?returnUrl={returnUrl}");
            },

            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = JsonContentType;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(
                        ApiResult.Fail("FORBIDDEN", "Đã đăng nhập nhưng không đủ quyền"),
                        new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("403 — Tài khoản không có quyền truy cập trang quản trị.");
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- Razor Pages quản trị (API.md mục 4.4 task BE) ----------
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/dang-nhap");
});

// ---------- CORS: FE chạy ở domain Vercel / Vite local ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Cần cho cookie admin khi FE gọi API bằng fetch({ credentials: 'include' })
    .AllowCredentials()));

// ---------- Rate limit: chặn spam form công khai & dò mật khẩu ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResult.Fail("RATE_LIMITED", "Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút."),
            cancellationToken: token);
    };

    options.AddPolicy(RateLimitPolicies.PublicWrite, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddOpenApi();

var app = builder.Build();

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

// Lưu ý: KHÔNG bật UseHttpsRedirection khi demo, vì FE gọi trực tiếp http://localhost:5252
// và redirect sẽ làm hỏng request cross-origin.
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Banova API — BE (ASP.NET Core)"));

// Health check cho host free (Render/Azure) và bước warm-up trước demo
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
{
    var databaseOk = await db.Database.CanConnectAsync(ct);

    return Results.Json(new
    {
        status = databaseOk ? "healthy" : "degraded",
        database = databaseOk,
        utc = DateTime.UtcNow
    }, statusCode: databaseOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

// ---------- Nạp dữ liệu mẫu khi khởi động ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();

// IP dùng làm khoá rate-limit; khi chạy sau reverse proxy đọc thêm X-Forwarded-For.
static string ClientKey(HttpContext context)
{
    var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(forwarded))
    {
        return forwarded.Split(',')[0].Trim();
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
