using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Lib;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Banova.Api.Pages.Admin;

/// <summary>
/// Trang đăng nhập admin (/Admin/dang-nhap) — cùng cookie <c>banova_token</c> với API.
/// </summary>
public class LoginModel : PageModel
{
    private readonly IAuthService _auth;

    public LoginModel(IAuthService auth) => _auth = auth;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Admin/Index");
        }

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct = default)
    {
        try
        {
            var (_, token, expiresAt) = await _auth.LoginAsync(
                new LoginRequest { Email = Email, Password = Password }, ct);

            AuthCookie.Append(Response, token, expiresAt, Request.IsHttps);

            var target = !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
                ? ReturnUrl!
                : "/Admin";

            return LocalRedirect(target);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public IActionResult OnPostLogout()
    {
        AuthCookie.Delete(Response);
        return RedirectToPage("/Admin/dang-nhap");
    }
}
