# Windows環境でのEXE確認コマンド実行指示

## 実行環境
**対象**: @windows-build-server (Windows環境)
**実行場所**: C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\

## 実行手順

### 1. EXE存在確認
```cmd
dir C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe
```

### 2. 詳細情報取得
```powershell
$exe = Get-Item "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe"
Write-Host "ファイルサイズ: $([math]::Round($exe.Length/1MB, 1))MB"
Write-Host "作成日時: $($exe.CreationTime)"
Write-Host "更新日時: $($exe.LastWriteTime)"
```

### 3. 起動テスト
```powershell
$proc = Start-Process -FilePath $exe.FullName -PassThru
Start-Sleep -Seconds 3
if (-not $proc.HasExited) {
    Write-Host "✅ EXE正常起動"
    Stop-Process -Id $proc.Id -Force
}
```

## 期待される出力形式（Claude.md第12条準拠）
```
✅ TaxDocOrganizer V2.2 完成: C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe - [実際のサイズ]MB - [実際の作成日時]
```

## 注意事項
- EXEが存在しない場合は、まず `build-windows.bat` を実行
- プロセスが正常起動しない場合は、依存関係やランタイムの確認が必要