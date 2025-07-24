# MCP経由 Windows環境ビルド実行ガイド

## 現在の状況

- **環境**: Mac環境からWindows環境でのビルド実行要請
- **準備完了**: 全修正スクリプト・コマンド準備済み
- **次ステップ**: Windows MCP環境での実行

## Windows MCP環境での実行コマンド

### 前提条件
```cmd
# Windows環境に移動
cd C:\builds\Standard-image-repo\v2.2-taxdoc-organizer
```

### 段階的実行

#### Phase 1: Git同期
```cmd
git pull origin main
```

#### Phase 2: 統合診断・修正スクリプト実行
```powershell
PowerShell -ExecutionPolicy Bypass -File "mcp-windows-build-fix.ps1"
```

#### Phase 3: EXE確認
```powershell
if (Test-Path "release\TaxDocOrganizer.UI.exe") {
    $exe = Get-Item "release\TaxDocOrganizer.UI.exe"
    Write-Host "✅ SUCCESS: $($exe.FullName)"
    Write-Host "📏 Size: $([math]::Round($exe.Length/1MB, 1))MB"
    Write-Host "📅 Created: $($exe.CreationTime)"
}
```

### 成功時の期待出力

```
✅ TaxDocOrganizer V2.2 完成: C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe - [XX]MB - [日時]
```

## 代替手順（手動実行）

Windows環境で以下を順次実行：

```cmd
cd C:\builds\Standard-image-repo\v2.2-taxdoc-organizer
git pull origin main
build-windows.bat
```

## トラブルシューティング

### 問題1: .NET SDK not found
```cmd
# 解決策
winget install Microsoft.DotNet.SDK.6
```

### 問題2: PowerShell実行ポリシー
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser
```

### 問題3: ビルドエラー
```powershell
# 詳細診断実行
PowerShell -ExecutionPolicy Bypass -File "mcp-windows-build-fix.ps1" -DiagnoseOnly
```

## Claude.md第12条準拠確認

実行完了後、以下の形式で結果報告：

### 成功時
```
✅ TaxDocOrganizer V2.2 完成: C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe - [サイズ]MB - [日時]
```

### 失敗時
```
❌ ビルド失敗: [具体的なエラー内容]
🔧 修正必要: [対処方法]
```

---
**生成日時**: 2025-07-21
**対象**: Windows MCP環境
**目的**: TaxDocOrganizer V2.2 EXE生成