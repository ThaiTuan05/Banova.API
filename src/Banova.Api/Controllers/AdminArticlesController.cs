using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>API quản trị bài viết (API.md mục 8) — toàn bộ yêu cầu quyền ADMIN.</summary>
[ApiController]
[Route("api/admin/articles")]
[Authorize(Roles = "ADMIN")]
public class AdminArticlesController : ControllerBase
{
    private readonly IArticleService _articles;

    public AdminArticlesController(IArticleService articles) => _articles = articles;

    /// <summary>GET /api/admin/articles — danh sách cho trang quản trị (gồm cả bản nháp).</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ArticleQuery query, CancellationToken ct)
    {
        var (items, total, page, pageSize) = await _articles.ListAdminAsync(query, ct);
        return Ok(ApiResult.OkPaged(items, page, pageSize, total));
    }

    /// <summary>POST /api/admin/articles → 201 { data: Article }; PUBLISHED thì tự set publishedAt.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ArticleUpsertRequest request, CancellationToken ct)
    {
        var article = await _articles.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResult.Ok(article));
    }

    /// <summary>PUT /api/admin/articles/{id} → 200 { data: Article }</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ArticleUpsertRequest request, CancellationToken ct)
        => Ok(ApiResult.Ok(await _articles.UpdateAsync(id, request, ct)));

    /// <summary>DELETE /api/admin/articles/{id} → xoá mềm → 200 { data: { success: true } }</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _articles.SoftDeleteAsync(id, ct);
        return Ok(ApiResult.Ok(new { success = true }));
    }
}
