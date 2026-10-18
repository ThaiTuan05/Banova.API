# Banova — Backend API (ASP.NET Core)

Backend cho website **Banova** (Tinh bột kháng RS2 — chuối xanh Gia Lai).
Kiến trúc: **FE React/Vite (deploy Vercel)** + **BE ASP.NET Core Web API (repo này)** — hai bên nói chuyện qua REST/JSON, không chung runtime.

> Nguồn chân lý duy nhất giữa FE và BE là **hợp đồng API** (`docs/API.md` trong repo FE).
> Mọi thay đổi endpoint/shape phải báo cả nhóm trước, không tự ý sửa.

---

## 1. Công nghệ

| Hạng mục | Lựa chọn |
|---|---|
| Runtime | .NET 10 (`net10.0`) |
| Framework | ASP.NET Core Web API (Controllers) |
| ORM | EF Core 10 + SQL Server |
| Database (dev) | SQL Server LocalDB `(localdb)\mssqllocaldb` |
| Xác thực | JWT Bearer + cookie `banova_token` (httpOnly) — *đang triển khai* |
| Hash mật khẩu | BCrypt.Net-Next |
| Validate | FluentValidation + kiểm tra nghiệp vụ trong service |
| Tài liệu API | OpenAPI + Scalar UI |

---

## 2. Chạy ở máy local

```bash
cd "E:\Project Exe\Banova.Api"

dotnet restore
dotnet build
dotnet run --project src\Banova.Api --urls http://localhost:5252
```

- Lần chạy đầu tiên sẽ **tự động tạo database, apply migration và nạp dữ liệu mẫu** (seeder idempotent — chạy lại không nhân đôi dữ liệu).
- Xem/tests API tại: **http://localhost:5252/scalar/v1**
- OpenAPI JSON: `http://localhost:5252/openapi/v1.json`

### Lệnh thường dùng

```bash
dotnet ef migrations add <TenMigration> --project src\Banova.Api\Banova.Api.csproj
dotnet ef database update --project src\Banova.Api\Banova.Api.csproj
dotnet ef migrations list --project src\Banova.Api\Banova.Api.csproj
```

### Đổi kết nối database

Sửa `src/Banova.Api/appsettings.json` → `ConnectionStrings:Default`.
Ví dụ dùng SQL Server đầy đủ:

```json
"Default": "Server=localhost;Database=BanovaDb;User Id=sa;Password=***;TrustServerCertificate=True"
```

---

## 3. Tài khoản & dữ liệu mẫu

| | Giá trị |
|---|---|
| Admin | `admin@banova.vn` / `Banova@123` |
| Danh mục | Bột (`bot`) · Bánh (`banh`) · Bữa sáng (`bua-sang`) · Hộp quà (`hop-qua`) |
| Sản phẩm | 8 SKU đúng giá theo tài liệu: 89.000 · 159.000 · 69.000 · 39.000 · 25.000 · 69.000 · 49.000 · 219.000 |
| Bài viết | 3 SCIENCE (có `references`) + 2 BLOG, tất cả `PUBLISHED` |

---

## 4. Chuẩn response (BẮT BUỘC)

**Thành công**
```jsonc
{ "data": { /* ... */ } }
```

**Danh sách có phân trang**
```jsonc
{ "data": [ /* ... */ ], "meta": { "page": 1, "pageSize": 12, "total": 8, "totalPages": 1 } }
```

**Lỗi**
```jsonc
{ "error": { "code": "VALIDATION", "message": "Dữ liệu không hợp lệ", "details": {} } }
```

Mã lỗi: `VALIDATION` 400 · `UNAUTHORIZED` 401 · `FORBIDDEN` 403 · `NOT_FOUND` 404 · `CONFLICT` 409 · `SERVER_ERROR` 500.

Quy ước khác: field **camelCase** · tiền **số nguyên VND** · thời gian **ISO 8601 UTC (có `Z`)** · enum trả **chuỗi** (`"PENDING"`, `"SCIENCE"`, `"RS2"`).

---

## 5. Trạng thái endpoint

### ✅ Đã xong — API công khai phục vụ FE

| Method | Endpoint | Ghi chú |
|---|---|---|
| GET | `/api/products` | `page`, `pageSize` (max 50), `category`, `q`, `sort` = `newest\|price_asc\|price_desc` |
| GET | `/api/products/{slug}` | 404 `NOT_FOUND` nếu không có |
| GET | `/api/categories` | Sắp theo `sortOrder` |
| GET | `/api/articles` | Chỉ `PUBLISHED`; lọc `type=SCIENCE\|BLOG`, `q` |
| GET | `/api/articles/{slug}` | 404 nếu chưa publish |
| POST | `/api/quiz/submit` | Nhận **cả 2** định dạng: `[{questionId,value}]` (API.md) và `{goal, morning_routine, rs_experience, sensitivity}` (FE hiện tại). Có lưu `QuizResult` |
| POST | `/api/contact` | 400 `VALIDATION` kèm `details` theo từng field |
| POST | `/api/newsletter` | 409 `CONFLICT` nếu email đã đăng ký |

### ⏳ Chưa làm

| Method | Endpoint | Ghi chú |
|---|---|---|
| POST | `/api/orders` | Server tự tính tiền từ DB (NFR-09) + snapshot giá (NFR-10) + trừ `stock` |
| GET | `/api/orders/{orderCode}` | Public theo mã đơn |
| GET | `/api/orders` 🔒 | Admin, lọc `status`, phân trang |
| PATCH | `/api/orders/{id}/status` 🔒 | Đổi trạng thái |
| POST | `/api/auth/login` | Trả JWT + set cookie `banova_token` |
| POST | `/api/auth/logout` · GET `/api/auth/me` 🔒 | |
| POST/PUT/DELETE | `/api/admin/products` · `/api/admin/products/{id}` 🔒 | DELETE = **soft delete** |
| POST/PUT/DELETE | `/api/admin/articles` · `/api/admin/articles/{id}` 🔒 | `PUBLISHED` → tự set `publishedAt` |
| GET | `/api/admin/stats` 🔒 | Dashboard |
| — | Trang Admin (Razor Pages) | Login · Dashboard · CRUD sản phẩm · CRUD bài viết · Danh sách đơn |

---

## 6. Ghép với FE

FE (Vite) đang deploy tại `https://banova-virid.vercel.app`, BE chạy ở domain riêng
→ **bắt buộc CORS**. Whitelist nằm ở `appsettings.json` → `Cors:AllowedOrigins`:

```
http://localhost:5173
http://127.0.0.1:5173
https://banova-virid.vercel.app
```

Phía FE cần thêm biến môi trường (Người 1 làm):

```env
VITE_API_BASE_URL=http://localhost:5252/api      # khi dev
# VITE_API_BASE_URL=https://<domain-be>/api       # khi demo
```

### ⚠️ Lệch tên field giữa FE (mock hiện tại) và API — FE cần map lại

| FE đang đọc | API trả về |
|---|---|
| `image` | `imageUrl` |
| `rsBadge` (chuỗi "5.8g RS/khẩu phần") | `rsContent` (số) + `rsType` (`"RS2"`) |
| `formattedPrice` (chuỗi "89.000₫") | `price` (số 89000) — FE tự format |
| `unitInfo` ("Hũ 250 gram") | `unit` + `servingSize` |
| `category` (chuỗi) | `category` (object `{ id, name, slug }`) |
| `id: 'prod-01'` (mock) | `id` (GUID thật) — **giỏ hàng & đơn hàng phải dùng id này** |
| — | `slug` (dùng cho URL chi tiết sản phẩm / bài viết) |

Khuyến nghị: viết lớp adapter `src/services/api.js` ở phía FE để map, thay vì sửa từng component.

---

## 7. Kiến trúc mã nguồn

```
src/Banova.Api/
├─ Entities/     # 9 bảng + 6 enum
├─ Data/         # AppDbContext, DbSeeder
├─ Dtos/         # DTO + query params + mapping
├─ Services/     # ★ NGHIỆP VỤ — CHỈ tầng này được chạm DbContext
├─ Controllers/  # Mỏng: nhận request -> gọi service -> trả ApiResult
├─ Common/       # ApiResult, ApiException, ExceptionMiddleware
└─ Lib/          # Constants + nhãn tiếng Việt
```

**Luật vàng:** Controller/Page **không bao giờ** import `AppDbContext` hay viết LINQ truy vấn — chỉ gọi Service.

---

## 8. Việc còn lại (theo thứ tự ưu tiên)

1. Auth: `POST /api/auth/login` + `JwtTokenService` + cookie `banova_token`
2. Orders: `POST /api/orders` (transaction, tính tiền server-side, `orderCode` `BNV-yyyyMMdd-XXX`)
3. Admin CRUD sản phẩm / bài viết
4. Admin danh sách đơn + `PATCH` trạng thái + `/api/admin/stats`
5. Trang Admin (Razor Pages)
6. Rà soát nội dung: **cấm** các từ "chữa", "điều trị", "thay thuốc", "đặc trị"; bài SCIENCE phải có `references`
7. Deploy (Azure App Service F1 / MonsterASP.NET — **Vercel không chạy được .NET**)

---

## 9. Quy tắc nội dung (bắt buộc — liên quan pháp lý)

- **CẤM** dùng các từ: "chữa", "điều trị", "thay thuốc", "đặc trị".
- Mọi con số (GI, %RS, thống kê) phải có nguồn; bài SCIENCE bắt buộc có mục nguồn tham khảo.
- Disclaimer y tế phải xuất hiện ở footer, chi tiết sản phẩm và cuối bài khoa học.
