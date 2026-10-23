using Banova.Api.Common;
using Banova.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banova.Api.Controllers;

/// <summary>Thống kê dashboard admin (API.md mục 9.3).</summary>
[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = "ADMIN")]
public class AdminStatsController : ControllerBase
{
    private readonly IStatsService _stats;

    public AdminStatsController(IStatsService stats) => _stats = stats;

    /// <summary>GET /api/admin/stats → 200 { data: AdminStats }</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(ApiResult.Ok(await _stats.GetAsync(ct)));
}
