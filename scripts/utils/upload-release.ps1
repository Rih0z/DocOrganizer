# GitHub Release Upload Script for DocOrganizer
# 大容量EXEファイルをGitHub Releasesにアップロードするスクリプト

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [string]$ExePath = "release\DocOrganizer.exe",
    [string]$Owner = "Rih0z",
    [string]$Repo = "DocOrganizer"
)

# 設定
$ApiUrl = "https://api.github.com"
$Headers = @{
    "Authorization" = "token $GitHubToken"
    "Accept" = "application/vnd.github.v3+json"
    "User-Agent" = "DocOrganizer-Release-Script"
}

Write-Host "🚀 DocOrganizer Release Upload Script" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "File: $ExePath" -ForegroundColor Yellow

# EXEファイルの存在確認
if (-not (Test-Path $ExePath)) {
    Write-Error "❌ EXEファイルが見つかりません: $ExePath"
    exit 1
}

$FileInfo = Get-Item $ExePath
$FileSizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
Write-Host "ファイルサイズ: $FileSizeMB MB" -ForegroundColor Yellow

# Step 1: リリースの作成
Write-Host "`n📝 Step 1: GitHub Releaseを作成中..." -ForegroundColor Cyan

$ReleaseData = @{
    tag_name = "v$Version"
    target_commitish = "main"
    name = "DocOrganizer v$Version"
    body = @"
# DocOrganizer v$Version

## 🎯 主な機能
- ✅ PDF編集: ページ結合・分割・回転・削除
- ✅ 画像変換: HEIC・JPG・PNG・JPEG → PDF
- ✅ 向き自動補正: スキャン文書の自動回転
- ✅ ドラッグ&ドロップ: 直感的なファイル操作
- ✅ 自動更新: GitHub Releases連携

## 📦 インストール手順
1. **DocOrganizer.exe** をダウンロード
2. 任意のフォルダに配置
3. エクスプローラーからダブルクリックで起動
4. ⚠️ **重要**: 管理者権限で実行しないこと（ドラッグ&ドロップが無効化されます）

## 📋 動作環境
- Windows 10/11 (64-bit)
- .NET 6.0 Runtime (自己完結型のため不要)
- メモリ: 4GB以上推奨

## 🔄 更新内容
このバージョンの詳細な更新内容については、[Commits](https://github.com/$Owner/$Repo/commits/v$Version) を確認してください。

## 🆘 サポート
- 🐛 **不具合報告**: [Issues](https://github.com/$Owner/$Repo/issues)
- 📖 **使用方法**: [Documentation](https://github.com/$Owner/$Repo/tree/main/docs)
- 💬 **質問・要望**: [Discussions](https://github.com/$Owner/$Repo/discussions)

---
**DocOrganizer** - プロフェッショナルな文書整理を簡単に
"@
    draft = $false
    prerelease = $false
} | ConvertTo-Json -Depth 10

try {
    $CreateUrl = "$ApiUrl/repos/$Owner/$Repo/releases"
    $Response = Invoke-RestMethod -Uri $CreateUrl -Method POST -Headers $Headers -Body $ReleaseData -ContentType "application/json"
    $ReleaseId = $Response.id
    $UploadUrl = $Response.upload_url -replace '\{\?name,label\}', ''
    
    Write-Host "✅ Release作成成功: ID $ReleaseId" -ForegroundColor Green
} catch {
    Write-Error "❌ Release作成失敗: $($_.Exception.Message)"
    
    # 既存のリリースが存在する場合の処理
    if ($_.Exception.Message -like "*already_exists*") {
        Write-Host "⚠️  既存のリリースを取得中..." -ForegroundColor Yellow
        try {
            $GetUrl = "$ApiUrl/repos/$Owner/$Repo/releases/tags/v$Version"
            $ExistingRelease = Invoke-RestMethod -Uri $GetUrl -Method GET -Headers $Headers
            $ReleaseId = $ExistingRelease.id
            $UploadUrl = $ExistingRelease.upload_url -replace '\{\?name,label\}', ''
            Write-Host "✅ 既存のRelease使用: ID $ReleaseId" -ForegroundColor Green
        } catch {
            Write-Error "❌ 既存のRelease取得失敗: $($_.Exception.Message)"
            exit 1
        }
    } else {
        exit 1
    }
}

# Step 2: EXEファイルのアップロード
Write-Host "`n📤 Step 2: EXEファイルをアップロード中..." -ForegroundColor Cyan
Write-Host "アップロード先: $UploadUrl" -ForegroundColor Gray

$FileName = Split-Path $ExePath -Leaf
$ContentType = "application/octet-stream"
$UploadUrlWithName = "$UploadUrl" + "?name=$FileName"

# アップロード用ヘッダー（Content-Type指定）
$UploadHeaders = $Headers.Clone()
$UploadHeaders["Content-Type"] = $ContentType

# プログレス表示付きアップロード
$ProgressPreference = 'Continue'

try {
    Write-Host "アップロード開始: $FileName ($FileSizeMB MB)" -ForegroundColor Yellow
    
    # ファイルを読み込み
    $FileBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $ExePath).Path)
    
    # REST APIでアップロード
    $UploadResponse = Invoke-RestMethod -Uri $UploadUrlWithName -Method POST -Headers $UploadHeaders -Body $FileBytes
    
    Write-Host "✅ アップロード完了!" -ForegroundColor Green
    Write-Host "ダウンロードURL: $($UploadResponse.browser_download_url)" -ForegroundColor Green
    
} catch {
    Write-Error "❌ アップロード失敗: $($_.Exception.Message)"
    Write-Host "エラー詳細:" -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor Red
    exit 1
}

# Step 3: 完了報告
Write-Host "`n🎉 GitHub Release作成完了!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "📦 Release: https://github.com/$Owner/$Repo/releases/tag/v$Version" -ForegroundColor Cyan
Write-Host "⬇️  ダウンロード: $($UploadResponse.browser_download_url)" -ForegroundColor Cyan
Write-Host "📊 ファイルサイズ: $FileSizeMB MB" -ForegroundColor Yellow
Write-Host "🏷️  バージョン: v$Version" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green

Write-Host "`n📋 次のステップ:" -ForegroundColor Yellow
Write-Host "1. リリースページで内容を確認" -ForegroundColor White
Write-Host "2. DocOrganizer.exeの自動更新機能をテスト" -ForegroundColor White
Write-Host "3. ユーザーに新バージョンを告知" -ForegroundColor White

Write-Host "`n🚀 DocOrganizer v$Version リリース完了!" -ForegroundColor Green