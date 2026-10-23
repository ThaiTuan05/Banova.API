# BANOVA — DANH SÁCH TASK BACKEND (BE: ASP.NET CORE)

> **Người thực hiện:** BE (bạn)
> **Phạm vi sở hữu:** toàn bộ repo `Banova.Api` — database, API, xác thực, đơn hàng, trang quản trị
> **Cập nhật lần cuối:** 21/10/2026
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
[CN 18/10]  API quiz/contact/newsletter + ha tang chung            ✅
   │
[T2 19/10]  API dat hang (POST/GET) phuc vu CheckoutPage            ✅
   │        Auth + Admin API + 5 trang Razor                        ✅ (21/10)
   │
[T3 20/10]  Deploy + ghep API voi FE + ra soat phap ly             ⬅ DANG O DAY
   │
[T4 21/10]  Rehearse demo (khong them tinh nang)                    ⬜
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

### GIAI ĐOẠN 2 — AUTH + ADMIN API ✅ XONG (21/10)

> **Phạm vi:** mở lại toàn bộ phần admin đã hoãn ở đợt 19/10, phục vụ trang quản trị Razor.
> API công khai phục vụ FE giữ nguyên, không thay đổi shape.

- [x] **`JwtTokenService`** — ký/kiểm tra JWT bằng `Microsoft.IdentityModel.Tokens`, đọc `Jwt:Secret` từ config
- [x] Cấu hình `AddAuthentication().AddJwtBearer()` — **đọc token từ cookie `banova_token`** (và cả header `Authorization`)
- [x] `POST /api/auth/login` — BCrypt verify → set cookie `banova_token` (httpOnly, sameSite=lax) → trả thông tin admin
  - [x] `401 UNAUTHORIZED` khi sai tài khoản/mật khẩu (cùng một message để không lộ email tồn tại)
  - [x] Rate limit 10 request/phút/IP chống brute-force
- [x] `POST /api/auth/logout` — xoá cookie
- [x] `GET /api/auth/me` 🔒 — `401` nếu chưa đăng nhập
- [x] `[Authorize(Roles = "ADMIN")]` cho toàn bộ controller admin
- [x] **`AdminProductsController`** 🔒
  - [x] `GET /api/admin/products` *(bổ sung)* — danh sách gồm cả sản phẩm tạm ẩn
  - [x] `POST /api/admin/products` → `201`; slug trùng → `409 CONFLICT`
  - [x] `PUT /api/admin/products/{id}` — partial update
  - [x] `DELETE /api/admin/products/{id}` → **soft delete** (chỉ set `deletedAt`)
- [x] **`AdminArticlesController`** 🔒
  - [x] `GET /api/admin/articles` *(bổ sung)* — gồm cả bản nháp
  - [x] `POST /api/admin/articles` — nếu `status = PUBLISHED` → tự set `publishedAt = now`; SCIENCE thiếu `references` → `400`
  - [x] `PUT /api/admin/articles/{id}`
  - [x] `DELETE /api/admin/articles/{id}` → soft delete
- [x] `AdminStatsController` 🔒 — `GET /api/admin/stats` (8 chỉ số, gồm doanh thu trừ đơn huỷ)
- [x] `GET /api/orders` 🔒 — lọc `status`, tìm `q`, phân trang
- [x] `PATCH /api/orders/{id}/status` 🔒 — huỷ đơn **hoàn tồn kho**, mở lại đơn đã huỷ thì kiểm tra + trừ kho lại

### GIAI ĐOẠN 3 — ĐẶT HÀNG MÔ PHỎNG (19/10) ★ TRỌNG ĐIỂM ✅ XONG PHẦN FE CẦN

- [x] **`OrderService.createAsync`** — lưu cả đơn + trừ kho trong **1 `SaveChanges`** (transaction ngầm):
  - [x] Tra giá từ DB cho từng `productId` (KHÔNG nhận giá từ client — NFR-09)
  - [x] Gộp dòng trùng sản phẩm trước khi kiểm tra tồn kho
  - [x] Sản phẩm không tồn tại / ngừng bán → `404 NOT_FOUND` kèm danh sách `productIds`
  - [x] Hết hàng (`stock` < số lượng) → `409 CONFLICT` kèm `{ productId, name, stock, requested }`
  - [x] Tính `subTotal`, `shippingFee = 0`, `totalAmount`
  - [x] Tạo `Order` status `PENDING` + `OrderItem` **snapshot `productName` + `unitPrice`** (NFR-10)
  - [x] Sinh `orderCode` dạng **`BNV-yyyyMMdd-XXX`** — số thứ tự trong ngày theo **giờ Việt Nam**, dò tới khi không trùng
  - [x] Trừ `stock` sản phẩm
  - [x] `paymentMethod` nhận `COD` | `DEMO`, giá trị khác → `400 VALIDATION`
- [x] `POST /api/orders` — Public (Guest checkout) → `201` trả đầy đủ đơn + `items`
- [x] `GET /api/orders/{orderCode}` — Public theo mã đơn → `404` nếu sai
- [x] Thêm `statusLabel` (nhãn tiếng Việt) vào response cho FE hiển thị trực tiếp
- [x] `GET /api/orders` 🔒 — danh sách, lọc `status`, tìm `q`, phân trang (21/10)
- [x] `PATCH /api/orders/{id}/status` 🔒 — đổi trạng thái + hoàn/trừ tồn kho tương ứng (21/10)

### GIAI ĐOẠN 4 — TRANG QUẢN TRỊ (Razor Pages) ✅ XONG (21/10)

- [x] `/Admin/dang-nhap` — form đăng nhập, lưu cookie
- [x] Guard: `AuthorizeFolder("/Admin")` + cho phép anonymous ở trang đăng nhập; chưa login → redirect kèm `returnUrl`
- [x] `/Admin` — Dashboard: tổng đơn · chờ xử lý · hoàn thành · doanh thu · sản phẩm · bài viết · đăng ký nhận tin · liên hệ + 6 đơn mới nhất
- [x] `/Admin/san-pham` — danh sách (giá, tồn kho, trạng thái, tìm kiếm) + form thêm/sửa đầy đủ `rsContent, rsType, gi, fiber, ingredients, usage` + xoá mềm
- [x] `/Admin/bai-viet` — danh sách + form viết bài + **DRAFT / PUBLISHED** + ô `references` + xoá mềm
- [x] `/Admin/don-hang` — danh sách + lọc trạng thái + tìm mã đơn/tên/SĐT + xem chi tiết món + **đổi trạng thái**

### GIAI ĐOẠN 5 — TÍCH HỢP FE + DEPLOY (20/10) ⬜ CÒN LẠI

- [x] Bàn giao FE: `VITE_API_BASE_URL`, link Scalar, bảng map field (xem mục 5)
- [ ] Chạy đủ **6 kịch bản demo** cùng Người 1
- [ ] **Deploy** (⚠️ **Vercel KHÔNG chạy được .NET**):
  - [x] Chuẩn bị artifact: `Dockerfile`, `web.config`, `appsettings.Production.json`, `.env.example`, `/health`
  - [ ] Chọn host: Azure App Service F1 (free) / MonsterASP.NET (free) / Render (Docker)
  - [ ] SQL Server cloud (hoặc LocalDB nếu demo tại máy)
  - [x] Đổi `Jwt:Secret` + `ConnectionStrings` bằng **biến môi trường** — app từ chối khởi động nếu Production còn `REPLACE_ME`
  - [ ] Cập nhật `Cors:AllowedOrigins` = domain FE thật (đã bật `AllowCredentials` cho cookie admin)
- [ ] Warm-up host trước demo (gọi `/health` nếu plan free có spin-down)

### GIAI ĐOẠN 6 — RÀ SOÁT PHÁP LÝ & REHEARSE (21/10) ⬜ CÒN LẠI

- [ ] Quét toàn bộ dữ liệu seed + nội dung: không còn *"chữa"*, *"điều trị"*, *"thay thuốc"*, *"đặc trị"*
- [x] Mọi bài `SCIENCE` bắt buộc có `references` (NFR-03/NFR-09) — đã validate trong `ArticleService`
- [x] `dotnet build` phải **0 warning, 0 error**
- [x] `dotnet test` **31/31 pass**
- [x] Không lộ secret ra log/response
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
| 9 | POST | `/api/orders` | Public | ✅ Xong |
| 10 | GET | `/api/orders/{orderCode}` | Public | ✅ Xong |
| 11 | POST | `/api/auth/login` | Public | ✅ Xong |
| 12 | POST | `/api/auth/logout` | Admin | ✅ Xong |
| 13 | GET | `/api/auth/me` | Admin | ✅ Xong |
| 14 | POST | `/api/admin/products` | Admin | ✅ Xong |
| 15 | PUT | `/api/admin/products/{id}` | Admin | ✅ Xong |
| 16 | DELETE | `/api/admin/products/{id}` | Admin | ✅ Xong |
| 17 | POST | `/api/admin/articles` | Admin | ✅ Xong |
| 18 | PUT | `/api/admin/articles/{id}` | Admin | ✅ Xong |
| 19 | DELETE | `/api/admin/articles/{id}` | Admin | ✅ Xong |
| 20 | GET | `/api/orders` | Admin | ✅ Xong |
| 21 | PATCH | `/api/orders/{id}/status` | Admin | ✅ Xong |
| 22 | GET | `/api/admin/stats` | Admin | ✅ Xong |

> **Trạng thái: 22/22 endpoint theo `API.md` đã xong** (1–22), kèm 2 endpoint bổ sung cho trang admin
> (`GET /api/admin/products`, `GET /api/admin/articles`) và `GET /health` cho deploy.
> Kiểm thử: `dotnet test` **31/31 pass** · `scripts/smoke-test.ps1` **35/35 pass** · `scripts/smoke-admin.ps1` **50/50 pass**.

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

### Đợt 2 — Đặt hàng (19/10, nhánh `feat/be-dat-hang`)

| # | Ca kiểm thử | Kết quả |
|---|---|---|
| 17 | `POST /api/orders` 2×250g + 1×cookie | ✅ 201, `BNV-20261019-001`, `subTotal = 247000` (server tự tính) |
| 18 | `POST /api/orders` đơn thứ 2 | ✅ 201, mã tăng thành `BNV-20261019-002` |
| 19 | Kiểm tra trừ tồn kho | ✅ SP1 từ `120` → `117` (đúng 3 sản phẩm) |
| 20 | `GET /api/orders/BNV-20261019-001` | ✅ 200, trả đủ `items` + `statusLabel` |
| 21 | `GET /api/orders/BNV-99999999-999` | ✅ 404 `NOT_FOUND` |
| 22 | `POST /api/orders` thiếu họ tên + địa chỉ | ✅ 400 `VALIDATION` + `details` theo field |
| 23 | `POST /api/orders` sản phẩm không tồn tại | ✅ 404 `NOT_FOUND` + `details.productIds` |
| 24 | `POST /api/orders` vượt tồn kho (hộp quà ×999) | ✅ 409 `CONFLICT` + `{ stock, requested }` |
| 25 | `POST /api/orders` `paymentMethod = "VNPAY"` | ✅ 400 `VALIDATION` |
| 26 | `POST /api/orders` `quantity = 0` | ✅ 400 `VALIDATION` |
| 27 | CORS preflight `OPTIONS /api/orders` | ✅ 204 + `Access-Control-Allow-Origin` |

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
