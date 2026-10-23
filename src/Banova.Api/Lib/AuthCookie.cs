namespace Banova.Api.Lib;

/// <summary>
/// Ghi/xoá cookie JWT <c>banova_token</c> (httpOnly, sameSite=lax) — API.md mục 6.1.
/// Dùng chung cho AuthController (gọi API) và trang đăng nhập Razor (cùng một cookie).
/// </summary>
public static class AuthCookie
{
    public static void Append(HttpResponse response, string token, DateTime expiresAtUtc, bool secure)
    {
        response.Cookies.Append(Constants.AuthCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = new DateTimeOffset(expiresAtUtc)
        });
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Constants.AuthCookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
