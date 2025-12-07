# Скрипт для восстановления истории чата Cursor из резервной копии
# Использование: .\restore_cursor_chat_history.ps1 [путь_к_резервной_копии]

param(
    [string]$BackupPath = ""
)

$cursorDataPath = "$env:APPDATA\Cursor\User\globalStorage"
$backupBasePath = "$env:USERPROFILE\Documents\CursorChatBackups"

# Если путь не указан, показываем список доступных резервных копий
if ([string]::IsNullOrEmpty($BackupPath)) {
    Write-Host "=== Доступные резервные копии ===" -ForegroundColor Cyan
    Write-Host ""
    
    $backups = Get-ChildItem -Path $backupBasePath -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
    
    if ($backups.Count -eq 0) {
        Write-Host "Резервные копии не найдены в: $backupBasePath" -ForegroundColor Red
        exit
    }
    
    $index = 1
    foreach ($backup in $backups) {
        Write-Host "$index. $($backup.Name) - $($backup.LastWriteTime)" -ForegroundColor White
        $index++
    }
    
    Write-Host ""
    $choice = Read-Host "Введите номер резервной копии для восстановления (или 'q' для выхода)"
    
    if ($choice -eq 'q' -or $choice -eq 'Q') {
        exit
    }
    
    try {
        $selectedBackup = $backups[[int]$choice - 1]
        $BackupPath = $selectedBackup.FullName
    } catch {
        Write-Host "Неверный выбор!" -ForegroundColor Red
        exit
    }
}

# Проверяем существование резервной копии
if (-not (Test-Path $BackupPath)) {
    Write-Host "Резервная копия не найдена: $BackupPath" -ForegroundColor Red
    exit
}

# Проверяем, что Cursor закрыт
$cursorProcesses = Get-Process -Name "Cursor" -ErrorAction SilentlyContinue
if ($cursorProcesses) {
    Write-Host "ВНИМАНИЕ: Cursor запущен! Закройте Cursor перед восстановлением." -ForegroundColor Red
    Write-Host "Запущенные процессы Cursor:" -ForegroundColor Yellow
    $cursorProcesses | ForEach-Object { Write-Host "  PID: $($_.Id) - $($_.ProcessName)" }
    $confirm = Read-Host "Продолжить восстановление? (y/n)"
    if ($confirm -ne 'y' -and $confirm -ne 'Y') {
        exit
    }
}

# Создаем резервную копию текущих файлов перед восстановлением
Write-Host ""
Write-Host "Создаю резервную копию текущих файлов..." -ForegroundColor Yellow
$currentBackupPath = Join-Path $backupBasePath "before_restore_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')"
New-Item -ItemType Directory -Path $currentBackupPath -Force | Out-Null

$filesToRestore = @("state.vscdb", "state.vscdb.backup")
foreach ($file in $filesToRestore) {
    $sourcePath = Join-Path $cursorDataPath $file
    if (Test-Path $sourcePath) {
        $destPath = Join-Path $currentBackupPath $file
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "  Сохранен: $file" -ForegroundColor Green
    }
}

# Восстанавливаем файлы из резервной копии
Write-Host ""
Write-Host "Восстанавливаю файлы из резервной копии..." -ForegroundColor Yellow

foreach ($file in $filesToRestore) {
    $sourcePath = Join-Path $BackupPath $file
    if (Test-Path $sourcePath) {
        $destPath = Join-Path $cursorDataPath $file
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "  Восстановлен: $file" -ForegroundColor Green
    } else {
        Write-Host "  Файл не найден в резервной копии: $file" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Восстановление завершено!" -ForegroundColor Green
Write-Host "Перезапустите Cursor, чтобы увидеть восстановленную историю чата." -ForegroundColor Cyan


