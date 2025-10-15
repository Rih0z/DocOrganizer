# =====================================================
# GitHubリリース自動作成スクリプト (PowerShell版)
# =====================================================
#
# 機能:
# - Version.csからバージョン番号を自動取得
# - リリースビルドを実行
# - GitHubリリースを作成
# - 実行ファイルをアップロード
#
# 前提条件:
# - GitHub CLI (gh) がインストール済み
# - gh auth login でGitHub認証済み
#
# 使用方法:
#   .\.script\create-github-release.ps1
#   .\.script\create-github-release.ps1 -ReleaseNotes "カスタムリリースノート"
# =====================================================

param(
    [string]$ReleaseNotes = ""
)

# エラー時に停止
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " DocOrganizer GitHub Release Creator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# =====================================================
# ステップ1: バージョン番号の取得
# =====================================================

Write-Host "[1/6] Version.csからバージョン番号を取得中..." -ForegroundColor Yellow

$versionFile = "src\DocOrganizer.Core\Version.cs"

if (-not (Test-Path $versionFile)) {
    Write-Host "エラー: Version.csが見つかりません: $versionFile" -ForegroundColor Red
    exit 1
}

$versionContent = Get-Content $versionFile -Raw
if ($versionContent -match 'public const string Version = "([0-9]+\.[0-9]+\.[0-9]+)"') {
    $version = $matches[1]
    $tagName = "v$version"
    Write-Host "  ✓ バージョン: $version" -ForegroundColor Green
    Write-Host "  ✓ タグ名: $tagName" -ForegroundColor Green
} else {
    Write-Host "エラー: Version.csからバージョン番号を取得できませんでした" -ForegroundColor Red
    exit 1
}

# =====================================================
# ステップ2: GitHub CLI (gh) の確認
# =====================================================

Write-Host ""
Write-Host "[2/6] GitHub CLI (gh) の確認..." -ForegroundColor Yellow

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "エラー: GitHub CLI (gh) がインストールされていません" -ForegroundColor Red
    Write-Host "  インストール方法: winget install --id GitHub.cli" -ForegroundColor Yellow
    exit 1
}

Write-Host "  ✓ GitHub CLI: インストール済み" -ForegroundColor Green

# GitHub認証確認
$ghAuthStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "エラー: GitHub CLIで認証されていません" -ForegroundColor Red
    Write-Host "  認証方法: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "  ✓ GitHub認証: OK" -ForegroundColor Green

# =====================================================
# ステップ3: Gitの状態確認
# =====================================================

Write-Host ""
Write-Host "[3/6] Gitの状態確認..." -ForegroundColor Yellow

# 未コミットの変更確認
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host "警告: 未コミットの変更があります" -ForegroundColor Yellow
    Write-Host $gitStatus
    $confirm = Read-Host "続行しますか? (y/N)"
    if ($confirm -ne "y" -and $confirm -ne "Y") {
        Write-Host "キャンセルしました" -ForegroundColor Yellow
        exit 0
    }
}

# リモートと同期確認
git fetch origin main 2>&1 | Out-Null
$localCommit = git rev-parse main
$remoteCommit = git rev-parse origin/main

if ($localCommit -ne $remoteCommit) {
    Write-Host "警告: ローカルとリモートが同期していません" -ForegroundColor Yellow
    $confirm = Read-Host "git pushを実行しますか? (y/N)"
    if ($confirm -eq "y" -or $confirm -eq "Y") {
        Write-Host "  → git push実行中..." -ForegroundColor Cyan
        git push origin main
        if ($LASTEXITCODE -ne 0) {
            Write-Host "エラー: git push失敗" -ForegroundColor Red
            exit 1
        }
        Write-Host "  ✓ git push完了" -ForegroundColor Green
    }
}

Write-Host "  ✓ Git状態: OK" -ForegroundColor Green

# =====================================================
# ステップ4: リリースビルド
# =====================================================

Write-Host ""
Write-Host "[4/6] リリースビルド実行中..." -ForegroundColor Yellow

# クリーン
Write-Host "  → dotnet clean..." -ForegroundColor Cyan
dotnet clean --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "エラー: dotnet clean失敗" -ForegroundColor Red
    exit 1
}

# リストア
Write-Host "  → dotnet restore..." -ForegroundColor Cyan
dotnet restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "エラー: dotnet restore失敗" -ForegroundColor Red
    exit 1
}

# リリースビルド
Write-Host "  → dotnet publish (リリースビルド・ログ無効版)..." -ForegroundColor Cyan
dotnet publish src\DocOrganizer.UI\DocOrganizer.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o release `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "エラー: dotnet publish失敗" -ForegroundColor Red
    exit 1
}

# EXE存在確認
$exePath = "release\DocOrganizer.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "エラー: $exePath が生成されませんでした" -ForegroundColor Red
    exit 1
}

$exeSize = (Get-Item $exePath).Length / 1MB
Write-Host "  ✓ ビルド完了: $exePath ($([math]::Round($exeSize, 1)) MB)" -ForegroundColor Green

# =====================================================
# ステップ5: リリースノート作成
# =====================================================

Write-Host ""
Write-Host "[5/6] リリースノート作成..." -ForegroundColor Yellow

if ($ReleaseNotes -eq "") {
    # CLAUDE.mdからバージョン履歴を取得
    $claudeMd = Get-Content "CLAUDE.md" -Raw

    if ($claudeMd -match "\| V$version \| ([0-9\-]+) \| (.+?) \|") {
        $changeDate = $matches[1]
        $changeDesc = $matches[2]

        $ReleaseNotes = @"
## V$version ($changeDate)

### 変更内容
$changeDesc

---

**ダウンロード**: 下記の ``DocOrganizer.exe`` をダウンロードしてご使用ください。

**インストール方法**:
1. 既存の ``DocOrganizer.exe`` を終了
2. ダウンロードした ``DocOrganizer.exe`` で上書き
3. アプリケーションを再起動

**システム要件**: Windows 10/11 (64bit)

---

**自動アップデート**: アプリケーション内の「ヘルプ」→「アップデート確認」から自動更新可能です。
"@
    } else {
        # デフォルトのリリースノート
        $ReleaseNotes = @"
## DocOrganizer V$version

### 変更内容
詳細はCLAUDE.mdを参照してください。

---

**ダウンロード**: 下記の ``DocOrganizer.exe`` をダウンロードしてご使用ください。

**システム要件**: Windows 10/11 (64bit)
"@
    }
}

Write-Host "  ✓ リリースノート準備完了" -ForegroundColor Green

# =====================================================
# ステップ6: GitHubリリース作成
# =====================================================

Write-Host ""
Write-Host "[6/6] GitHubリリース作成中..." -ForegroundColor Yellow

# 既存のリリース確認
$existingRelease = gh release view $tagName 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "警告: タグ $tagName のリリースが既に存在します" -ForegroundColor Yellow
    $confirm = Read-Host "既存のリリースを削除して再作成しますか? (y/N)"
    if ($confirm -eq "y" -or $confirm -eq "Y") {
        Write-Host "  → 既存リリース削除中..." -ForegroundColor Cyan
        gh release delete $tagName --yes
        if ($LASTEXITCODE -ne 0) {
            Write-Host "エラー: リリース削除失敗" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "キャンセルしました" -ForegroundColor Yellow
        exit 0
    }
}

# リリース作成
Write-Host "  → GitHubリリース作成中..." -ForegroundColor Cyan

# リリースノートを一時ファイルに保存
$tempNotesFile = [System.IO.Path]::GetTempFileName()
$ReleaseNotes | Out-File -FilePath $tempNotesFile -Encoding UTF8

gh release create $tagName `
    $exePath `
    --title "DocOrganizer V$version" `
    --notes-file $tempNotesFile `
    --latest

# 一時ファイル削除
Remove-Item $tempNotesFile -Force

if ($LASTEXITCODE -ne 0) {
    Write-Host "エラー: GitHubリリース作成失敗" -ForegroundColor Red
    exit 1
}

Write-Host "  ✓ GitHubリリース作成完了" -ForegroundColor Green

# =====================================================
# 完了
# =====================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " ✓ リリース作成完了！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "リリース情報:" -ForegroundColor Cyan
Write-Host "  バージョン: V$version" -ForegroundColor White
Write-Host "  タグ: $tagName" -ForegroundColor White
Write-Host "  URL: https://github.com/Rih0z/DocOrganizer/releases/tag/$tagName" -ForegroundColor White
Write-Host ""
Write-Host "次のステップ:" -ForegroundColor Cyan
Write-Host "  1. GitHubでリリースを確認: https://github.com/Rih0z/DocOrganizer/releases" -ForegroundColor White
Write-Host "  2. アプリケーション内で「ヘルプ」→「アップデート確認」をテスト" -ForegroundColor White
Write-Host ""
