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

/// <summary>Tiếp nhận tin nhắn từ trang Liên hệ.</summary>
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
