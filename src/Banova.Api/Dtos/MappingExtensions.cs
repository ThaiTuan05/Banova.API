using Banova.Api.Dtos;
using Banova.Api.Entities;
using Banova.Api.Lib;

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

    public static OrderItemDto ToDto(this OrderItem i) => new()
    {
        Id = i.Id,
        ProductId = i.ProductId,
        ProductName = i.ProductName,
        UnitPrice = i.UnitPrice,
        Quantity = i.Quantity,
        SubTotal = i.SubTotal
    };

    public static OrderDto ToDto(this Order o) => new()
    {
        Id = o.Id,
        OrderCode = o.OrderCode,
        CustomerName = o.CustomerName,
        Phone = o.Phone,
        Email = o.Email,
        Address = o.Address,
        Note = o.Note,
        SubTotal = o.SubTotal,
        ShippingFee = o.ShippingFee,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        StatusLabel = Constants.OrderStatusLabel(o.Status),
        PaymentMethod = o.PaymentMethod,
        Items = o.Items.Select(i => i.ToDto()).ToList(),
        CreatedAt = o.CreatedAt
    };
}
