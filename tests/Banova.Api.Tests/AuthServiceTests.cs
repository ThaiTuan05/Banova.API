using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Entities;
using Banova.Api.Services;
using Microsoft.Extensions.Options;

namespace Banova.Api.Tests;

public class AuthServiceTests
{
    private const string Password = "Banova@123";

    private static AuthService CreateService(Data.AppDbContext db)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "Banova",
            Audience = "BanovaAdmin",
            Secret = new string('s', 48),
            ExpireMinutes = 60
        });

        return new AuthService(db, new JwtTokenService(options));
    }

    private static async Task<Data.AppDbContext> CreateWithAdminAsync()
    {
        var db = TestDb.Create();
        db.Users.Add(new User
        {
            Email = "admin@banova.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            FullName = "Quản trị Banova",
            Role = Role.ADMIN
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task LoginAsync_DungMatKhau_TraTokenVaThongTin()
    {
        var db = await CreateWithAdminAsync();

        var (user, token, expiresAt) = await CreateService(db).LoginAsync(new LoginRequest
        {
            Email = "ADMIN@banova.vn ",
            Password = Password
        });

        Assert.Equal("admin@banova.vn", user.Email);
        Assert.Equal(Role.ADMIN, user.Role);
        Assert.False(string.IsNullOrWhiteSpace(user.FullName));
        Assert.Equal(3, token.Split('.').Length);
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_SaiMatKhau_TraUnauthorized()
    {
        var db = await CreateWithAdminAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateService(db).LoginAsync(new LoginRequest
        {
            Email = "admin@banova.vn",
            Password = "sai-mat-khau"
        }));

        Assert.Equal("UNAUTHORIZED", ex.Code);
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_EmailKhongTonTai_TraUnauthorized()
    {
        var db = await CreateWithAdminAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateService(db).LoginAsync(new LoginRequest
        {
            Email = "khong-ton-tai@banova.vn",
            Password = Password
        }));

        Assert.Equal("UNAUTHORIZED", ex.Code);
    }

    [Fact]
    public async Task LoginAsync_ThieuDuLieu_TraValidation()
    {
        var db = await CreateWithAdminAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateService(db).LoginAsync(new LoginRequest
        {
            Email = "",
            Password = ""
        }));

        Assert.Equal("VALIDATION", ex.Code);
    }

    [Fact]
    public async Task GetCurrentAsync_TraDungUser()
    {
        var db = await CreateWithAdminAsync();
        var admin = db.Users.Single();
        var service = CreateService(db);

        var user = await service.GetCurrentAsync(admin.Id);

        Assert.Equal(admin.Id, user.Id);
        Assert.Equal("admin@banova.vn", user.Email);
    }
}
