using Banova.Api.Common;
using Banova.Api.Data;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface IContactService
{
    Task<Guid> CreateAsync(ContactRequest request, CancellationToken ct = default);
}

public class ContactService : IContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(ContactRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = "Vui lòng nhập họ tên";
        if (string.IsNullOrWhiteSpace(request.Email)) errors["email"] = "Vui lòng nhập email";
        else if (!request.Email.Contains('@')) errors["email"] = "Email không hợp lệ";
        if (string.IsNullOrWhiteSpace(request.Phone)) errors["phone"] = "Vui lòng nhập số điện thoại";
        if (string.IsNullOrWhiteSpace(request.Message)) errors["message"] = "Vui lòng nhập nội dung";

        if (errors.Count > 0)
        {
            throw ApiException.Validation("Dữ liệu không hợp lệ", errors);
        }

        var entity = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Message = request.Message.Trim()
        };

        _db.ContactMessages.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public interface INewsletterService
{
    Task<Guid> SubscribeAsync(NewsletterRequest request, CancellationToken ct = default);
}

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
