# Banova — Backend API (ASP.NET Core)

Backend cho website **Banova** (Tinh bột kháng RS2 — chuối xanh Gia Lai).
Kiến trúc: **FE React/Vite (deploy Vercel)** + **BE ASP.NET Core Web API (repo này)** — hai bên nói chuyện qua REST/JSON, không chung runtime.

> Nguồn chân lý duy nhất giữa FE và BE là **hợp đồng API** (`API.md` trong repo FE).
> Mọi thay đổi endpoint/shape phải báo cả nhóm trước, không tự ý sửa.

---

## 1. Công nghệ

| Hạng mục | Lựa chọn |
|---|---|
| Runtime | .NET 10 (`net10.0`) |
| Framework | ASP.NET Core Web API (Controllers) + Razor Pages cho trang admin |
| ORM | EF Core 10 + SQL Server |
| Database (dev) | SQL Server LocalDB `(localdb)\mssqllocaldb` |
| Xác thực | **JWT Bearer** + cookie `banova_token` (httpOnly, sameSite=lax) — áp dụng cho cả API và trang admin |
| Hash mật khẩu | BCrypt.Net-Next |
| Validate | Kiểm tra nghiệp vụ trong service, trả `details` theo từng field |
| Rate limit | Fixed window: 30 request/phút cho form công khai, 10 request/phút cho đăng nhập |
| Tài liệu API | OpenAPI + Scalar UI |
| Test | xUnit (`tests/Banova.Api.Tests`) + 2 script smoke test |

---

## 2. Chạy ở máy local

```bash
cd "D:\EXE_101\Banova.API"

dotnet restore
dotnet build
dotnet run --project src\Banova.Api --urls http://localhost:5252
```

- Lần chạy đầu tiên sẽ **tự động tạo database, apply migration và nạp dữ liệu mẫu** (seeder idempotent — chạy lại không nhân đôi dữ liệu).
- Xem/test API: **http://localhost:5252/scalar/v1**
- OpenAPI JSON: `http://localhost:5252/openapi/v1.json`
- Health check (dùng cho warm-up/deploy): **http://localhost:5252/health**
- Trang quản trị: **http://localhost:5252/Admin** (tự chuyển về `/Admin/dang-nhap` nếu chưa đăng nhập)

### Tài khoản admin mặc định

| | Giá trị |
|---|---|
| Email | `admin@banova.vn` |
| Mật khẩu | `Banova@123` |

> Đổi mật khẩu này trước khi deploy thật (seed hiện đang cố định trong `DbSeeder`).

### Lệnh thường dùng

```bash
# Build + test
dotnet build
dotnet test

# EF Core
dotnet ef migrations add <TenMigration> --project src\Banova.Api\Banova.Api.csproj
dotnet ef database update --project src\Banova.Api\Banova.Api.csproj
dotnet ef migrations list --project src\Banova.Api\Banova.Api.csproj
```

### Kiểm thử

```bash
# Cách 1 — Toàn bộ: unit test (không cần DB)
dotnet test

# Cách 2 — Smoke test API công khai (cần server đang chạy, 35 ca)
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1

# Cách 3 — Smoke test Auth + Admin (cần server đang chạy, 50 ca)
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-admin.ps1
```

Kết quả mong đợi:
- `dotnet test` → **31/31 pass**
- `smoke-test.ps1` → `35/35 PASS`
- `smoke-admin.ps1` → `50/50 PASS`

Test bản đã deploy: thêm `-BaseUrl "https://your-be.azurewebsites.net"` vào 2 script smoke test.

### Reset database

```bash
dotnet ef database drop -f --project src\Banova.Api\Banova.Api.csproj
dotnet run --project src\Banova.Api --urls http://localhost:5252
```

> ⚠️ `dotnet ef database drop` chỉ xoá database, **không** xoá dữ liệu đã seed trong file. Chạy lại server là seeder nạp lại từ đầu.

### Đổi kết nối database

Dev: sửa `src/Banova.Api/appsettings.json` → `ConnectionStrings:Default`.
Production: **không sửa file**, đặt biến môi trường `ConnectionStrings__Default` (xem mục 8).

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

Mã lỗi: `VALIDATION` 400 · `UNAUTHORIZED` 401 · `FORBIDDEN` 403 · `NOT_FOUND` 404 · `CONFLICT` 409 · `RATE_LIMITED` 429 · `SERVER_ERROR` 500.

Quy ước khác: field **camelCase** · tiền **số nguyên VND** · thời gian **ISO 8601 UTC (có `Z`)** · enum trả **chuỗi** (`"PENDING"`, `"SCIENCE"`, `"RS2"`).

---

## 5. Trạng thái endpoint — ✅ 22/22 theo `API.md`

### API công khai

| Method | Endpoint | Ghi chú |
|---|---|---|
| GET | `/api/products` | `page`, `pageSize` (max 50), `category`, `q`, `sort` = `newest\|price_asc\|price_desc` |
| GET | `/api/products/{slug}` | 404 `NOT_FOUND` nếu không có |
| GET | `/api/categories` | Sắp theo `sortOrder` |
| GET | `/api/articles` | Chỉ `PUBLISHED`; lọc `type=SCIENCE\|BLOG`, `q` |
| GET | `/api/articles/{slug}` | 404 nếu chưa publish |
| POST | `/api/quiz/submit` | Nhận **cả 2** định dạng: `[{questionId,value}]` và `{goal, morning_routine, rs_experience, sensitivity}`. Có lưu `QuizResult` |
| POST | `/api/contact` | 400 `VALIDATION` kèm `details` theo từng field |
| POST | `/api/newsletter` | 409 `CONFLICT` nếu email đã đăng ký |
| POST | `/api/orders` | Server tự tính tiền từ DB (NFR-09) + snapshot giá (NFR-10) + trừ `stock`. 404 nếu sản phẩm không tồn tại, 409 nếu hết hàng |
| GET | `/api/orders/{orderCode}` | Public theo mã đơn (dùng cho trang xác nhận đơn) |

### Xác thực

| Method | Endpoint | Quyền | Ghi chú |
|---|---|---|---|
| POST | `/api/auth/login` | Public | Trả `AdminUser`, set cookie `banova_token` (httpOnly). 401 nếu sai. Rate limit 10/phút |
| POST | `/api/auth/logout` | Admin | Xoá cookie |
| GET | `/api/auth/me` | Admin | 401 nếu chưa đăng nhập/hết hạn |

### Quản trị (yêu cầu role `ADMIN`)

| Method | Endpoint | Ghi chú |
|---|---|---|
| GET | `/api/admin/products` | *Bổ sung*: danh sách cho admin, gồm cả sản phẩm tạm ẩn |
| POST | `/api/admin/products` | 201; `slug` trùng → 409 |
| PUT | `/api/admin/products/{id}` | Partial update: field nào gửi thì cập nhật, không gửi thì giữ nguyên |
| DELETE | `/api/admin/products/{id}` | Xoá mềm (`deletedAt`), giữ lịch sử đơn hàng |
| GET | `/api/admin/articles` | *Bổ sung*: gồm cả bản nháp |
| POST | `/api/admin/articles` | `PUBLISHED` → tự set `publishedAt`; SCIENCE thiếu `references` → 400 |
| PUT | `/api/admin/articles/{id}` | Partial update |
| DELETE | `/api/admin/articles/{id}` | Xoá mềm |
| GET | `/api/orders` | Danh sách + lọc `status`, tìm `q` (mã đơn/tên/SĐT), phân trang |
| PATCH | `/api/orders/{id}/status` | Huỷ đơn → **hoàn tồn kho**; mở lại đơn đã huỷ → kiểm tra + trừ kho lại |
| GET | `/api/admin/stats` | `totalOrders`, `pendingOrders`, `completedOrders`, `totalProducts`, `totalArticles`, `totalSubscribers`, `totalContacts`, `totalRevenue` |

### Trang quản trị Razor (`/Admin`)

| Trang | Chức năng |
|---|---|
| `/Admin/dang-nhap` | Đăng nhập, lưu cookie JWT |
| `/Admin` | Dashboard: 8 chỉ số + 6 đơn mới nhất |
| `/Admin/san-pham` | Danh sách + thêm/sửa đầy đủ thông số RS + tạm ẩn + xoá mềm |
| `/Admin/bai-viet` | Danh sách + soạn/sửa + DRAFT/PUBLISHED + nguồn trích dẫn + xoá mềm |
| `/Admin/don-hang` | Lọc theo trạng thái, tìm kiếm, xem sản phẩm trong đơn, đổi trạng thái |

---

## 6. Ghép với FE

FE (Vite) deploy tại `https://banova-virid.vercel.app`, BE ở domain riêng → **bắt buộc CORS**.
Whitelist nằm ở `appsettings.json` → `Cors:AllowedOrigins` (dev) hoặc biến môi trường khi deploy:

```
http://localhost:5173
http://127.0.0.1:5173
https://banova-virid.vercel.app
```

Phía FE cần:

```env
VITE_API_BASE_URL=http://localhost:5252/api      # khi dev
# VITE_API_BASE_URL=https://<domain-be>/api       # khi demo
```

Nếu FE gọi API admin bằng `fetch` kèm cookie thì phải thêm `credentials: 'include'`
(CORS đã bật `AllowCredentials`). Lưu ý: cookie `sameSite=lax` chỉ hoạt động khi FE và BE
cùng site (localhost ↔ localhost, hoặc cùng một domain). Trang admin Razor chạy trực tiếp
trên domain BE nên không bị ảnh hưởng.

### ⚠️ Lệch tên field giữa dữ liệu FE mock cũ và API — FE cần map lại

| FE mock cũ | API trả về |
|---|---|
| `image` | `imageUrl` |
| `rsBadge` (chuỗi "5.8g RS/khẩu phần") | `rsContent` (số) + `rsType` (`"RS2"`) |
| `formattedPrice` (chuỗi "89.000₫") | `price` (số 89000) — FE tự format |
| `unitInfo` ("Hũ 250 gram") | `unit` + `servingSize` |
| `category` (chuỗi) | `category` (object `{ id, name, slug }`) |
| `id: 'prod-01'` (mock) | `id` (GUID thật) — **giỏ hàng & đơn hàng phải dùng id này** |
| — | `slug` (dùng cho URL chi tiết sản phẩm / bài viết) |

---

## 7. Kiến trúc mã nguồn

```
src/Banova.Api/
├─ Entities/      # 9 bảng + 6 enum
├─ Data/          # AppDbContext, DbSeeder
├─ Dtos/          # DTO + query params + mapping
├─ Services/      # ★ NGHIỆP VỤ — CHỈ tầng này được chạm DbContext
├─ Controllers/   # Mỏng: nhận request -> gọi service -> trả ApiResult
├─ Pages/Admin/   # 5 trang Razor quản trị
├─ Common/        # ApiResult, ApiException, ExceptionMiddleware
└─ Lib/           # Constants, cookie auth, tên rate-limit policy

tests/Banova.Api.Tests/   # xUnit + EF InMemory
scripts/                  # smoke-test.ps1 (public) + smoke-admin.ps1 (auth/admin)
```

**Luật vàng:** Controller/Page **không bao giờ** import `AppDbContext` hay viết LINQ truy vấn — chỉ gọi Service.

---

## 8. Deploy

Vercel **không chạy được .NET** → BE phải ở host khác (Azure App Service, Render, MonsterASP…).

### Biến môi trường bắt buộc (Production)

| Biến | Ý nghĩa |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | Chuỗi kết nối SQL Server cloud |
| `Jwt__Secret` | Chuỗi bí mật **>= 32 ký tự**, sinh bằng `openssl rand -base64 48` |
| `Cors__AllowedOrigins__0`, `__1`… | Domain FE được phép gọi API |

Xem mẫu đầy đủ trong `.env.example`.
App sẽ **từ chối khởi động** nếu Production còn dùng `REPLACE_ME` trong connection string hoặc JWT secret (fail-fast, tránh deploy nhầm cấu hình).

### Docker (Render / VPS / Azure Container)

```bash
docker build -t banova-api .
docker run -p 8080:8080 --env-file .env banova-api
```

Container lắng nghe cổng **8080**, có sẵn `/health` để health check.
Với Azure App Service (Windows/IIS) dùng `web.config` đã có trong project.

### Warm-up trước demo

Gọi `GET /health` trước khi demo để tránh host free bị spin-down (request đầu chậm 30–60s).

---

## 9. Việc còn lại

- [x] Auth (login/logout/me) + cookie JWT
- [x] Admin CRUD sản phẩm (kèm xoá mềm)
- [x] Admin CRUD bài viết (kèm DRAFT/PUBLISHED + nguồn trích dẫn)
- [x] Admin danh sách đơn + đổi trạng thái (hoàn tồn kho khi huỷ)
- [x] Dashboard thống kê
- [x] 5 trang quản trị Razor
- [x] Dockerfile + `appsettings.Production.json` + `.env.example` + `web.config` + `/health`
- [x] Rate limit form công khai + chống brute-force đăng nhập
- [x] Unit test + 2 script smoke test
- [ ] Chọn host thật và deploy (Azure/Render/MonsterASP) + SQL Server cloud
- [ ] Đổi mật khẩu admin seed trước khi deploy
- [ ] Ghép API với FE (FE cần sửa các điểm lệch field ở mục 6)
- [ ] Rà soát pháp lý lần cuối trên dữ liệu seed + nội dung bài viết

---

## 10. Quy tắc nội dung (bắt buộc — liên quan pháp lý)

- **CẤM** dùng các từ: "chữa", "điều trị", "thay thuốc", "đặc trị".
- Mọi con số (GI, %RS, thống kê) phải có nguồn; bài SCIENCE bắt buộc có mục nguồn tham khảo
  (đã được validate ở `ArticleService`: thiếu `references` → 400).
- Disclaimer y tế phải xuất hiện ở footer, chi tiết sản phẩm và cuối bài khoa học.
