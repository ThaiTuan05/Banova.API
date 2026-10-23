using Banova.Api.Common;
using Banova.Api.Dtos;
using Banova.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Banova.Api.Pages.Admin;

/// <summary>
/// Quản lý bài viết (/Admin/bai-viet): danh sách + soạn/sửa + xuất bản + xoá mềm.
/// Bài SCIENCE bắt buộc có nguồn trích dẫn (NFR-03/NFR-09).
/// </summary>
public class ArticlesModel : PageModel
{
    private readonly IArticleService _articles;

    public ArticlesModel(IArticleService articles) => _articles = articles;

    public IReadOnlyList<ArticleDto> Articles { get; private set; } = Array.Empty<ArticleDto>();

    [BindProperty]
    public ArticleUpsertRequest Input { get; set; } = new();

    [BindProperty]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public async Task OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is null)
        {
            Input = new ArticleUpsertRequest
            {
                Type = "SCIENCE",
                Status = "DRAFT",
                Author = "Banova"
            };
        }
        else
        {
            var article = await _articles.GetByIdAsync(id.Value, ct);
            Id = id;

            Input = new ArticleUpsertRequest
            {
                Title = article.Title,
                Slug = article.Slug,
                Excerpt = article.Excerpt,
                Content = article.Content,
                Thumbnail = article.Thumbnail,
                Type = article.Type.ToString(),
                References = article.References,
                Author = article.Author,
                Status = article.Status.ToString()
            };
        }

        await LoadListAsync(ct);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        try
        {
            if (Id is not null)
            {
                await _articles.UpdateAsync(Id.Value, Input, ct);
                TempData["Message"] = "Đã cập nhật bài viết.";
            }
            else
            {
                await _articles.CreateAsync(Input, ct);
                TempData["Message"] = "Đã tạo bài viết mới.";
            }

            return RedirectToPage("/Admin/bai-viet");
        }
        catch (ApiException ex)
        {
            TempData["Error"] = BuildErrorMessage(ex);
            await LoadListAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await _articles.SoftDeleteAsync(id, ct);
            TempData["Message"] = "Đã xoá mềm bài viết.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage("/Admin/bai-viet");
    }

    private async Task LoadListAsync(CancellationToken ct)
    {
        var (items, _, _, _) = await _articles.ListAdminAsync(
            new ArticleQuery { Page = 1, PageSize = 50, Q = Q }, ct);

        Articles = items;
    }

    private static string BuildErrorMessage(ApiException ex)
    {
        if (ex.Details is Dictionary<string, string> fieldErrors && fieldErrors.Count > 0)
        {
            return $"{ex.Message}: {string.Join(" · ", fieldErrors.Select(kv => $"{kv.Key} — {kv.Value}"))}";
        }

        return ex.Message;
    }
}
