# BANOVA — DANH SÁCH TASK BACKEND (BE: ASP.NET CORE)

> **Người thực hiện:** BE (bạn)
> **Phạm vi sở hữu:** toàn bộ repo `Banova.Api` — database, API, xác thực, đơn hàng, trang quản trị
> **Cập nhật lần cuối:** 19/10/2026
> **Mốc demo:** Thứ 4, 21/10/2026

---

## ⛳ NGUYÊN TẮC VÀNG

1. **Controller mỏng** — chỉ: nhận request → gọi `Services/` → trả `ApiResult`. **CẤM** viết truy vấn LINQ hay chạm `AppDbContext` trong Controller.
2. **Bám đúng `API.md`** — đó là hợp đồng với FE. Đổi endpoint/shape phải báo cả nhóm trước, không tự ý sửa.
3. **Không tin dữ liệu từ client** — tiền và tổng đơn do server tính từ DB (NFR-09); snapshot giá vào `OrderItem` (NFR-10).
4. **Validate 100% input** bằng service + trả lỗi đúng format `{ error: { code, message, details } }`.
5. **Pháp lý y tế**: **CẤM** dùng các từ *"chữa"*, *"điều trị"*, *"thay thuốc"*, *"đặc trị"* trong mọi dữ liệu seed/nội dung.
6. **Nhánh Git**: `feat/be-*` cho tính năng, `fix/be-*` cho sửa lỗi. Không push thẳng lên `main` mà không báo nhóm.

---

## 0. CHỐT KỸ THUẬT

| Hạng mục | Quyết định | Ghi chú |
|---|---|---|
| Runtime | **.NET 10** (`net10.0`) | máy đã có SDK 10.0.401 |
| Framework | **ASP.NET Core Web API** (Controllers) | không dùng Minimal API để dễ chia nhóm |
| ORM | **EF Core 10** + `Microsoft.EntityFrameworkCore.SqlServer` | `dotnet-ef` 10.0.13 |
| Database | **SQL Server** — LocalDB khi dev, SQL Server cloud khi deploy | dev: `(localdb)\mssqllocaldb`, DB `BanovaDb` |
| Xác thực | **JWT Bearer** + cookie `banova_token` (httpOnly) | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| Hash mật khẩu | **BCrypt.Net-Next** | NFR-07 |
| Tài liệu API | **OpenAPI + Scalar UI** | `/scalar/v1` |
| Trang Admin | **Razor Pages** (không dùng React) | để BE không phải học FE stack |
| FE (do Người 1 làm) | React 19 + **Vite** SPA, deploy Vercel | gọi BE qua **absolute URL + CORS** |

### ⚠️ Khác biệt so với tài liệu nhóm (phải ghi vào báo cáo)

| Tài liệu nhóm ghi | Thực tế |
|---|---|
| `SRS v3.0`: ASP.NET Core Web API (C#) + Next.js | ✅ **BE đúng SRS**; FE dùng React/Vite thay Next.js |
| `Quy tắc code`: "Next.js fullstack (App Router)" | ❌ Không đúng — BE là **repo ASP.NET Core riêng** |
| `REQUIREMENTS`: PostgreSQL + Prisma | ❌ Đổi sang **SQL Server + EF Core** (phù hợp người làm BE) |
| API cùng origin, không cần CORS | ❌ FE ở Vercel, BE ở host .NET → **phải bật CORS** |

> **Lý do giải trình:** SRS gốc đã chọn ASP.NET Core cho BE; nhóm chỉ đổi vì muốn gộp 1 stack cho kịp 5 ngày, nhưng thực tế FE đã tách thành app Vite riêng nên lý do đó không còn. Dùng ASP.NET Core giúp tận dụng kỹ năng sẵn có và đúng SRS hơn.

---

## 1. LỘ TRÌNH (đã cập nhật theo thực tế)

```
[T6 16/10]  Chốt API.md + khung dự án                              ✅
   │
[T7 17/10]  DB + seed + API sản phẩm/bài viết                      ✅
   │
[CN 18/10]  API quiz/contact/newsletter + hạ tầng chung            ✅
   │
[T2 19/10]  Auth admin + CRUD admin + ĐẶT HÀNG END-TO-END          ⬅ ĐANG Ở ĐÂY
   │
[T3 20/10]  Admin UI (Razor) + dashboard + deploy + tích hợp FE    ⬜
   │
[T4 21/10]  Rà soát pháp lý + rehearse demo (không thêm tính năng) ⬜
```

---

## 2. CHI TIẾT TASK THEO GIAI ĐOẠN

### GIAI ĐOẠN 0 — KHUNG DỰ ÁN & DATABASE (16–17/10) ✅ XONG

- [x] Tạo solution `Banova.slnx` + project `src/Banova.Api` (.NET 10)
- [x] Cài package: EF Core SqlServer/Design, JwtBearer, BCrypt.Net-Next, FluentValidation, Scalar.AspNetCore
- [x] Cấu hình `appsettings.json`: `ConnectionStrings:Default`, `Cors:AllowedOrigins`, `Jwt:*`
- [x] **`AppDbContext`** — 9 bảng, index UNIQUE (`slug`, `email`, `orderCode`), soft delete, enum lưu dạng chuỗi
- [x] **Bộ chuyển đổi UTC** cho `DateTime` (SQL Server mất `DateTimeKind` → JSON thiếu `Z`)
- [x] **Migration** `InitialCreate` — tạo DB bằng `dotnet ef`
- [x] **`DbSeeder`** — idempotent, chạy tự động khi khởi động:
  - [x] 1 admin: `admin@banova.vn` / `Banova@123` (hash BCrypt)
  - [x] 4 danh mục: `bot`, `banh`, `bua-sang`, `hop-qua`
  - [x] **8 SKU đúng giá**: 89.000 · 159.000 · 69.000 · 39.000 · 25.000 · 69.000 · 49.000 · 219.000
  - [x] 5 bài viết: 3 SCIENCE (có `references`) + 2 BLOG, đều `PUBLISHED`
- [x] **Hạ tầng dùng chung**:
  - [x] `Common/ApiResult` — `Ok()` / `OkPaged()` / `Fail()`
  - [x] `Common/ApiException` — 6 mã lỗi chuẩn
  - [x] `Common/ExceptionMiddleware` — bắt mọi lỗi, trả JSON đúng format
  - [x] Ép lỗi model-binding của ASP.NET (mặc định là ProblemDetails) về `VALIDATION`
  - [x] `Lib/Constants` — `pageSize` mặc định 12 / tối đa 50 + nhãn tiếng Việt trạng thái đơn
  - [x] JSON: camelCase, enum ra chuỗi, bỏ field null, tiếng Việt đọc được
  - [x] **CORS** whitelist `localhost:5173` + `https://banova-virid.vercel.app`
  - [x] Sitemap tuyến: bỏ `UseHttpsRedirection` (làm hỏng request cross-origin khi demo)

### GIAI ĐOẠN 1 — API CÔNG KHAI PHỤC VỤ FE (17–18/10) ✅ XONG

- [x] **`IProductService` / `ProductService`**
  - [x] `listProducts` — lọc `category`, `q`, sắp `newest|price_asc|price_desc`, phân trang, chỉ `isActive && deletedAt == null`
  - [x] `getProductBySlug` — ném `404 NOT_FOUND`
- [x] **`ICategoryService`** — sắp theo `sortOrder`
- [x] **`IArticleService`** — chỉ trả `PUBLISHED`, lọc `type`, `q`; `404` nếu chưa publish
- [x] **`IQuizService`** — chấm điểm theo bộ luật `goal / morning_routine / rs_experience / sensitivity`, **lưu `QuizResult`**, nhận **cả 2** định dạng body
- [x] **`IContactService`** — validate từng field, trả `details` theo field
- [x] **`INewsletterService`** — trùng email trả **409 CONFLICT**
- [x] Controllers: `Products`, `Categories`, `Articles`, `Quiz`, `Marketing`
- [x] **Đã test thật 14 ca** (xem mục 6)

### GIAI ĐOẠN 2 — AUTH + ADMIN API (19/10) ⬜ CHƯA LÀM

- [ ] **`JwtTokenService`** — ký/kiểm tra JWT bằng `Microsoft.IdentityModel.Tokens`, đọc `Jwt:Secret` từ config
- [ ] Cấu hình `AddAuthentication().AddJwtBearer()` — **đọc token từ cookie `banova_token`** (không chỉ header)
- [ ] `POST /api/auth/login` — BCrypt verify → set cookie `banova_token` (httpOnly, sameSite=lax) → trả thông tin admin
  - [ ] `401 UNAUTHORIZED` khi sai tài khoản/mật khẩu
- [ ] `POST /api/auth/logout` — xoá cookie
- [ ] `GET /api/auth/me` 🔒 — `401` nếu chưa đăng nhập
- [ ] Helper **`requireAdmin()`** — dùng `[Authorize(Roles = "ADMIN")]`
- [ ] **`AdminProductsController`** 🔒
  - [ ] `POST /api/admin/products` → `201`
  - [ ] `PUT /api/admin/products/{id}`
  - [ ] `DELETE /api/admin/products/{id}` → **soft delete** (chỉ set `deletedAt`)
- [ ] **`AdminArticlesController`** 🔒
  - [ ] `POST /api/admin/articles` — nếu `status = PUBLISHED` → tự set `publishedAt = now`
  - [ ] `PUT /api/admin/articles/{id}`
  - [ ] `DELETE /api/admin/articles/{id}`
- [ ] `AdminStatsController` 🔒 — `GET /api/admin/stats` *(Should)*

### GIAI ĐOẠN 3 — ĐẶT HÀNG END-TO-END (19/10) ★ TRỌNG ĐIỂM ⬜ CHƯA LÀM

- [ ] **`OrderService.createOrder`** — trong **1 transaction**:
  - [ ] Tra giá từ DB cho từng `productId` (KHÔNG nhận giá từ client — NFR-09)
  - [ ] Sản phẩm không tồn tại → `404 NOT_FOUND`
  - [ ] Hết hàng (`stock` < số lượng) → `409 CONFLICT`
  - [ ] Tính `subTotal`, `shippingFee = 0`, `totalAmount`
  - [ ] Tạo `Order` status `PENDING` + `OrderItem` **snapshot `productName` + `unitPrice`** (NFR-10)
  - [ ] Sinh `orderCode` dạng **`BNV-yyyyMMdd-XXX`** — đảm bảo UNIQUE, chống trùng khi tạo đồng thời
  - [ ] Trừ `stock` sản phẩm
- [ ] `POST /api/orders` — Public (Guest checkout) → `201`
- [ ] `GET /api/orders/{orderCode}` — Public theo mã đơn → `404` nếu sai
- [ ] `GET /api/orders` 🔒 — phân trang + lọc `status`
- [ ] `PATCH /api/orders/{id}/status` 🔒 — validate enum, `400` nếu sai
- [ ] Body đơn hàng: `{ customerName, phone, email, address, note, paymentMethod, items:[{productId, quantity}] }`

### GIAI ĐOẠN 4 — TRANG QUẢN TRỊ (Razor Pages) (20/10) ⬜ CHƯA LÀM

- [ ] `/Admin/dang-nhap` — form đăng nhập, lưu cookie
- [ ] Guard: mọi trang `/Admin/*` phải đăng nhập (Razor Pages convention + `[Authorize]`)
- [ ] `/Admin` — Dashboard 4 chỉ số: tổng đơn · đơn chờ xử lý · số sản phẩm · số bài viết · số đăng ký nhận tin
- [ ] `/Admin/san-pham` — danh sách (ảnh, giá, tồn kho, trạng thái) + form thêm/sửa đầy đủ `rsContent, rsType, gi, fiber, ingredients, usage`
- [ ] `/Admin/bai-viet` — danh sách + form viết bài + nút **DRAFT / PUBLISH** + ô `references`
- [ ] `/Admin/don-hang` — danh sách đơn + lọc trạng thái + xem chi tiết + **đổi trạng thái**

### GIAI ĐOẠN 5 — TÍCH HỢP FE + DEPLOY (20/10) ⬜ CHƯA LÀM

- [ ] Bàn giao FE: `VITE_API_BASE_URL`, link Scalar, bảng map field (xem mục 5)
- [ ] Chạy đủ **6 kịch bản demo** cùng Người 1
- [ ] **Deploy** (⚠️ **Vercel KHÔNG chạy được .NET**):
  - [ ] Chọn host: Azure App Service F1 (free) / MonsterASP.NET (free) / Render (Docker)
  - [ ] SQL Server cloud (hoặc LocalDB nếu demo tại máy)
  - [ ] Đổi `Jwt:Secret` + `ConnectionStrings` bằng **biến môi trường**, không commit
  - [ ] Cập nhật `Cors:AllowedOrigins` = domain FE thật
- [ ] Warm-up host trước demo (nếu plan free có spin-down)

### GIAI ĐOẠN 6 — RÀ SOÁT PHÁP LÝ & REHEARSE (21/10) ⬜ CHƯA LÀM

- [ ] Quét toàn bộ dữ liệu seed + nội dung: không còn *"chữa"*, *"điều trị"*, *"thay thuốc"*, *"đặc trị"*
- [ ] Mọi bài `SCIENCE` bắt buộc có `references` (NFR-03/NFR-09)
- [ ] `dotnet build` phải **0 warning, 0 error**
- [ ] Không lộ secret ra log/response
- [ ] Code freeze — chỉ sửa lỗi nhỏ, không thêm tính năng

---

## 3. BẢNG THEO DÕI ENDPOINT

| # | Method | Endpoint | Quyền | Trạng thái |
|---|---|---|---|---|
| 1 | GET | `/api/products` | Public | ✅ Xong |
| 2 | GET | `/api/products/{slug}` | Public | ✅ Xong |
| 3 | GET | `/api/categories` | Public | ✅ Xong |
| 4 | GET | `/api/articles` | Public | ✅ Xong |
| 5 | GET | `/api/articles/{slug}` | Public | ✅ Xong |
| 6 | POST | `/api/quiz/submit` | Public | ✅ Xong |
| 7 | POST | `/api/contact` | Public | ✅ Xong |
| 8 | POST | `/api/newsletter` | Public | ✅ Xong |
| 9 | POST | `/api/orders` | Public | ⬜ Chưa |
| 10 | GET | `/api/orders/{orderCode}` | Public | ⬜ Chưa |
| 11 | POST | `/api/auth/login` | Public | ⬜ Chưa |
| 12 | POST | `/api/auth/logout` | Admin | ⬜ Chưa |
| 13 | GET | `/api/auth/me` | Admin | ⬜ Chưa |
| 14 | POST | `/api/admin/products` | Admin | ⬜ Chưa |
| 15 | PUT | `/api/admin/products/{id}` | Admin | ⬜ Chưa |
| 16 | DELETE | `/api/admin/products/{id}` | Admin | ⬜ Chưa |
| 17 | POST | `/api/admin/articles` | Admin | ⬜ Chưa |
| 18 | PUT | `/api/admin/articles/{id}` | Admin | ⬜ Chưa |
| 19 | DELETE | `/api/admin/articles/{id}` | Admin | ⬜ Chưa |
| 20 | GET | `/api/orders` | Admin | ⬜ Chưa |
| 21 | PATCH | `/api/orders/{id}/status` | Admin | ⬜ Chưa |
| 22 | GET | `/api/admin/stats` | Admin | ⬜ Chưa |

> **Ưu tiên nếu trễ tiến độ:** 9 → 11 → 20 → 21 → 14 → 17. Cắt được: 22 (stats) và các trang Admin phụ.
> **TUYỆT ĐỐI KHÔNG CẮT:** 1, 2, 4, 5, 9, 11, 14, 17, 20, 21.

---

## 4. TIÊU CHUẨN HOÀN THÀNH (DoD)

Một task chỉ được tick ✅ khi:

- [ ] Chạy được ở local, không lỗi ở terminal
- [ ] Trả đúng format: `{ data }` / `{ data, meta }` / `{ error: { code, message, details } }`
- [ ] Input đã validate, sai trả `400 VALIDATION` kèm `details` theo field
- [ ] **Không import `AppDbContext` ngoài tầng `Services/`**
- [ ] Mọi endpoint admin đều có `[Authorize(Roles = "ADMIN")]`
- [ ] `dotnet build` sạch (0 warning, 0 error)
- [ ] Có dữ liệu mẫu để bấm thử
- [ ] Tuân thủ quy tắc nội dung (mục 6)

---

## 5. BÀN GIAO CHO FE

### Cấu hình FE cần
```env
VITE_API_BASE_URL=http://localhost:5252/api          # khi dev
# VITE_API_BASE_URL=https://<domain-be>/api           # khi demo
```
- Origin đã whitelist CORS: `http://localhost:5173`, `http://127.0.0.1:5173`, `https://banova-virid.vercel.app`
- Test API: **http://localhost:5252/scalar/v1**

### ⚠️ Lệch tên field — FE phải map lại (khuyến nghị viết `src/services/api.js`)

| FE đang đọc (mock) | API trả về |
|---|---|
| `image` | `imageUrl` |
| `rsBadge` (chuỗi `"5.8g RS/khẩu phần"`) | `rsContent` (số) + `rsType` (`"RS2"`) |
| `formattedPrice` (`"89.000₫"`) | `price` (số `89000`) — FE tự format |
| `unitInfo` (`"Hũ 250 gram"`) | `unit` + `servingSize` |
| `category` (chuỗi) | `category` (object `{ id, name, slug, ... }`) |
| `id: 'prod-01'` (mock) | `id` (GUID thật) — **giỏ hàng/đơn hàng phải dùng id này** |
| — | `slug` (dùng cho URL chi tiết sản phẩm / bài viết) |

### Quy ước chung
- Field **camelCase**; tiền **số nguyên VND**; thời gian **ISO 8601 UTC** (có `Z`)
- Enum trả **chuỗi**: `"PENDING"`, `"CONFIRMED"`, `"SCIENCE"`, `"RS2"`
- `?page=1&pageSize=12` (mặc định 12, tối đa 50)

---

## 6. NHẬT KÝ KIỂM THỬ (đã chạy thật ngày 18/10)

| # | Ca kiểm thử | Kết quả |
|---|---|---|
| 1 | `GET /api/categories` | ✅ 200, 4 danh mục đúng `sortOrder` |
| 2 | `GET /api/products?sort=price_asc` | ✅ 200, sắp giá tăng đúng |
| 3 | `GET /api/products?category=banh` | ✅ 200, lọc đúng |
| 4 | `GET /api/products/{slug}` | ✅ 200, đủ bộ thông số RS |
| 5 | `GET /api/products/khong-ton-tai` | ✅ 404 `NOT_FOUND` |
| 6 | `GET /api/articles?type=SCIENCE` | ✅ 200, đúng 3 bài |
| 7 | `GET /api/articles?type=BLOG` | ✅ 200, đúng 2 bài |
| 8 | `POST /api/quiz/submit` (object) | ✅ 200, `score:10` → Bột 250g |
| 9 | `POST /api/quiz/submit` (array) | ✅ 200, `score:8` → Muffin |
| 10 | `POST /api/newsletter` lần 1 | ✅ 201 |
| 11 | `POST /api/newsletter` trùng | ✅ 409 `CONFLICT` |
| 12 | `POST /api/contact` thiếu dữ liệu | ✅ 400 `VALIDATION` + `details` |
| 13 | `POST /api/contact` hợp lệ | ✅ 201 |
| 14 | CORS preflight từ domain Vercel | ✅ 204 + `Access-Control-Allow-Origin` |
| 15 | Seeder tự chạy khi khởi động | ✅ 1 admin · 4 danh mục · 8 SKU · 5 bài viết |
| 16 | `dotnet build` | ✅ 0 warning, 0 error |

---

## 7. RỦI RO & LƯU Ý

| Rủi ro | Cách xử lý |
|---|---|
| **Vercel không host được .NET** → BE phải ở domain thứ 2 | Chọn Azure App Service F1 / MonsterASP.NET / Render; giữ CORS whitelist đúng domain |
| Host free có **spin-down** (Render) → request đầu chậm 30–60s | Warm-up trước demo, hoặc chọn host không spin-down |
| `orderCode` trùng khi tạo 2 đơn cùng lúc | Sinh mã trong transaction + unique index + retry |
| SQL Server mất `DateTimeKind` → JSON thiếu `Z` | Đã có `UtcDateTimeConverter` trong `AppDbContext` |
| LocalDB chỉ chạy trên máy dev | Deploy phải đổi `ConnectionStrings` sang SQL Server cloud |
| `Jwt:Secret` đang nằm trong `appsettings.json` | **Đổi trước khi deploy**, đưa vào biến môi trường |
| Vi phạm từ ngữ y tế trong nội dung | Quét lại toàn bộ seed + để Người 1 sửa `ArticleDetailPage.jsx` (đang chứa từ *"điều trị"*) |
| FE chưa có router/slug → không deep-link được | Chuẩn bị sẵn endpoint theo `slug` (đã xong); Người 1 tự xử lý phần route |

---

## 8. LỆNH THƯỜNG DÙNG

```bash
# Chạy dev
dotnet run --project src\Banova.Api --urls http://localhost:5252
# Test API: http://localhost:5252/scalar/v1

# Build kiểm tra (phải 0 warning, 0 error trước khi commit)
dotnet build

# EF Core
dotnet ef migrations add <TenMigration> --project src\Banova.Api\Banova.Api.csproj
dotnet ef database update --project src\Banova.Api\Banova.Api.csproj
dotnet ef migrations list --project src\Banova.Api\Banova.Api.csproj

# Git (nhánh riêng theo quy ước nhóm)
git checkout -b feat/be-dat-hang
git commit -m "feat(order): them API tao don hang mo phong"
git push -u origin feat/be-dat-hang
```

---

## 9. TÀI KHOẢN & DỮ LIỆU MẪU

| | Giá trị |
|---|---|
| Admin | `admin@banova.vn` / `Banova@123` |
| Database (dev) | `(localdb)\mssqllocaldb` → `BanovaDb` |
| Danh mục | `bot` · `banh` · `bua-sang` · `hop-qua` |
| Repo | https://github.com/ThaiTuan05/Banova.API |

---

*Mọi thay đổi hợp đồng API phải báo cả nhóm trước — FE đang code dựa trên `API.md`.*
