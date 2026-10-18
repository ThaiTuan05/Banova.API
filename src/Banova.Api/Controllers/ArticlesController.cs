using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>API công khai — Góc khoa học / Blog (API.md mục 3.1, 3.2).</summary>
[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articles;

    public ArticlesController(IArticleService articles) => _articles = articles;

    /// <summary>GET /api/articles?page=&amp;pageSize=&amp;type=SCIENCE|BLOG&amp;q=</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ArticleQuery query, CancellationToken ct)
    {
        var (items, total, page, pageSize) = await _articles.ListAsync(query, ct);
        return Ok(ApiResult.OkPaged(items, page, pageSize, total));
    }

    /// <summary>GET /api/articles/{slug}</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
        => Ok(ApiResult.Ok(await _articles.GetBySlugAsync(slug, ct)));
}
