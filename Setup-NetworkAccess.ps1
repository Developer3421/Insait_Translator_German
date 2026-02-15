# Insait Translator - Налаштування мережевого доступу
# Цей скрипт додає правила брандмауера та URL ACL резервації для доступу з локальної мережі
# Запускайте як адміністратор!

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Insait Translator - Network Setup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Перевірка прав адміністратора
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ПОМИЛКА: Цей скрипт потрібно запускати як адміністратор!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Клацніть правою кнопкою миші на скрипті та виберіть 'Запустити як адміністратор'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Натисніть будь-яку клавішу для виходу..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# Отримуємо поточного користувача
$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name

Write-Host "Поточний користувач: $currentUser" -ForegroundColor Gray
Write-Host ""

# ============================================
# Крок 1: URL ACL резервації для HTTP.sys
# ============================================
Write-Host "Крок 1: Налаштування URL ACL резервацій..." -ForegroundColor Yellow
Write-Host ""

# Видалення старих резервацій, якщо вони існують
$urlsToReserve = @(
    "http://+:4200/",
    "http://+:4201/",
    "http://*:4200/",
    "http://*:4201/"
)

foreach ($url in $urlsToReserve) {
    Write-Host "Видалення існуючої резервації: $url" -ForegroundColor Gray
    netsh http delete urlacl url=$url 2>$null | Out-Null
}

# Додавання нових резервацій (для всіх користувачів)
$reservations = @(
    @{Url = "http://+:4200/"; Description = "Backend API"},
    @{Url = "http://+:4201/"; Description = "React UI Dev Server"}
)

foreach ($res in $reservations) {
    try {
        # Використовуємо "Everyone" для Windows англійською або "Всі" для української
        # sddl=D:(A;;GX;;;WD) - дозволяє всім користувачам
        $result = netsh http add urlacl url=$($res.Url) sddl=D:(A;;GX;;;WD) 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] URL ACL для $($res.Description) ($($res.Url)) додано для всіх користувачів" -ForegroundColor Green
        } else {
            Write-Host "[УВАГА] $result" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "[ПОМИЛКА] Не вдалося додати URL ACL для $($res.Url): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# ============================================
# Крок 2: Правила брандмауера
# ============================================
Write-Host "Крок 2: Додавання правил брандмауера..." -ForegroundColor Yellow
Write-Host ""

# Видалення старих правил, якщо вони існують
$existingRules = Get-NetFirewallRule -DisplayName "Insait Translator*" -ErrorAction SilentlyContinue
if ($existingRules) {
    Write-Host "Видалення існуючих правил..." -ForegroundColor Gray
    Remove-NetFirewallRule -DisplayName "Insait Translator*" -ErrorAction SilentlyContinue
}

# Додавання правила для Backend (порт 4200)
try {
    New-NetFirewallRule `
        -DisplayName "Insait Translator Backend" `
        -Description "Дозволяє доступ до Insait Translator Backend API з локальної мережі" `
        -Direction Inbound `
        -LocalPort 4200 `
        -Protocol TCP `
        -Action Allow `
        -Profile Domain,Private,Public `
        -Enabled True | Out-Null
    
    Write-Host "[OK] Порт 4200 (Backend API) відкрито" -ForegroundColor Green
} catch {
    Write-Host "[ПОМИЛКА] Не вдалося відкрити порт 4200: $($_.Exception.Message)" -ForegroundColor Red
}

# Додавання правила для React UI (порт 4201)
try {
    New-NetFirewallRule `
        -DisplayName "Insait Translator React UI" `
        -Description "Дозволяє доступ до Insait Translator React інтерфейсу з локальної мережі" `
        -Direction Inbound `
        -LocalPort 4201 `
        -Protocol TCP `
        -Action Allow `
        -Profile Domain,Private,Public `
        -Enabled True | Out-Null
    
    Write-Host "[OK] Порт 4201 (React UI) відкрито" -ForegroundColor Green
} catch {
    Write-Host "[ПОМИЛКА] Не вдалося відкрити порт 4201: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Налаштування завершено!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Показати IP адресу
Write-Host "Ваші IP адреси:" -ForegroundColor Yellow
$ipAddresses = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -ne "127.0.0.1" -and $_.IPAddress -notlike "169.*" } | Select-Object -ExpandProperty IPAddress
foreach ($ip in $ipAddresses) {
    Write-Host "  React UI: http://${ip}:4201" -ForegroundColor Cyan
    Write-Host "  Backend API: http://${ip}:4200" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Інші користувачі в мережі можуть відкрити React UI за адресою вище" -ForegroundColor White
Write-Host ""
Write-Host "Натисніть будь-яку клавішу для виходу..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

