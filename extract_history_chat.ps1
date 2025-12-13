# Скрипт для извлечения и анализа содержимого папки History
# Может содержать сохраненные данные из чата

$historyPath = "$env:APPDATA\Cursor\User\History"
$outputPath = "$env:USERPROFILE\Documents\CursorHistoryExtract"

Write-Host "=== Извлечение данных из папки History ===" -ForegroundColor Cyan
Write-Host ""

# Создаем папку для результатов
if (-not (Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
    Write-Host "Создана папка: $outputPath" -ForegroundColor Green
}

# Ищем все файлы, которые могут содержать данные чата
$chatFiles = @()

# Ищем .md файлы (могут быть экспортированными чатами)
$mdFiles = Get-ChildItem -Path $historyPath -Recurse -Filter "*.md" -ErrorAction SilentlyContinue
Write-Host "Найдено .md файлов: $($mdFiles.Count)" -ForegroundColor Yellow

# Ищем .txt файлы
$txtFiles = Get-ChildItem -Path $historyPath -Recurse -Filter "*.txt" -ErrorAction SilentlyContinue
Write-Host "Найдено .txt файлов: $($txtFiles.Count)" -ForegroundColor Yellow

# Ищем .json файлы (entries.json и другие)
$jsonFiles = Get-ChildItem -Path $historyPath -Recurse -Filter "*.json" -ErrorAction SilentlyContinue
Write-Host "Найдено .json файлов: $($jsonFiles.Count)" -ForegroundColor Yellow

Write-Host ""

# Копируем все потенциально интересные файлы
$allFiles = $mdFiles + $txtFiles + $jsonFiles
$copiedCount = 0

foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Replace($historyPath, "").TrimStart('\')
    $destPath = Join-Path $outputPath $relativePath
    $destDir = Split-Path $destPath -Parent
    
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    Copy-Item -Path $file.FullName -Destination $destPath -Force
    $copiedCount++
}

Write-Host "Скопировано файлов: $copiedCount" -ForegroundColor Green
Write-Host "Результаты сохранены в: $outputPath" -ForegroundColor Cyan
Write-Host ""

# Показываем список .md и .txt файлов с датами
Write-Host "=== Файлы, которые могут содержать данные чата ===" -ForegroundColor Cyan
Write-Host ""

$interestingFiles = $mdFiles + $txtFiles | Sort-Object LastWriteTime -Descending

foreach ($file in $interestingFiles) {
    $relativePath = $file.FullName.Replace($historyPath, "").TrimStart('\')
    $size = [math]::Round($file.Length / 1KB, 2)
    Write-Host "$($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm')) - $size KB - $relativePath" -ForegroundColor White
}

Write-Host ""
Write-Host "💡 Откройте папку $outputPath и проверьте содержимое файлов" -ForegroundColor Yellow
Write-Host "   Особенно обратите внимание на .md и .txt файлы - они могут содержать чаты!" -ForegroundColor Yellow





