# Скрипт для резервного копирования истории чата Cursor
# Использование: .\backup_cursor_chat_history.ps1

$cursorDataPath = "$env:APPDATA\Cursor\User\globalStorage"
$backupBasePath = "$env:USERPROFILE\Documents\CursorChatBackups"

# Создаем папку для резервных копий, если её нет
if (-not (Test-Path $backupBasePath)) {
    New-Item -ItemType Directory -Path $backupBasePath -Force | Out-Null
    Write-Host "Создана папка для резервных копий: $backupBasePath" -ForegroundColor Green
}

# Формируем имя файла с датой и временем
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$backupFileName = "cursor_chat_history_$timestamp"
$backupPath = Join-Path $backupBasePath $backupFileName

# Копируем файлы базы данных
$filesToBackup = @(
    "state.vscdb",
    "state.vscdb.backup"
)

# Также копируем папку History (может содержать сохраненные данные)
$historyPath = "$env:APPDATA\Cursor\User\History"

$backedUpFiles = @()

foreach ($file in $filesToBackup) {
    $sourcePath = Join-Path $cursorDataPath $file
    if (Test-Path $sourcePath) {
        $destPath = Join-Path $backupPath $file
        if (-not (Test-Path $backupPath)) {
            New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
        }
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        $backedUpFiles += $file
        Write-Host "Скопирован: $file" -ForegroundColor Green
    } else {
        Write-Host "Файл не найден: $file" -ForegroundColor Yellow
    }
}

# Копируем папку History, если она существует
if (Test-Path $historyPath) {
    $historyBackupPath = Join-Path $backupPath "History"
    Write-Host ""
    Write-Host "Копирую папку History..." -ForegroundColor Yellow
    Copy-Item -Path $historyPath -Destination $historyBackupPath -Recurse -Force
    Write-Host "✓ Папка History скопирована" -ForegroundColor Green
}

if ($backedUpFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "Резервная копия создана успешно!" -ForegroundColor Green
    Write-Host "Путь: $backupPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Скопированные файлы:" -ForegroundColor Yellow
    foreach ($file in $backedUpFiles) {
        Write-Host "  - $file"
    }
    if (Test-Path $historyPath) {
        Write-Host "  - History/ (папка с историей)"
    }
} else {
    Write-Host "Не удалось создать резервную копию - файлы не найдены" -ForegroundColor Red
}

# Показываем список всех резервных копий
Write-Host ""
Write-Host "=== Все резервные копии ===" -ForegroundColor Cyan
Get-ChildItem -Path $backupBasePath -Directory | Sort-Object LastWriteTime -Descending | ForEach-Object {
    Write-Host "$($_.Name) - $($_.LastWriteTime)" -ForegroundColor White
}

