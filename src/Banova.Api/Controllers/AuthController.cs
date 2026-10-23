using System.Security.Claims;
using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Lib;
using Banova.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Banova.Api.Controllers;

/// <summary>
/// API xác thực admin (API.md mục 6). Token JWT được set vào cookie httpOnly
/// <c>banova_token</c> để vừa dùng cho API (header Bearer) vừa dùng cho trang Razor.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>POST /api/auth/login → 200 { data: AdminUser } + set cookie. Sai → 401.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var (user, token, expiresAt) = await _auth.LoginAsync(request, ct);

        AuthCookie.Append(Response, token, expiresAt, Request.IsHttps);

        return Ok(ApiResult.Ok(user));
    }

    /// <summary>POST /api/auth/logout → 200 { data: { success: true } }, xoá cookie.</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        AuthCookie.Delete(Response);
        return Ok(ApiResult.Ok(new { success = true }));
    }

    /// <summary>GET /api/auth/me → 200 { data: AdminUser }, chưa đăng nhập → 401.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = CurrentUserId();
        return Ok(ApiResult.Ok(await _auth.GetCurrentAsync(userId, ct)));
    }

    private Guid CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var userId))
        {
            throw ApiException.Unauthorized("Token không hợp lệ");
        }

        return userId;
    }
}
