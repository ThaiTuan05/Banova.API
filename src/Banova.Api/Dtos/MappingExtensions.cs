using Banova.Api.Dtos;
using Banova.Api.Entities;

namespace Banova.Api.Dtos;

public static class MappingExtensions
{
    public static CategoryDto ToDto(this Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        SortOrder = c.SortOrder
    };

    public static ProductDto ToDto(this Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        ShortDescription = p.ShortDescription,
        Description = p.Description,
        Category = p.Category is null ? new CategoryDto() : p.Category.ToDto(),
        Price = p.Price,
        Unit = p.Unit,
        ServingSize = p.ServingSize,
        RsContent = p.RsContent,
        RsType = p.RsType,
        Gi = p.Gi,
        Fiber = p.Fiber,
        Ingredients = p.Ingredients,
        Usage = p.Usage,
        ImageUrl = p.ImageUrl,
        Stock = p.Stock,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };

    public static ArticleDto ToDto(this Article a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Slug = a.Slug,
        Excerpt = a.Excerpt,
        Content = a.Content,
        Thumbnail = a.Thumbnail,
        Type = a.Type,
        References = a.References,
        Author = a.Author,
        Status = a.Status,
        PublishedAt = a.PublishedAt,
        CreatedAt = a.CreatedAt
    };
}
