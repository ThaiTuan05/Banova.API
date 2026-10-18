using Banova.Api.Data;
using Banova.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default);
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct = default)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories.Select(c => c.ToDto()).ToList();
    }
}
