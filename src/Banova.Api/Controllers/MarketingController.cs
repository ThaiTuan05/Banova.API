using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>API công khai — Liên hệ &amp; Đăng ký nhận tin (API.md mục 4.2, 4.3).</summary>
[ApiController]
public class MarketingController : ControllerBase
{
    private readonly IContactService _contact;
    private readonly INewsletterService _newsletter;

    public MarketingController(IContactService contact, INewsletterService newsletter)
    {
        _contact = contact;
        _newsletter = newsletter;
    }

    /// <summary>POST /api/contact → 201 { data: { id } }</summary>
    [HttpPost("api/contact")]
    public async Task<IActionResult> Contact([FromBody] ContactRequest request, CancellationToken ct)
    {
        var id = await _contact.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResult.Ok(new { id }));
    }

    /// <summary>POST /api/newsletter → 201 { data: { id, email } }, trùng email trả 409 CONFLICT</summary>
    [HttpPost("api/newsletter")]
    public async Task<IActionResult> Newsletter([FromBody] NewsletterRequest request, CancellationToken ct)
    {
        var id = await _newsletter.SubscribeAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResult.Ok(new { id, email = request.Email.Trim().ToLowerInvariant() }));
    }
}
