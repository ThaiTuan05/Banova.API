using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Banova.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQL Server không giữ DateTimeKind -> mọi DateTime đọc ra được gắn lại là UTC
        // để System.Text.Json serialize kèm hậu tố Z, khớp quy ước ISO 8601 UTC của API.md.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<Category>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
        });

        b.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.DeletedAt });
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(500);
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.ServingSize).HasMaxLength(50).IsRequired();
            e.Property(x => x.RsContent).HasPrecision(6, 2);
            e.Property(x => x.Fiber).HasPrecision(6, 2);
            e.Property(x => x.RsType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Ingredients).HasMaxLength(2000);
            e.Property(x => x.Usage).HasMaxLength(2000);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Article>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.Status, x.Type });
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(300).IsRequired();
            e.Property(x => x.Excerpt).HasMaxLength(1000);
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.Thumbnail).HasMaxLength(1000);
            e.Property(x => x.Author).HasMaxLength(200);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderCode).IsUnique();
            e.HasIndex(x => x.Status);
            e.Property(x => x.OrderCode).HasMaxLength(40).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Address).HasMaxLength(500).IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<OrderItem>(e =>
        {
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Không cho xoá sản phẩm đã từng nằm trong đơn -> bảo toàn lịch sử doanh thu (NFR-10)
            e.HasOne(x => x.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<QuizResult>(e =>
        {
            e.Property(x => x.AnswersJson).IsRequired();
            e.HasOne(x => x.RecommendedProduct)
                .WithMany()
                .HasForeignKey(x => x.RecommendedProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ContactMessage>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        });

        b.Entity<NewsletterSubscriber>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
        });
    }
}

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime())
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
