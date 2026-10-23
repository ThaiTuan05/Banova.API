# ============================================================
# Banova API — smoke test cho phần A1 (Auth + Admin)
# Chạy sau khi server đã khởi động:
#   powershell -ExecutionPolicy Bypass -File .\scripts\smoke-admin.ps1
# ============================================================
param(
    [string]$BaseUrl = "http://localhost:5252",
    [string]$Email = "admin@banova.vn",
    [string]$Password = "Banova@123"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$script:pass = 0
$script:fail = 0
$stamp = (Get-Date -Format "MMddHHmmss")

function Write-Case([string]$name, [bool]$ok, [string]$detail = "") {
    if ($ok) {
        $script:pass++
        Write-Host "  [PASS] $name" -ForegroundColor Green
    } else {
        $script:fail++
        Write-Host "  [FAIL] $name $detail" -ForegroundColor Red
    }
}

function Invoke-Api {
    param(
        [string]$Method = "GET",
        [string]$Path,
        $Body = $null,
        $Session = $null,
        [switch]$NoRedirect
    )

    $uri = "$BaseUrl$Path"
    $params = @{
        Uri                = $uri
        Method             = $Method
        UseBasicParsing    = $true
        TimeoutSec         = 20
        MaximumRedirection = if ($NoRedirect) { 0 } else { 5 }
    }
    if ($Session) { $params.WebSession = $Session }
    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress)
        $params.ContentType = "application/json"
    }

    try {
        $response = Invoke-WebRequest @params
        return [pscustomobject]@{
            Status  = [int]$response.StatusCode
            Content = $response.Content
            Json    = try { $response.Content | ConvertFrom-Json } catch { $null }
        }
    }
    catch {
        $resp = $_.Exception.Response
        if ($null -ne $resp) {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $text = $reader.ReadToEnd()
            return [pscustomobject]@{
                Status  = [int]$resp.StatusCode
                Content = $text
                Json    = try { $text | ConvertFrom-Json } catch { $null }
            }
        }
        return [pscustomobject]@{ Status = 0; Content = $_.Exception.Message; Json = $null }
    }
}

function Invoke-StatusOnly {
    param([string]$Path)

    # Không follow redirect để đọc đúng mã 302 khi chưa đăng nhập
    $request = [System.Net.HttpWebRequest]::Create("$BaseUrl$Path")
    $request.Method = "GET"
    $request.AllowAutoRedirect = $false
    $request.Timeout = 15000

    try {
        $response = $request.GetResponse()
        try { return [int]$response.StatusCode } finally { $response.Close() }
    }
    catch [System.Net.WebException] {
        if ($null -ne $_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
        return 0
    }
}

Write-Host "== 0. Cho server san sang ==" -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    $health = Invoke-Api -Path "/health"
    if ($health.Status -eq 200) { $ready = $true; break }
    Start-Sleep -Seconds 2
}
Write-Case "GET /health = 200" ($health.Status -eq 200) "status=$($health.Status)"
if (-not $ready) { Write-Host "Server chua san sang." -ForegroundColor Red; exit 1 }

$healthJson = $health.Content | ConvertFrom-Json
Write-Case "health.database = true" ($healthJson.database -eq $true)

Write-Host "`n== 1. Auth (API.md muc 6) ==" -ForegroundColor Cyan

$anon = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$me401 = Invoke-Api -Path "/api/auth/me" -Session $anon
Write-Case "GET /api/auth/me (chua login) = 401" ($me401.Status -eq 401) "status=$($me401.Status)"
Write-Case "401 dung format error.code=UNAUTHORIZED" ($me401.Json.error.code -eq "UNAUTHORIZED") "code=$($me401.Json.error.code)"

$admin401 = Invoke-Api -Method POST -Path "/api/admin/products" -Body @{ name = "x" } -Session $anon
Write-Case "POST /api/admin/products (chua login) = 401" ($admin401.Status -eq 401) "status=$($admin401.Status)"

$login = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ email = $Email; password = $Password } -Session $anon
Write-Case "POST /api/auth/login = 200" ($login.Status -eq 200) "status=$($login.Status) body=$($login.Content)"
Write-Case "login tra role ADMIN" ($login.Json.data.role -eq "ADMIN") "role=$($login.Json.data.role)"
$cookie = $anon.Cookies.GetCookies($BaseUrl)["banova_token"]
Write-Case "co cookie banova_token" ($null -ne $cookie)

$me = Invoke-Api -Path "/api/auth/me" -Session $anon
Write-Case "GET /api/auth/me (da login) = 200" ($me.Status -eq 200) "status=$($me.Status)"
Write-Case "me.email dung" ($me.Json.data.email -eq $Email)

$badLogin = Invoke-Api -Method POST -Path "/api/auth/login" -Body @{ email = $Email; password = "sai-mat-khau" }
Write-Case "login sai mat khau = 401" ($badLogin.Status -eq 401) "status=$($badLogin.Status)"

Write-Host "`n== 2. Admin san pham (API.md muc 7) ==" -ForegroundColor Cyan

$categories = Invoke-Api -Path "/api/categories"
$categoryId = $categories.Json.data[0].id
Write-Case "GET /api/categories = 200" ($categories.Status -eq 200)

$productSlug = "sp-smoke-$stamp"
$createProduct = Invoke-Api -Method POST -Path "/api/admin/products" -Session $anon -Body @{
    name         = "San pham smoke $stamp"
    slug         = $productSlug
    description  = "San pham tao boi smoke test"
    categoryId   = $categoryId
    price        = 123000
    unit         = "hu"
    servingSize  = "15g"
    rsContent    = 5.5
    rsType       = "RS2"
    stock        = 7
    isActive     = $true
}
Write-Case "POST /api/admin/products = 201" ($createProduct.Status -eq 201) "status=$($createProduct.Status) body=$($createProduct.Content)"
$productId = $createProduct.Json.data.id
Write-Case "san pham moi tra GUID" ([guid]::TryParse("$productId", [ref]([guid]::Empty)))

$dupProduct = Invoke-Api -Method POST -Path "/api/admin/products" -Session $anon -Body @{
    name = "Trung slug"; slug = $productSlug; description = "x"; categoryId = $categoryId
    price = 1000; unit = "hu"; servingSize = "15g"; rsContent = 1; rsType = "RS2"; stock = 1
}
Write-Case "tao trung slug = 409 CONFLICT" ($dupProduct.Status -eq 409) "status=$($dupProduct.Status)"

$updateProduct = Invoke-Api -Method PUT -Path "/api/admin/products/$productId" -Session $anon -Body @{ price = 99000; stock = 12 }
Write-Case "PUT /api/admin/products/{id} = 200" ($updateProduct.Status -eq 200) "status=$($updateProduct.Status)"
Write-Case "PUT cap nhat gia + giu ten cu" ($updateProduct.Json.data.price -eq 99000 -and $updateProduct.Json.data.name -eq "San pham smoke $stamp")

$adminList = Invoke-Api -Path "/api/admin/products?q=smoke&pageSize=50" -Session $anon
Write-Case "GET /api/admin/products = 200" ($adminList.Status -eq 200) "status=$($adminList.Status)"
Write-Case "admin list thay san pham moi" ($adminList.Json.data.Count -ge 1)

Write-Host "`n== 3. Admin bai viet (API.md muc 8) ==" -ForegroundColor Cyan

$articleSlug = "bai-smoke-$stamp"
$scienceNoRef = Invoke-Api -Method POST -Path "/api/admin/articles" -Session $anon -Body @{
    title = "Bai thieu nguon"; slug = "bai-thieu-nguon-$stamp"; content = "<p>x</p>"; type = "SCIENCE"; status = "DRAFT"
}
Write-Case "SCIENCE thieu references = 400" ($scienceNoRef.Status -eq 400) "status=$($scienceNoRef.Status)"

$createArticle = Invoke-Api -Method POST -Path "/api/admin/articles" -Session $anon -Body @{
    title      = "Bai smoke $stamp"
    slug       = $articleSlug
    excerpt    = "Tom tat"
    content    = "<p>Noi dung bai smoke</p>"
    type       = "SCIENCE"
    status     = "PUBLISHED"
    references = "1. Birt DF, et al. (2013)."
    author     = "Smoke Test"
}
Write-Case "POST /api/admin/articles = 201" ($createArticle.Status -eq 201) "status=$($createArticle.Status) body=$($createArticle.Content)"
$articleId = $createArticle.Json.data.id
Write-Case "PUBLISHED tu set publishedAt" ($null -ne $createArticle.Json.data.publishedAt)

$publicArticles = Invoke-Api -Path "/api/articles?pageSize=50"
$found = @($publicArticles.Json.data | Where-Object { $_.slug -eq $articleSlug })
Write-Case "bai vua dang hien o API cong khai" ($found.Count -eq 1)

$updateArticle = Invoke-Api -Method PUT -Path "/api/admin/articles/$articleId" -Session $anon -Body @{ status = "DRAFT"; title = "Bai smoke da sua $stamp" }
Write-Case "PUT /api/admin/articles/{id} = 200" ($updateArticle.Status -eq 200) "status=$($updateArticle.Status)"
Write-Case "doi sang DRAFT" ($updateArticle.Json.data.status -eq "DRAFT")

$publicAfterDraft = Invoke-Api -Path "/api/articles?pageSize=50"
$foundDraft = @($publicAfterDraft.Json.data | Where-Object { $_.slug -eq $articleSlug })
Write-Case "bai DRAFT an khoi API cong khai" ($foundDraft.Count -eq 0)

Write-Host "`n== 4. Don hang admin (API.md muc 9) ==" -ForegroundColor Cyan

$products = Invoke-Api -Path "/api/products?pageSize=1"
$orderProductId = $products.Json.data[0].id
$stockBefore = $products.Json.data[0].stock

$createOrder = Invoke-Api -Method POST -Path "/api/orders" -Body @{
    customerName  = "Khach Smoke $stamp"
    phone         = "0905123456"
    address       = "123 Le Loi, Quy Nhon"
    paymentMethod = "DEMO"
    items         = @(@{ productId = $orderProductId; quantity = 2 })
}
Write-Case "POST /api/orders (public) = 201" ($createOrder.Status -eq 201) "status=$($createOrder.Status) body=$($createOrder.Content)"
$orderId = $createOrder.Json.data.id
$orderCode = $createOrder.Json.data.orderCode
Write-Case "orderCode dung format BNV-yyyyMMdd-XXX" ($orderCode -match '^BNV-\d{8}-\d{3}$')

$orders401 = Invoke-Api -Path "/api/orders"
Write-Case "GET /api/orders (chua login) = 401" ($orders401.Status -eq 401) "status=$($orders401.Status)"

$orders = Invoke-Api -Path "/api/orders?pageSize=50" -Session $anon
Write-Case "GET /api/orders (admin) = 200" ($orders.Status -eq 200) "status=$($orders.Status)"
Write-Case "meta phan trang co total" ($orders.Json.meta.total -ge 1)

$confirm = Invoke-Api -Method PATCH -Path "/api/orders/$orderId/status" -Session $anon -Body @{ status = "CONFIRMED" }
Write-Case "PATCH status = CONFIRMED" ($confirm.Status -eq 200 -and $confirm.Json.data.status -eq "CONFIRMED") "status=$($confirm.Status)"
Write-Case "statusLabel tieng Viet" ($confirm.Json.data.statusLabel -eq "Đã xác nhận") "label=$($confirm.Json.data.statusLabel)"

$cancel = Invoke-Api -Method PATCH -Path "/api/orders/$orderId/status" -Session $anon -Body @{ status = "CANCELLED" }
Write-Case "PATCH status = CANCELLED" ($cancel.Status -eq 200 -and $cancel.Json.data.status -eq "CANCELLED") "status=$($cancel.Status)"

$productAfterCancel = Invoke-Api -Path "/api/products/$($products.Json.data[0].slug)"
Write-Case "huy don hoan lai ton kho" ($productAfterCancel.Json.data.stock -eq $stockBefore) "truoc=$stockBefore sau=$($productAfterCancel.Json.data.stock)"

$badStatus = Invoke-Api -Method PATCH -Path "/api/orders/$orderId/status" -Session $anon -Body @{ status = "DANG_GIAO" }
Write-Case "status sai = 400 VALIDATION" ($badStatus.Status -eq 400 -and $badStatus.Json.error.code -eq "VALIDATION") "status=$($badStatus.Status)"

$stats = Invoke-Api -Path "/api/admin/stats" -Session $anon
Write-Case "GET /api/admin/stats = 200" ($stats.Status -eq 200) "status=$($stats.Status)"
Write-Case "stats co du 5 chi so" ($null -ne $stats.Json.data.totalOrders -and $null -ne $stats.Json.data.pendingOrders -and $null -ne $stats.Json.data.totalProducts -and $null -ne $stats.Json.data.totalArticles -and $null -ne $stats.Json.data.totalSubscribers)

Write-Host "`n== 5. Trang quan tri Razor ==" -ForegroundColor Cyan

$loginPage = Invoke-Api -Path "/Admin/dang-nhap"
Write-Case "GET /Admin/dang-nhap (anon) = 200" ($loginPage.Status -eq 200) "status=$($loginPage.Status)"

$adminAnonStatus = Invoke-StatusOnly -Path "/Admin"
Write-Case "GET /Admin (anon) bi chan (302/401)" ($adminAnonStatus -eq 302 -or $adminAnonStatus -eq 401) "status=$adminAnonStatus"

$dashboard = Invoke-Api -Path "/Admin" -Session $anon
Write-Case "GET /Admin (co cookie) = 200" ($dashboard.Status -eq 200) "status=$($dashboard.Status)"
Write-Case "dashboard hien thi noi dung" ($dashboard.Content -match "Tổng quan")

foreach ($page in @("/Admin/san-pham", "/Admin/bai-viet", "/Admin/don-hang")) {
    $result = Invoke-Api -Path $page -Session $anon
    Write-Case "GET $page = 200" ($result.Status -eq 200) "status=$($result.Status)"
}

Write-Host "`n== 6. Xoa mem + dang xuat ==" -ForegroundColor Cyan

$deleteProduct = Invoke-Api -Method DELETE -Path "/api/admin/products/$productId" -Session $anon
Write-Case "DELETE /api/admin/products/{id} = 200" ($deleteProduct.Status -eq 200) "status=$($deleteProduct.Status)"

$deletedDetail = Invoke-Api -Path "/api/products/$productSlug"
Write-Case "san pham da xoa khong con o API cong khai" ($deletedDetail.Status -eq 404) "status=$($deletedDetail.Status)"

$deleteArticle = Invoke-Api -Method DELETE -Path "/api/admin/articles/$articleId" -Session $anon
Write-Case "DELETE /api/admin/articles/{id} = 200" ($deleteArticle.Status -eq 200) "status=$($deleteArticle.Status)"

$logout = Invoke-Api -Method POST -Path "/api/auth/logout" -Session $anon
Write-Case "POST /api/auth/logout = 200" ($logout.Status -eq 200) "status=$($logout.Status)"

$meAfterLogout = Invoke-Api -Path "/api/auth/me" -Session $anon
Write-Case "sau logout /api/auth/me = 401" ($meAfterLogout.Status -eq 401) "status=$($meAfterLogout.Status)"

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
if ($script:fail -eq 0) {
    Write-Host "KET QUA: $($script:pass)/$($script:pass) PASS - AUTH + ADMIN SAN SANG" -ForegroundColor Green
    exit 0
} else {
    Write-Host "KET QUA: $($script:pass) PASS / $($script:fail) FAIL" -ForegroundColor Red
    exit 1
}
