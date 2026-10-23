# BANOVA — VIỆC FE CẦN LÀM (bàn giao từ BE)

> **Người nhận:** Người 1 (FE khách hàng)
> **Từ:** BE — `Banova.Api`
> **Cập nhật:** 19/10/2026
> **Trạng thái BE:** ✅ 10/10 endpoint phục vụ FE hiện tại đã xong, smoke test **35/35 PASS**

---

## 0. TÌNH HÌNH HIỆN TẠI

| | |
|---|---|
| **BE** | Đã chạy thật: sản phẩm, danh mục, bài viết, quiz, liên hệ, newsletter, **đặt hàng** |
| **FE** | 7 trang đã xong UI nhưng **100% dùng mock**, chưa có một dòng `fetch` nào gọi API |
| **Chặn demo** | Bước "đặt hàng → nhận mã `BNV-...`" và "dữ liệu thật từ DB" **chưa hoạt động vì FE chưa nối API** |

> ⚠️ **Đây là việc chặn đường găng.** BE xong rồi, nhưng nếu FE chưa nối thì khi demo vẫn chỉ thấy dữ liệu giả trong code.

---

## 1. VIỆC BẮT BUỘC (P0) — làm theo đúng thứ tự

### Bước 1 — Thêm biến môi trường

Tạo file `.env.local` ở gốc repo FE (ngang hàng `package.json`):

```env
VITE_API_BASE_URL=http://localhost:5252/api
```

Khi deploy lên Vercel: **Settings → Environment Variables** → thêm `VITE_API_BASE_URL` = `https://<domain-be>/api` → **Redeploy**.
> ⚠️ Vite chỉ đọc biến môi trường **lúc build**, thêm biến mà không redeploy sẽ không có tác dụng.

### Bước 2 — Tạo `src/services/api.js`

```js
// src/services/api.js
const BASE_URL = import.meta.env.VITE_API_BASE_URL;

if (!BASE_URL) {
  console.warn('[api] Thiếu VITE_API_BASE_URL trong .env');
}

function toQuery(params = {}) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.append(key, value);
  });
  const qs = search.toString();
  return qs ? `?${qs}` : '';
}

async function request(path, options = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
    ...options,
  });

  const body = await response.json().catch(() => ({}));

  if (!response.ok) {
    const error = body?.error ?? { code: 'SERVER_ERROR', message: 'Không kết nối được máy chủ' };
    const exception = new Error(error.message);
    exception.code = error.code;      // 'VALIDATION' | 'NOT_FOUND' | 'CONFLICT' | ...
    exception.details = error.details;
    exception.status = response.status;
    throw exception;
  }

  return body; // { data } hoặc { data, meta }
}

export const api = {
  products: {
    list: (params = {}) => request(`/products${toQuery(params)}`),
    detail: (slug) => request(`/products/${encodeURIComponent(slug)}`),
  },
  categories: {
    list: () => request('/categories'),
  },
  articles: {
    list: (params = {}) => request(`/articles${toQuery(params)}`),
    detail: (slug) => request(`/articles/${encodeURIComponent(slug)}`),
  },
  quiz: {
    // BE nhận đúng shape FE đang có: { goal, morning_routine, rs_experience, sensitivity }
    submit: (answers) => request('/quiz/submit', { method: 'POST', body: JSON.stringify({ answers }) }),
  },
  orders: {
    create: (payload) => request('/orders', { method: 'POST', body: JSON.stringify(payload) }),
    detail: (orderCode) => request(`/orders/${encodeURIComponent(orderCode)}`),
  },
  newsletter: {
    subscribe: (email) => request('/newsletter', { method: 'POST', body: JSON.stringify({ email }) }),
  },
  contact: {
    send: (payload) => request('/contact', { method: 'POST', body: JSON.stringify(payload) }),
  },
};
```

### Bước 3 — Tạo mapper (đổi shape API → shape UI đang dùng)

```js
// src/services/mappers.js
const vnd = new Intl.NumberFormat('vi-VN');

export const formatPrice = (amount) => `${vnd.format(amount)}₫`;

/** ProductDto của BE -> shape các component UI đang đọc */
export const mapProduct = (p) => ({
  ...p,
  image: p.imageUrl ?? '',
  formattedPrice: formatPrice(p.price),
  rsBadge: `${p.rsContent}g ${p.rsType}/khẩu phần`,
  unitInfo: `${p.unit} ${p.servingSize}`,
  subInfo: p.shortDescription ?? '',
  // UI có đọc nhưng BE không trả -> để undefined để JSX tự bỏ qua
  badge: undefined,
  discount: undefined,
  originalPrice: undefined,
});

/** Kết quả quiz -> shape màn hình kết quả đang dùng */
export const mapQuizResult = (data) => ({
  score: data.score,
  message: data.message,
  product: {
    ...mapProduct(data.recommendedProduct),
    rsTag: `${data.recommendedProduct.rsContent}g ${data.recommendedProduct.rsType}`,
    whyFitText: data.message,
  },
});
```

### Bước 4 — Thay mock bằng API

| File | Đang mock | Thay bằng | Lưu ý |
|---|---|---|---|
| `components/home/FeaturedProducts.jsx` | 4 SKU hardcode | `api.products.list({ pageSize: 4 })` | map qua `mapProduct` |
| `pages/CatalogPage.jsx` | mảng + **lọc client-side** | `api.products.list({ page, pageSize, category, q, sort })` | bỏ lọc tay, dùng `meta.totalPages` cho phân trang |
| `pages/ProductDetailPage.jsx` | nhận object qua props | `api.products.detail(slug)` | **cần slug trên URL** |
| `pages/SciencePage.jsx` | mảng bài viết | `api.articles.list({ type: 'SCIENCE' \| 'BLOG' })` | |
| `pages/ArticleDetailPage.jsx` | nhận object qua props | `api.articles.detail(slug)` | **cần slug trên URL** |
| `pages/QuizPage.jsx` | **tự chấm điểm ở client** | `api.quiz.submit(answers)` | giữ nguyên state `answers` hiện tại |
| `pages/CheckoutPage.jsx` | tự sinh mã đơn, cart id giả | `api.orders.create(payload)` + `api.orders.detail(code)` | xem Bước 5 |
| `components/layout/Footer.jsx` | `setTimeout` giả | `api.newsletter.subscribe(email)` | xử lý `409 CONFLICT` |
| `pages/SciencePage.jsx` (hộp newsletter) | `setTimeout` giả | `api.newsletter.subscribe(email)` | |
| *(chưa có trang Liên hệ)* | — | `api.contact.send(payload)` | BE đã sẵn sàng |

### Bước 5 — Sửa 4 lỗi luồng (quan trọng nhất)

**5.1. Giỏ hàng phải lưu `id` thật (GUID)**
Hiện `INITIAL_CART_ITEMS` trong `App.jsx` dùng `id: 'prod-01'`. `POST /api/orders` sẽ trả **404 NOT_FOUND** nếu gửi id giả.
→ Cart chỉ được chứa sản phẩm lấy từ API, `id` là GUID do BE trả về. Bỏ hẳn `INITIAL_CART_ITEMS` mock.

**5.2. Bỏ tự sinh `orderCode`**
`CheckoutPage.jsx` dòng ~77 đang tự sinh mã (`BNV-${year}1019-${random}`). Phải lấy từ response:

```js
const { data } = await api.orders.create({
  customerName: form.fullName,
  phone: form.phone,
  email: form.email || null,
  address: form.address,
  note: form.note || null,
  paymentMethod: form.payment === 'cod' ? 'COD' : 'DEMO', // VietQR -> DEMO
  items: cartItems.map((i) => ({ productId: i.id, quantity: i.quantity })),
});

setOrderCode(data.orderCode);   // mã thật từ server: BNV-20261019-001
setOrderResult(data);
// KHÔNG gửi giá lên server — server tự tra giá từ DB (NFR-09)
```

**5.3. `paymentMethod` chỉ có 2 giá trị**
BE chấp nhận `COD` | `DEMO`. UI có 3 lựa chọn (Demo Instant / COD / VietQR) → **VietQR phải map về `DEMO`**, nếu không sẽ nhận `400 VALIDATION`.

**5.4. Chi tiết sản phẩm/bài viết phải fetch theo `slug`**
Hiện FE truyền nguyên object qua `useState` (`handleSelectProduct`), nên:
- Không F5 được trang chi tiết
- Không share link được
- Khi nối API sẽ không biết gọi slug nào

→ Cần thêm router (hoặc ít nhất đọc slug từ `window.location`) rồi gọi `api.products.detail(slug)` / `api.articles.detail(slug)`.

### Bước 6 — Loading & hiển thị lỗi

- Hiện **skeleton** khi đang chờ (đừng để trắng trang).
- Hiển thị `error.message` từ BE (đã là tiếng Việt, viết sẵn cho người dùng đọc).
- Dùng `error.code` để xử lý riêng:
  - `NOT_FOUND` → trang "Không tìm thấy sản phẩm"
  - `CONFLICT` (409) → "Email này đã đăng ký" / "Sản phẩm không đủ hàng"
  - `VALIDATION` (400) → tô đỏ field trong `error.details`

---

## 2. BẢNG MAP FIELD (BẮT BUỘC — nếu bỏ qua sẽ hiển thị `undefined`)

| FE đang đọc | API trả về |
|---|---|
| `image` | `imageUrl` |
| `rsBadge` (chuỗi `"5.8g RS/khẩu phần"`) | `rsContent` (số `5.8`) + `rsType` (`"RS2"`) |
| `formattedPrice` (`"89.000₫"`) | `price` (số `89000`) — FE tự format |
| `unitInfo` (`"Hũ 250 gram"`) | `unit` (`"hũ"`) + `servingSize` (`"250g"`) |
| `category` (chuỗi) | `category` (**object** `{ id, name, slug, description, sortOrder }`) |
| `subInfo` | `shortDescription` |
| `id: 'prod-01'` (mock) | `id` (**GUID thật**) — dùng cho giỏ hàng & đơn hàng |
| — | `slug` (dùng cho URL chi tiết) |

**Các field FE đang đọc nhưng BE KHÔNG có** (phải để trống hoặc bỏ khỏi UI):
`badge`, `badgeType`, `discount`, `originalPrice`, `rating`, `reviewsCount`, `rsTag`, `discountBadge`, `whyFitText`.
> `whyFitText` có thể thay bằng `message` trong kết quả quiz.

---

## 3. HỢP ĐỒNG DỮ LIỆU CẦN BIẾT

| Quy ước | Giá trị |
|---|---|
| Thành công | `{ "data": {...} }` |
| Danh sách phân trang | `{ "data": [...], "meta": { page, pageSize, total, totalPages } }` |
| Lỗi | `{ "error": { code, message, details } }` |
| Enum | **chuỗi**: `"PENDING"`, `"SCIENCE"`, `"RS2"`, `"DEMO"` |
| Tiền | số nguyên VND |
| Thời gian | ISO 8601 UTC, có hậu tố `Z` |
| Phân trang | `?page=1&pageSize=12` (mặc định 12, tối đa 50) |

**Đã đối chiếu, KHÔNG cần sửa gì thêm:**
- `totalAmount` của BE = `subTotal` (freeship 0đ, VAT đã bao gồm trong giá) → **khớp** với phần tóm tắt đơn đang hiển thị.
- Voucher `BANOVAQUI` / `FREESHIP` của FE đang giảm **0đ** → không xung đột với `totalAmount`, giữ nguyên được.

---

## 4. VIỆC PHÁP LÝ FE PHẢI SỬA (NFR-01)

- [ ] `src/pages/ArticleDetailPage.jsx` (~dòng 584) đang có cụm *"...không thay thế thuốc **điều trị** hạ đường huyết..."* — chứa từ **"điều trị"** nằm trong danh sách **CẤM**.
  → Sửa theo mẫu chuẩn: *"Sản phẩm không phải là thuốc và không có tác dụng thay thế thuốc chữa bệnh."*
- [ ] Đảm bảo `MedicalDisclaimer` có đủ 3 chỗ: **Footer**, **chi tiết sản phẩm**, **cuối bài khoa học**.
- [ ] Không còn từ *"chữa"*, *"điều trị"*, *"thay thuốc"*, *"đặc trị"* ở bất kỳ đâu (trừ câu disclaimer mẫu).

---

## 5. CÒN THIẾU Ở FE (không phải việc của BE)

| Việc | Ghi chú |
|---|---|
| Trang **FAQ** | BE không cần API (nội dung tĩnh) |
| Trang **Liên hệ** | BE **đã có** `POST /api/contact`, chờ FE làm UI |
| **Router thật** (`react-router-dom`) | cần cho slug + F5 + share link + SEO |
| **SEO** meta/OG/sitemap | SPA thuần client-side nên sẽ yếu — ghi rõ trong báo cáo là "chuyển sang CSR do giới hạn thời gian" |
| Zustand + localStorage cho giỏ | hiện `useState` ở `App.jsx` → F5 mất giỏ |

---

## 6. CHECKLIST TRƯỚC DEMO

- [ ] `.env.local` có `VITE_API_BASE_URL`
- [ ] Vercel đã thêm biến môi trường + **Redeploy**
- [ ] `src/services/api.js` + `mappers.js` đã tạo
- [ ] 8 file đã thay mock bằng API
- [ ] Giỏ hàng dùng `id` GUID thật (không còn `prod-01`)
- [ ] `orderCode` lấy từ server (không tự sinh)
- [ ] `VietQR` map về `DEMO`
- [ ] Có loading skeleton + hiển thị lỗi
- [ ] Chạy thử full flow: Trang chủ → Sản phẩm → Chi tiết → Giỏ → Đặt hàng → **thấy mã `BNV-...`**
- [ ] Sửa từ "điều trị" ở `ArticleDetailPage.jsx`
- [ ] Test mobile 375px + desktop 1440px

---

## 7. CÁCH TEST NHANH KHI BE ĐANG CHẠY

```bash
# Terminal BE
cd "E:\Project Exe\Banova.Api"
dotnet run --project src\Banova.Api --urls http://localhost:5252
```

- Giao diện test API: **http://localhost:5252/scalar/v1**
- Test tự động 35 ca: `powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1`

**CORS đã mở sẵn** cho `http://localhost:5173` và `https://banova-virid.vercel.app` — FE gọi trực tiếp được, không cần cấu hình gì thêm.

Nếu gặp lỗi `blocked by CORS policy` → gửi BE domain FE đang chạy để thêm vào `Cors:AllowedOrigins`.

---

## 8. LIÊN HỆ

| Khi nào | Báo ai |
|---|---|
| API trả thiếu field / sai shape so với `API.md` | BE — sửa API, **không tự sửa FE cho khớp** |
| Cần thêm endpoint mới | BE — nhưng phải bổ sung vào `API.md` trước |
| CORS lỗi | BE — thêm origin |
| Lỗi hiển thị / UI | FE |

---

## 9. CẬP NHẬT 21/10 — ADMIN API ĐÃ SẴN SÀNG

Toàn bộ phần admin trước đây hoãn **đã xong** (22/22 endpoint theo `API.md`):

- `POST /api/auth/login` · `POST /api/auth/logout` · `GET /api/auth/me` (cookie `banova_token`)
- `POST/PUT/DELETE /api/admin/products` (+ `GET /api/admin/products` bổ sung cho danh sách)
- `POST/PUT/DELETE /api/admin/articles` (+ `GET /api/admin/articles`)
- `GET /api/orders` · `PATCH /api/orders/{id}/status` · `GET /api/admin/stats`

**FE không cần làm trang admin nữa**: BE đã có 5 trang Razor chạy sẵn tại `/Admin`
(đăng nhập · dashboard · sản phẩm · bài viết · đơn hàng). Tài khoản demo `admin@banova.vn` / `Banova@123`.

Nếu sau này FE muốn tự làm admin SPA thì dùng các endpoint trên; nhớ `fetch` kèm
`credentials: 'include'` để gửi cookie và bật CORS origin tương ứng ở BE.

Các việc P0 còn lại **vẫn thuộc FE** (xem mục 1–5): nối API đúng field, sửa form liên hệ,
bỏ id mock, render `content` bài viết. BE không thay đổi shape của 10 endpoint công khai.
