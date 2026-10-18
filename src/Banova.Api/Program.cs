using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "BanovaFe";

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

// ---------- Database ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---------- Nghiệp vụ (DI) ----------
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

// ---------- CORS: FE chạy ở domain Vercel / Vite local ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();

// Lưu ý: KHÔNG bật UseHttpsRedirection khi demo, vì FE gọi trực tiếp http://localhost:5252
// và redirect sẽ làm hỏng request cross-origin.
app.UseCors(CorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Banova API — BE (ASP.NET Core)"));

// ---------- Nạp dữ liệu mẫu khi khởi động ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
