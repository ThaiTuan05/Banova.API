<#
.SYNOPSIS
    Smoke test toàn bộ API Banova (10 endpoint phục vụ FE hiện tại).

.DESCRIPTION
    Chạy khi server BE đang bật. Script tự kiểm tra status code + nội dung
    của các ca thành công và ca lỗi (400/404/409), in PASS/FAIL và tổng kết.

.EXAMPLE
    # Terminal 1: chạy server
    dotnet run --project src\Banova.Api --urls http://localhost:5252

    # Terminal 2: chạy test
    powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1

.EXAMPLE
    # Test bản đã deploy
    powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1 -BaseUrl "https://your-be.azurewebsites.net"
#>
param(
    [string]$BaseUrl = "http://localhost:5252"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$script:Pass = 0
$script:Fail = 0

function Write-Result {
    param([string]$Name, [bool]$Ok, [string]$Detail = "")
    if ($Ok) {
        $script:Pass++
        Write-Host "  [PASS] $Name" -ForegroundColor Green
    }
    else {
        $script:Fail++
        Write-Host "  [FAIL] $Name  $Detail" -ForegroundColor Red
    }
}

function Invoke-ApiTest {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [string]$Body,
        [int]$Expected
    )

    $url = "$BaseUrl$Path"
    $status = 0
    $content = ""

    try {
        $params = @{ Uri = $url; Method = $Method; UseBasicParsing = $true; TimeoutSec = 30 }
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json; charset=utf-8"
        }
        $response = Invoke-WebRequest @params
        $status = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $status = [int]$resp.StatusCode
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $content = $reader.ReadToEnd()
            $reader.Close()
        }
        else {
            Write-Result -Name $Name -Ok $false -Detail ("Lỗi kết nối: " + $_.Exception.Message)
            return $null
        }
    }

    $ok = ($status -eq $Expected)
    $preview = if ($content.Length -gt 150) { $content.Substring(0, 150) + "..." } else { $content }
    Write-Result -Name $Name -Ok $ok -Detail "(nhận HTTP $status, mong đợi $Expected) $preview"
    return $content
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " BANOVA API - SMOKE TEST" -ForegroundColor Cyan
Write-Host " Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# ---------- 0. Server còn sống? ----------
try {
    Invoke-WebRequest -Uri "$BaseUrl/api/categories" -UseBasicParsing -TimeoutSec 10 | Out-Null
}
catch {
    Write-Host ""
    Write-Host "KHONG KET NOI DUOC TOI $BaseUrl" -ForegroundColor Red
    Write-Host "Kiem tra:" -ForegroundColor Yellow
    Write-Host "  1. Da chay server chua? dotnet run --project src\Banova.Api --urls http://localhost:5252"
    Write-Host "  2. Port co bi chiem khong? netstat -ano | findstr 5252"
    Write-Host ""
    exit 1
}

# ---------- 1. Danh mục & sản phẩm ----------
Write-Host "`n[1] Danh muc & San pham" -ForegroundColor Cyan

$catJson = Invoke-ApiTest -Name "GET /api/categories" -Method GET -Path "/api/categories" -Expected 200
if ($catJson) {
    $cats = ($catJson | ConvertFrom-Json).data
    Write-Result -Name "  -> có đủ 4 danh mục" -Ok ($cats.Count -eq 4) -Detail ("nhận " + $cats.Count)
}

$prodJson = Invoke-ApiTest -Name "GET /api/products" -Method GET -Path "/api/products?pageSize=50" -Expected 200
$script:ProductList = @()
if ($prodJson) {
    $obj = $prodJson | ConvertFrom-Json
    $script:ProductList = $obj.data
    Write-Result -Name "  -> có meta phân trang" -Ok ($null -ne $obj.meta -and $obj.meta.totalPages -ge 1)
    Write-Result -Name "  -> có 8 sản phẩm" -Ok ($obj.meta.total -eq 8) -Detail ("nhận " + $obj.meta.total)
}

Invoke-ApiTest -Name "GET /api/products?category=banh" -Method GET -Path "/api/products?category=banh" -Expected 200 | Out-Null
Invoke-ApiTest -Name "GET /api/products?sort=price_asc" -Method GET -Path "/api/products?sort=price_asc&pageSize=3" -Expected 200 | Out-Null
Invoke-ApiTest -Name "GET /api/products/{slug}" -Method GET -Path "/api/products/bot-tinh-bot-khang-banova-250g" -Expected 200 | Out-Null
Invoke-ApiTest -Name "GET /api/products/khong-ton-tai (404)" -Method GET -Path "/api/products/khong-ton-tai" -Expected 404 | Out-Null

# ---------- 2. Bài viết ----------
Write-Host "`n[2] Goc khoa hoc / Blog" -ForegroundColor Cyan

$sciJson = Invoke-ApiTest -Name "GET /api/articles?type=SCIENCE" -Method GET -Path "/api/articles?type=SCIENCE" -Expected 200
if ($sciJson) {
    $sci = ($sciJson | ConvertFrom-Json).data
    Write-Result -Name "  -> có 3 bài SCIENCE" -Ok ($sci.Count -eq 3) -Detail ("nhận " + $sci.Count)
    $missingRefs = @($sci | Where-Object { [string]::IsNullOrWhiteSpace($_.references) })
    Write-Result -Name "  -> mọi bài SCIENCE đều có references" -Ok ($missingRefs.Count -eq 0) -Detail ("thiếu " + $missingRefs.Count)
}
Invoke-ApiTest -Name "GET /api/articles?type=BLOG" -Method GET -Path "/api/articles?type=BLOG" -Expected 200 | Out-Null
Invoke-ApiTest -Name "GET /api/articles/{slug}" -Method GET -Path "/api/articles/tinh-bot-khang-la-gi-va-vi-sao-tot-cho-duong-huyet" -Expected 200 | Out-Null

# ---------- 3. Quiz ----------
Write-Host "`n[3] Quiz tu van" -ForegroundColor Cyan

$quizObjectBody = '{"answers":{"goal":"blood-sugar","morning_routine":"smoothie","rs_experience":"new","sensitivity":"normal"}}'
$quiz1 = Invoke-ApiTest -Name "POST /api/quiz/submit (object - FE hien tai)" -Method POST -Path "/api/quiz/submit" -Body $quizObjectBody -Expected 200
if ($quiz1) {
    $q = ($quiz1 | ConvertFrom-Json).data
    Write-Result -Name "  -> có score + recommendedProduct" -Ok ($q.score -ge 0 -and $null -ne $q.recommendedProduct.id)
}

$quizArrayBody = '{"answers":[{"questionId":"goal","value":"weight-control"},{"questionId":"morning_routine","value":"bakery"}]}'
$quiz2 = Invoke-ApiTest -Name "POST /api/quiz/submit (array - API.md)" -Method POST -Path "/api/quiz/submit" -Body $quizArrayBody -Expected 200

# ---------- 4. Newsletter ----------
Write-Host "`n[4] Dang ky nhan tin" -ForegroundColor Cyan

$randomEmail = "test-" + [guid]::NewGuid().ToString("N").Substring(0, 10) + "@banova.test"
Invoke-ApiTest -Name "POST /api/newsletter (lan 1 -> 201)" -Method POST -Path "/api/newsletter" -Body ('{"email":"' + $randomEmail + '"}') -Expected 201 | Out-Null
Invoke-ApiTest -Name "POST /api/newsletter (trung -> 409)" -Method POST -Path "/api/newsletter" -Body ('{"email":"' + $randomEmail + '"}') -Expected 409 | Out-Null
Invoke-ApiTest -Name "POST /api/newsletter (email sai -> 400)" -Method POST -Path "/api/newsletter" -Body '{"email":"khong-phai-email"}' -Expected 400 | Out-Null

# ---------- 5. Liên hệ ----------
Write-Host "`n[5] Lien he" -ForegroundColor Cyan

Invoke-ApiTest -Name "POST /api/contact (thieu du lieu -> 400)" -Method POST -Path "/api/contact" -Body '{"name":"","email":"abc","phone":"","message":""}' -Expected 400 | Out-Null
$contactBody = '{"name":"Kiem Thu Tu Dong","email":"tester@banova.test","phone":"0905123456","message":"Tin nhan kiem thu tu dong"}'
Invoke-ApiTest -Name "POST /api/contact (hop le -> 201)" -Method POST -Path "/api/contact" -Body $contactBody -Expected 201 | Out-Null

# ---------- 6. Đặt hàng ----------
Write-Host "`n[6] Dat hang mo phong" -ForegroundColor Cyan

$p250 = $script:ProductList | Where-Object { $_.slug -eq "bot-tinh-bot-khang-banova-250g" } | Select-Object -First 1
$pGift = $script:ProductList | Where-Object { $_.slug -eq "hop-qua-quy-nhon-healthy" } | Select-Object -First 1

if (-not $p250 -or -not $pGift) {
    Write-Result -Name "  -> khong tim thay san pham de test dat hang" -Ok $false
}
else {
    $stockBefore = $p250.stock

    $orderBody = @{
        customerName  = "Kiem Thu Tu Dong"
        phone         = "0905123456"
        email         = "tester@banova.test"
        address       = "123 Le Loi, Quy Nhon"
        note          = "Don test tu dong"
        paymentMethod = "DEMO"
        items         = @(@{ productId = $p250.id; quantity = 2 })
    } | ConvertTo-Json -Depth 5

    $orderJson = Invoke-ApiTest -Name "POST /api/orders (hop le -> 201)" -Method POST -Path "/api/orders" -Body $orderBody -Expected 201
    $orderCode = $null
    if ($orderJson) {
        $order = ($orderJson | ConvertFrom-Json).data
        $orderCode = $order.orderCode
        Write-Result -Name "  -> ma don dung dinh dang BNV-yyyyMMdd-XXX" -Ok ($orderCode -match '^BNV-\d{8}-\d{3}$') -Detail ("nhận '" + $orderCode + "'")
        Write-Result -Name "  -> server tu tinh tien (2 x 89000 = 178000)" -Ok ($order.subTotal -eq 178000) -Detail ("nhận " + $order.subTotal)
        Write-Result -Name "  -> trang thai ban dau PENDING" -Ok ($order.status -eq "PENDING")
        Write-Result -Name "  -> co snapshot productName + unitPrice" -Ok ($order.items[0].unitPrice -eq 89000 -and -not [string]::IsNullOrWhiteSpace($order.items[0].productName))
    }

    if ($orderCode) {
        Invoke-ApiTest -Name "GET /api/orders/{orderCode}" -Method GET -Path "/api/orders/$orderCode" -Expected 200 | Out-Null
    }
    Invoke-ApiTest -Name "GET /api/orders/ma-sai (404)" -Method GET -Path "/api/orders/BNV-99999999-999" -Expected 404 | Out-Null

    # Trừ kho
    $prodAfter = (Invoke-RestMethod "$BaseUrl/api/products?pageSize=50").data | Where-Object { $_.slug -eq "bot-tinh-bot-khang-banova-250g" } | Select-Object -First 1
    Write-Result -Name "  -> da tru ton kho (2 san pham)" -Ok ($prodAfter.stock -eq ($stockBefore - 2)) -Detail ("trước $stockBefore, sau " + $prodAfter.stock)

    # Ca lỗi
    $badProductBody = @{
        customerName = "Kiem Thu"; phone = "0905123456"; address = "1 ABC"
        items = @(@{ productId = "11111111-1111-1111-1111-111111111111"; quantity = 1 })
    } | ConvertTo-Json -Depth 5
    Invoke-ApiTest -Name "POST /api/orders (SP khong ton tai -> 404)" -Method POST -Path "/api/orders" -Body $badProductBody -Expected 404 | Out-Null

    $overStockBody = @{
        customerName = "Kiem Thu"; phone = "0905123456"; address = "1 ABC"
        items = @(@{ productId = $pGift.id; quantity = ($pGift.stock + 1000) })
    } | ConvertTo-Json -Depth 5
    Invoke-ApiTest -Name "POST /api/orders (vuot ton kho -> 409)" -Method POST -Path "/api/orders" -Body $overStockBody -Expected 409 | Out-Null

    $missingBody = '{ "customerName":"", "phone":"0905123456", "address":"", "items":[{"productId":"' + $p250.id + '","quantity":1}] }'
    Invoke-ApiTest -Name "POST /api/orders (thieu ho ten/dia chi -> 400)" -Method POST -Path "/api/orders" -Body $missingBody -Expected 400 | Out-Null

    $badPayBody = '{ "customerName":"Kiem Thu", "phone":"0905123456", "address":"1 ABC", "paymentMethod":"VNPAY", "items":[{"productId":"' + $p250.id + '","quantity":1}] }'
    Invoke-ApiTest -Name "POST /api/orders (payment sai -> 400)" -Method POST -Path "/api/orders" -Body $badPayBody -Expected 400 | Out-Null
}

# ---------- 7. CORS ----------
Write-Host "`n[7] CORS cho FE (Vercel)" -ForegroundColor Cyan

$corsOk = $false
try {
    $corsResp = Invoke-WebRequest -Uri "$BaseUrl/api/products" -Method OPTIONS -UseBasicParsing `
        -Headers @{ "Origin" = "https://banova-virid.vercel.app"; "Access-Control-Request-Method" = "GET" }
    $corsOk = ($corsResp.StatusCode -eq 204) -and ($corsResp.Headers["Access-Control-Allow-Origin"] -eq "https://banova-virid.vercel.app")
}
catch {
    $corsOk = $false
}
Write-Result -Name "OPTIONS preflight tra Access-Control-Allow-Origin" -Ok $corsOk

# ---------- Tổng kết ----------
Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
$total = $script:Pass + $script:Fail
if ($script:Fail -eq 0) {
    Write-Host " KET QUA: $($script:Pass)/$total PASS  -  API SAN SANG" -ForegroundColor Green
}
else {
    Write-Host " KET QUA: $($script:Pass)/$total PASS  -  $($script:Fail) FAIL" -ForegroundColor Red
}
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

if ($script:Fail -gt 0) { exit 1 }
