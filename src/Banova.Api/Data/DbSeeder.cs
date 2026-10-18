using Banova.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banova.Api.Data;

/// <summary>
/// Dữ liệu mẫu bắt buộc (mục 6.1 quy tắc code): 1 admin, 4 danh mục, 8 SKU, 5 bài viết.
/// Chạy lại nhiều lần vẫn an toàn — chỉ nạp khi DB còn trống.
/// </summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@banova.vn";
    public const string AdminPassword = "Banova@123";

    // Ảnh dùng tạm chính URL mà FE đang hiển thị để giao diện không vỡ khi ghép API.
    // Khi có ảnh thật của Banova thì thay bằng file trong wwwroot/images.
    private const string Img250g = "https://lh3.googleusercontent.com/aida-public/AB6AXuAwGVTi1gB1XvNbSveYj_baHXsEbIOocmjJkIBVAFObemwtPxprwGnjI0ql6zljWfiOW3YYwywLmneJKhVc9vYG_lxGF9KNdEEGJ8G2KuFRvFhVgSJxlpv6969fl-nRcjp4ZHnopEgzoWmcQQE_zDbEEv3OlU5yB6kCjdpoiU_6pJQMEpqmtIMT4qSsrCc8VIroRwVHf3QsPqE9WTOpF9lF9qch7DDX04ErGUle-6PjvanY1rdFfVcB";
    private const string Img500g = "https://lh3.googleusercontent.com/aida-public/AB6AXuBsLeH_7AMePXKa6uSLLev1jV9T_4loFtxndS9sHFFEDuzsWanDShb-RtfeYBBL0zNt91LmuBghISC7aO3cGDLyBWYhOx3T6cRUIcfW_8fpWsSdjqTxgKnjzRFNYiPP2mBjeqhm6r2HcYCFaqB6uFKI91eTBYOSW5KAHvC9P0qheigZW7iKLeO7IVUBZkD7h4rNXXE97F9cydjSn45kh-cXkUShYz4SMHSL33o-1K6-Rv7qs3MgDBQR";
    private const string Img10Goi = "https://lh3.googleusercontent.com/aida-public/AB6AXuCrMX4CUMxJBtX2NtIrLRHdUr4SobsQ8mCYCaHnfjZ8UanKK5OrshjuWsVRnjxMaxBUr-IJ3wrakzhUf9l5fV0y1LpOKRrEjlj09ZfLJIbFhup7nv8WMe0utTDOcp9eCdANmvN6FI_DHoZhMNsBioZg6JUzhtVYEi1L8bZNb-Wjc_1EHd8lDE8ByONQjmP50pXPh7bKL5atginuEXHXsmfcW-2II5LQyzdzjrMO62nTQ7A8oOeCF0MZ";
    private const string ImgBanhQuy = "https://lh3.googleusercontent.com/aida-public/AB6AXuB-pZPCm-H90xKbf1tg-YyqR5gWMMx6nkcCMip1SCW-NN-BQp7_ktEZZ2w6fFQoihG-HzH_FfZdFuFB52WKp5oHRdqecutgIXTiByjVQvWvGFIyWLFq4h3XW4HXAlzb7DNG67Ss6-6U5QeHOHwx0npK5VbWIHO-03EGrt6sE6fhLzyW1YuEmzbvBvhCmYokPrRXhlyhAZOn3IU4-MwPk3VLeDMcJe0GJMFE2Cp9Bd-OIDmp0X5vRtHZ";
    private const string ImgHopQua = "https://lh3.googleusercontent.com/aida-public/AB6AXuAhHU4TxO1C2iLW-yIEMTt6Ki-s4As6aE-_qhpuanORYt4-o_Rei7EkhCWDcRwY7sj7JKKDj82yo8JDs4bI1tMPyGEQ_bFRaI3jbe0dmmfOTGQ9rCqCRtzOzFM48vxJLkm5_5VLgVjO67gnsGHQ5GxmPco9pD9W0bKPuMIMJYWsMLb6Roz3N997bMJmUbDPDHTFnsTSytXMu45ES-ZMxjyp-t6p5T0HM0v4vnINkFW6c5iIvt2FRxBz";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Categories.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // ---------- 1. Tài khoản admin ----------
        db.Users.Add(new User
        {
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
            FullName = "Quản trị Banova",
            Role = Role.ADMIN,
            CreatedAt = now,
            UpdatedAt = now
        });

        // ---------- 2. Danh mục ----------
        var catBot = new Category { Name = "Bột", Slug = "bot", SortOrder = 1, Description = "Bột tinh bột kháng nguyên chất từ chuối xanh Gia Lai", CreatedAt = now, UpdatedAt = now };
        var catBanh = new Category { Name = "Bánh", Slug = "banh", SortOrder = 2, Description = "Bánh dinh dưỡng ít đường cho bữa phụ lành mạnh", CreatedAt = now, UpdatedAt = now };
        var catBuaSang = new Category { Name = "Bữa sáng", Slug = "bua-sang", SortOrder = 3, Description = "Bộ bữa sáng tiện lợi giúp no lâu và ổn định đường huyết", CreatedAt = now, UpdatedAt = now };
        var catHopQua = new Category { Name = "Hộp quà", Slug = "hop-qua", SortOrder = 4, Description = "Hộp quà sức khỏe biếu tặng từ nông sản Quy Nhơn", CreatedAt = now, UpdatedAt = now };

        db.Categories.AddRange(catBot, catBanh, catBuaSang, catHopQua);

        // ---------- 3. Sản phẩm (8 SKU) ----------
        db.Products.AddRange(
            NewProduct("Bột tinh bột kháng Banova 250g", "bot-tinh-bot-khang-banova-250g", catBot.Id, 89000, "hũ", "15g", 5.8m, 35, 3.2m,
                "Hũ thuỷ tinh 250g bảo quản tối ưu, pha nước ấm hoặc sinh tố.",
                "Bột chuối xanh Gia Lai sấy lạnh, hàm lượng RS2 tự nhiên cao.",
                "Pha 1–2 muỗng (15g) với 150ml nước ấm hoặc sinh tố, dùng trước bữa chính 15–30 phút. Bảo quản nơi khô ráo, tránh ánh nắng.",
                Img250g, 120, now),

            NewProduct("Bột tinh bột kháng Banova 500g", "bot-tinh-bot-khang-banova-500g", catBot.Id, 159000, "hũ", "15g", 5.8m, 35, 3.2m,
                "Hũ 500g tiết kiệm cho gia đình dùng hàng ngày.",
                "Bột chuối xanh Gia Lai sấy lạnh, giữ trọn cấu trúc tinh thể RS2.",
                "Pha 1–2 muỗng (15g) với 150ml nước ấm hoặc sinh tố, dùng trước bữa chính 15–30 phút.",
                Img500g, 80, now),

            NewProduct("Bột Banova hộp 10 gói 15g tiện lợi", "bot-banova-hop-10-goi-15g", catBot.Id, 69000, "hộp", "15g", 5.8m, 35, 3.2m,
                "Định lượng sẵn 15g/gói, thuận tiện mang theo công tác hoặc dùng tại văn phòng.",
                "Bột chuối xanh Gia Lai chia liều sẵn, hoà tan nhanh.",
                "Xé 1 gói pha với 150ml nước ấm, khuấy đều và dùng ngay.",
                Img10Goi, 200, now),

            NewProduct("Bánh mì nguyên cám Banova", "banh-mi-nguyen-cam-banova", catBanh.Id, 39000, "ổ", "60g", 4.2m, 49, 2.8m,
                "Ổ bánh mì nguyên cám bổ sung bột chuối xanh, phù hợp bữa sáng.",
                "Bột mì nguyên cám, bột chuối xanh Gia Lai, men tự nhiên, muối biển.",
                "Dùng trực tiếp hoặc nướng nhẹ 2–3 phút trước khi dùng. Bảo quản ngăn mát được 3 ngày.",
                ImgBanhQuy, 60, now),

            NewProduct("Muffin chuối yến mạch", "muffin-chuoi-yen-mach", catBanh.Id, 25000, "cái", "50g", 3.5m, 45, 2.5m,
                "Bánh muffin chuối yến mạch ít đường cho bữa phụ gọn nhẹ.",
                "Yến mạch cán dẹt, chuối chín, trứng, bột chuối xanh, dầu dừa.",
                "Dùng ngay hoặc hâm nóng 30 giây. Bảo quản ngăn mát 2 ngày.",
                ImgBanhQuy, 90, now),

            NewProduct("Cookie hạt ít đường 200g", "cookie-hat-it-duong-200g", catBanh.Id, 69000, "hộp", "30g", 2.8m, 42, 3.0m,
                "Hộp cookie hạt ít đường, giòn tự nhiên, giàu chất xơ thô.",
                "Yến mạch, hạt điều, hạnh nhân, bột chuối xanh, mật hoa dừa.",
                "Dùng trực tiếp như bữa phụ. Bảo quản kín nơi khô ráo.",
                ImgBanhQuy, 70, now),

            NewProduct("Hộp bữa sáng dinh dưỡng Banova", "hop-bua-sang-dinh-duong-banova", catBuaSang.Id, 49000, "hộp", "1 hộp", 5.8m, 38, 4.1m,
                "Combo bữa sáng gọn nhẹ: bột RS2, hạt dinh dưỡng và trái cây sấy.",
                "Bột chuối xanh, hạt chia, yến mạch, chuối sấy, mật ong.",
                "Trộn với 150ml nước ấm hoặc sữa, để 2 phút cho nở rồi dùng.",
                Img10Goi, 40, now),

            NewProduct("Hộp quà Quy Nhơn Healthy", "hop-qua-quy-nhon-healthy", catHopQua.Id, 219000, "hộp", "1 bộ", 5.8m, 35, 5.0m,
                "Bộ quà biếu gồm bột chuối xanh, bánh ăn kiêng và ly shaker.",
                "Bột chuối xanh Gia Lai, bánh nguyên cám, yến mạch, ly shaker.",
                "Dùng theo hướng dẫn trên từng sản phẩm trong bộ quà.",
                ImgHopQua, 25, now)
        );

        // ---------- 4. Bài viết (3 Khoa học + 2 Blog) ----------
        db.Articles.AddRange(
            NewArticle(
                "Tinh bột kháng là gì và vì sao tốt cho đường huyết?",
                "tinh-bot-khang-la-gi-va-vi-sao-tot-cho-duong-huyet",
                "Tổng quan cơ chế tinh bột kháng RS2 kháng lại enzyme amylase, lên men tạo SCFA tại đại tràng và tác động tới đáp ứng đường huyết sau ăn.",
                ArticleType.SCIENCE,
                """
                <h2>Tinh bột kháng (Resistant Starch) là gì?</h2>
                <p>Tinh bột kháng là phần tinh bột không bị tiêu hoá ở ruột non. Thay vì bị phân giải thành glucose, tinh bột kháng đi thẳng xuống đại tràng và trở thành nguồn cơ chất cho hệ vi sinh đường ruột.</p>
                <h2>Cơ chế tác động</h2>
                <p>Ở nhóm RS2 (tinh bột kháng có trong chuối xanh), các phân tử amylose xếp thành cấu trúc tinh thể dày đặc khiến enzyme amylase khó tiếp cận. Nhờ vậy tốc độ giải phóng glucose sau ăn diễn ra chậm hơn.</p>
                <p>Tại đại tràng, vi khuẩn lên men tinh bột kháng và tạo ra các acid béo chuỗi ngắn (SCFA) như butyrate, propionate và acetate. Butyrate là nguồn năng lượng chính cho tế bào niêm mạc đại tràng.</p>
                <h2>Vì sao được quan tâm trong kiểm soát đường huyết?</h2>
                <p>Nhiều nghiên cứu ghi nhận tinh bột kháng có thể góp phần làm phẳng đường cong đường huyết sau ăn và kéo dài cảm giác no. Đây là lý do RS được nghiên cứu trong các chế độ ăn hỗ trợ kiểm soát cân nặng và đường huyết.</p>
                <h2>Lưu ý khi bắt đầu sử dụng</h2>
                <p>Nên tăng liều từ từ trong 1–2 tuần đầu để đường ruột thích nghi. Uống đủ nước trong ngày và tham khảo ý kiến chuyên gia dinh dưỡng nếu bạn có bệnh lý tiêu hoá mạn tính.</p>
                """,
                "1. Asp NG, Björck I. Resistant starch. Trends in Food Science &amp; Technology.\n2. Birt DF và cộng sự. Resistant Starch: Promise for Improving Human Health. Advances in Nutrition, 2013.\n3. Sajilata MG và cộng sự. Resistant Starch — A Review. Comprehensive Reviews in Food Science and Food Safety, 2006.\n4. Bộ Y tế Việt Nam. Hướng dẫn chế độ ăn cho người bệnh đái tháo đường.",
                "TS. Dược học Lê Minh Tuấn",
                Img250g,
                new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc), now),

            NewArticle(
                "Chuối xanh Gia Lai — nguyên liệu vàng cho tinh bột kháng",
                "chuoi-xanh-gia-lai-nguyen-lieu-vang-cho-tinh-bot-khang",
                "Vì sao đất bazan Gia Lai lại cho ra chuối xanh có hàm lượng RS2 cao và ổn định hơn các vùng nguyên liệu khác.",
                ArticleType.SCIENCE,
                """
                <h2>Vùng nguyên liệu bazan Gia Lai</h2>
                <p>Đất đỏ bazan Gia Lai giàu khoáng chất và có biên độ nhiệt ngày đêm rõ rệt — điều kiện thuận lợi để chuối tích tụ tinh bột trong giai đoạn quả còn xanh.</p>
                <h2>Vì sao phải thu hoạch khi còn xanh?</h2>
                <p>Trong quá trình chín, enzyme amylase chuyển hoá tinh bột kháng thành đường. Vì vậy thời điểm thu hoạch là yếu tố quyết định để giữ được hàm lượng RS2 cao nhất.</p>
                <h2>Sấy lạnh để bảo toàn cấu trúc RS2</h2>
                <p>Nhiệt độ thấp giúp bảo toàn cấu trúc tinh thể của hạt tinh bột, tránh hiện tượng hồ hoá làm mất tính kháng. Đây là điểm khác biệt giữa bột chuối xanh sấy lạnh và bột chuối sấy nhiệt cao.</p>
                """,
                "1. Sajilata MG và cộng sự. Resistant Starch — A Review, 2006.\n2. Viện Cây ăn quả miền Nam. Tài liệu về vùng trồng chuối Tây Nguyên.\n3. Bộ Y tế Việt Nam. Hướng dẫn chế độ ăn cho người bệnh đái tháo đường.",
                "ThS. Trần Quốc Bảo",
                Img500g,
                new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc), now),

            NewArticle(
                "Ăn sáng thế nào để no đến trưa mà không tăng đường huyết?",
                "an-sang-the-nao-de-no-den-trua-ma-khong-tang-duong-huyet",
                "Nguyên tắc phối hợp đạm, chất xơ và tinh bột kháng để bữa sáng giữ cảm giác no tới trưa mà không tạo đỉnh đường huyết.",
                ArticleType.SCIENCE,
                """
                <h2>Vấn đề của bữa sáng nhiều tinh bột nhanh</h2>
                <p>Bữa sáng nhiều bánh mì trắng, bún hoặc cháo trắng thường tạo ra đỉnh glucose sớm. Đỉnh này kéo theo phản ứng insulin mạnh, dẫn tới hạ đường huyết tương đối và cảm giác đói trở lại trước bữa trưa.</p>
                <h2>Bộ ba giữ no</h2>
                <p>Một bữa sáng giữ no tốt thường phối hợp: chất đạm (trứng, sữa, đậu), chất béo tốt (hạt, dầu ô liu) và chất xơ — trong đó tinh bột kháng đóng vai trò cơ chất lên men tại đại tràng, gián tiếp kéo dài cảm giác no.</p>
                <h2>Gợi ý áp dụng</h2>
                <p>Bắt đầu ngày mới bằng một ly nước ấm pha bột chuối xanh, sau đó dùng bữa sáng có đạm và hạt. Cách này giúp tổng lượng đường nạp vào được giải phóng chậm hơn.</p>
                """,
                "1. Birt DF và cộng sự. Resistant Starch: Promise for Improving Human Health, 2013.\n2. Bộ Y tế Việt Nam. Khuyến nghị dinh dưỡng cho người trưởng thành.\n3. IDF Diabetes Atlas, ấn bản mới nhất.",
                "ThS. BS. Nguyễn Hoàng Lan",
                Img10Goi,
                new DateTime(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc), now),

            NewArticle(
                "3 công thức smoothie tinh bột kháng cho người bận rộn",
                "3-cong-thuc-smoothie-tinh-bot-khang-cho-nguoi-ban-ron",
                "Ba công thức smoothie dưới 5 phút, phù hợp buổi sáng văn phòng và sau tập.",
                ArticleType.BLOG,
                """
                <h2>1. Smoothie chuối xanh — yến mạch</h2>
                <p>1 muỗng bột chuối xanh, 3 muỗng yến mạch cán dẹt, 150ml sữa tươi hoặc sữa hạt, xay cùng đá trong 30 giây.</p>
                <h2>2. Smoothie xoài — hạt chia</h2>
                <p>1 muỗng bột chuối xanh, 1/2 quả xoài chín, 1 muỗng hạt chia ngâm, 150ml nước dừa.</p>
                <h2>3. Smoothie berry — sữa chua</h2>
                <p>1 muỗng bột chuối xanh, 1 hộp sữa chua không đường, một nắm dâu tây. Xay nhuyễn và thêm hạt lanh lên trên.</p>
                <h2>Mẹo nhỏ</h2>
                <p>Pha bột với nước ấm trước rồi mới xay cùng trái cây để bột tan đều, không bị vón.</p>
                """,
                null,
                "Banova Kitchen",
                ImgBanhQuy,
                new DateTime(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc), now),

            NewArticle(
                "Chuyện từ vườn chuối Gia Lai: hành trình giữ trọn RS2",
                "chuyen-tu-vuon-chuoi-gia-lai-hanh-trinh-giu-tron-rs2",
                "Theo chân những buồng chuối xanh từ nương bazan về tới dây chuyền sấy lạnh của Banova.",
                ArticleType.BLOG,
                """
                <h2>Buổi sớm ở nương chuối</h2>
                <p>Chuối được cắt khi vừa đủ già nhưng vỏ còn xanh đậm. Đây là lúc hàm lượng tinh bột kháng RS2 cao nhất trong quả.</p>
                <h2>Giới hạn thời gian sau thu hoạch</h2>
                <p>Sau khi cắt, chuối tiếp tục chín và tinh bột dần chuyển thành đường. Vì vậy thời gian vận chuyển được rút ngắn tối đa trước khi vào dây chuyền chế biến.</p>
                <h2>Sấy lạnh và kiểm nghiệm</h2>
                <p>Mỗi lô nguyên liệu được lấy mẫu kiểm nghiệm hàm lượng RS trước khi đóng hũ, để sai lệch giữa các lô nằm trong ngưỡng cho phép.</p>
                """,
                null,
                "Banova Farm Team",
                ImgHopQua,
                new DateTime(2026, 10, 16, 8, 0, 0, DateTimeKind.Utc), now)
        );

        await db.SaveChangesAsync(ct);
    }

    private static Product NewProduct(
        string name, string slug, Guid categoryId, int price, string unit, string servingSize,
        decimal rsContent, int gi, decimal fiber, string shortDescription, string ingredients,
        string usage, string imageUrl, int stock, DateTime now)
        => new()
        {
            Name = name,
            Slug = slug,
            CategoryId = categoryId,
            Price = price,
            Unit = unit,
            ServingSize = servingSize,
            RsContent = rsContent,
            RsType = RsType.RS2,
            Gi = gi,
            Fiber = fiber,
            ShortDescription = shortDescription,
            Description = $"{name} — {shortDescription} Sản phẩm bổ sung nguồn tinh bột kháng RS2 từ chuối xanh Gia Lai, hỗ trợ hệ vi sinh đường ruột và duy trì cảm giác no tự nhiên.",
            Ingredients = ingredients,
            Usage = usage,
            ImageUrl = imageUrl,
            Stock = stock,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static Article NewArticle(
        string title, string slug, string excerpt, ArticleType type, string content,
        string? references, string? author, string thumbnail, DateTime publishedAt, DateTime now)
        => new()
        {
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            Content = content,
            Type = type,
            References = references,
            Author = author,
            Thumbnail = thumbnail,
            Status = PublishState.PUBLISHED,
            PublishedAt = publishedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
}
