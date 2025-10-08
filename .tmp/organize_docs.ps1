# docsフォルダ整理スクリプト

# 古いレポートをarchive化
Get-ChildItem -Path "docs\*.md" | Where-Object {
    $name = $_.Name
    -not (Test-Path "docs\architecture\$name") -and
    -not (Test-Path "docs\guides\$name") -and
    -not (Test-Path "docs\reports\$name") -and
    -not (Test-Path "docs\rule\$name") -and
    $name -ne "README.md"
} | ForEach-Object {
    Move-Item $_.FullName "docs\archive\2025-09\" -Force
    Write-Host "Moved: $($_.Name)"
}

# txtファイルをarchive化
Get-ChildItem -Path "docs\*.txt" | ForEach-Object {
    Move-Item $_.FullName "docs\archive\2025-09\" -Force
    Write-Host "Moved: $($_.Name)"
}

# 既存archiveフォルダを2025-08に移動
if (Test-Path "docs\archive\ui_zoom_fix_20250821") {
    Move-Item "docs\archive\ui_zoom_fix_20250821" "docs\archive\2025-08\" -Force
    Write-Host "Moved: ui_zoom_fix_20250821"
}

if (Test-Path "docs\archive\v3_025_drag_drop_implementation_20250822") {
    Move-Item "docs\archive\v3_025_drag_drop_implementation_20250822" "docs\archive\2025-08\" -Force
    Write-Host "Moved: v3_025_drag_drop_implementation_20250822"
}

Write-Host "整理完了"
