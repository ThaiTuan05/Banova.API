namespace Banova.Api.Lib;

/// <summary>
/// Tên policy rate-limit dùng chung giữa Program.cs và các Controller.
/// Mục đích: chặn spam form công khai và chặn dò mật khẩu admin (mục 4.6 quy tắc code).
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Form công khai: liên hệ, newsletter, quiz, đặt hàng.</summary>
    public const string PublicWrite = "public-write";

    /// <summary>Đăng nhập admin — chống brute-force.</summary>
    public const string Auth = "auth";
}
