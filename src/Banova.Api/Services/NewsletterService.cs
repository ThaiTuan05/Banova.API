using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface INewsletterService
{
    Task<Guid> SubscribeAsync(NewsletterRequest request, CancellationToken ct = default);
}

/// <summary>Đăng ký nhận tin ở Footer. Email UNIQUE — trùng thì trả 409 CONFLICT.</summary>
public class NewsletterService : INewsletterService
{
    private readonly AppDbContext _db;

    public NewsletterService(AppDbContext db) => _db = db;

    public async Task<Guid> SubscribeAsync(NewsletterRequest request, CancellationToken ct = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw ApiException.Validation("Email không hợp lệ", new { field = "email" });
        }

        if (await _db.NewsletterSubscribers.AnyAsync(x => x.Email == email, ct))
        {
            throw ApiException.Conflict("Email này đã đăng ký nhận tin");
        }

        var entity = new NewsletterSubscriber { Email = email };
        _db.NewsletterSubscribers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
