using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Lib;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Banova.Api.Controllers;

/// <summary>API công khai — Quiz tư vấn (API.md mục 4.1).</summary>
[ApiController]
[Route("api/quiz")]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quiz;

    public QuizController(IQuizService quiz) => _quiz = quiz;

    /// <summary>POST /api/quiz/submit — body { answers: [...] } hoặc { answers: { goal: "..." } }</summary>
    [HttpPost("submit")]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> Submit([FromBody] QuizSubmitRequest request, CancellationToken ct)
        => Ok(ApiResult.Ok(await _quiz.SubmitAsync(request, ct)));
}
