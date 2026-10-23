using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;

namespace Banova.Api.Tests;

public class ArticleServiceTests
{
    private static ArticleUpsertRequest ScienceRequest(string? references = "1. Birt DF, et al. (2013).") => new()
    {
        Title = "Tinh bột kháng là gì?",
        Slug = "tinh-bot-khang-la-gi",
        Excerpt = "Tóm tắt",
        Content = "<p>Nội dung</p>",
        Type = "SCIENCE",
        Status = "DRAFT",
        Author = "Banova",
        References = references
    };

    [Fact]
    public async Task CreateAsync_KhoaHocThieuNguon_TraValidation()
    {
        var db = TestDb.Create();

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => new ArticleService(db).CreateAsync(ScienceRequest(references: null)));

        Assert.Equal("VALIDATION", ex.Code);
        var details = Assert.IsType<Dictionary<string, string>>(ex.Details);
        Assert.Contains("references", details.Keys);
    }

    [Fact]
    public async Task CreateAsync_BlogKhongCanNguon_TaoThanhCong()
    {
        var db = TestDb.Create();

        var article = await new ArticleService(db).CreateAsync(new ArticleUpsertRequest
        {
            Title = "Bữa sáng nhanh",
            Slug = "bua-sang-nhanh",
            Content = "Nội dung",
            Type = "BLOG",
            Status = "DRAFT"
        });

        Assert.Equal("bua-sang-nhanh", article.Slug);
        Assert.Null(article.PublishedAt);
    }

    [Fact]
    public async Task CreateAsync_XuatBan_TuSetPublishedAt_VaHienCongKhai()
    {
        var db = TestDb.Create();
        var service = new ArticleService(db);

        var request = ScienceRequest();
        request.Status = "PUBLISHED";

        var article = await service.CreateAsync(request);

        Assert.NotNull(article.PublishedAt);

        var publicList = await service.ListAsync(new ArticleQuery());
        Assert.Equal(1, publicList.Total);
    }

    [Fact]
    public async Task UpdateAsync_ChuyenSangPublished_SetPublishedAt()
    {
        var db = TestDb.Create();
        var service = new ArticleService(db);

        var draft = await service.CreateAsync(ScienceRequest());
        Assert.Null(draft.PublishedAt);

        var published = await service.UpdateAsync(draft.Id, new ArticleUpsertRequest { Status = "PUBLISHED" });

        Assert.Equal(Entities.PublishState.PUBLISHED, published.Status);
        Assert.NotNull(published.PublishedAt);
    }

    [Fact]
    public async Task SoftDelete_AnKhoiDanhSachCongKhai()
    {
        var db = TestDb.Create();
        var service = new ArticleService(db);

        var request = ScienceRequest();
        request.Status = "PUBLISHED";
        var article = await service.CreateAsync(request);

        await service.SoftDeleteAsync(article.Id);

        var publicList = await service.ListAsync(new ArticleQuery());
        var adminList = await service.ListAdminAsync(new ArticleQuery());

        Assert.Equal(0, publicList.Total);
        Assert.Equal(0, adminList.Total);
    }

    [Fact]
    public async Task ListAdminAsync_ThayBanNhap()
    {
        var db = TestDb.Create();
        var service = new ArticleService(db);
        await service.CreateAsync(ScienceRequest());

        var publicList = await service.ListAsync(new ArticleQuery());
        var adminList = await service.ListAdminAsync(new ArticleQuery());

        Assert.Equal(0, publicList.Total);
        Assert.Equal(1, adminList.Total);
    }

    [Fact]
    public async Task CreateAsync_TrungSlug_TraConflict()
    {
        var db = TestDb.Create();
        var service = new ArticleService(db);
        await service.CreateAsync(ScienceRequest());

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(ScienceRequest()));

        Assert.Equal("CONFLICT", ex.Code);
    }
}
