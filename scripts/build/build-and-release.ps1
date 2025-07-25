# DocOrganizer Build and Release Script
# ビルドからGitHub Releaseまでの完全自動化スクリプト

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipBuild,
    [switch]$SkipTests
)

# 色付きログ関数
function Write-Step {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "`n🔄 $Message" -ForegroundColor $Color
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

# スクリプト開始
Write-Host "🚀 DocOrganizer Build & Release Pipeline" -ForegroundColor Green
Write-Host "Version: v$Version" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Skip Build: $SkipBuild" -ForegroundColor Yellow
Write-Host "Skip Tests: $SkipTests" -ForegroundColor Yellow

$StartTime = Get-Date

# Step 1: 環境確認
Write-Step "Step 1: 環境確認"

# .NET SDK確認
try {
    $DotNetVersion = dotnet --version
    Write-Success ".NET SDK バージョン: $DotNetVersion"
} catch {
    Write-Error-Custom ".NET SDKが見つかりません。インストールしてください。"
    exit 1
}

# Git確認
try {
    $GitVersion = git --version
    Write-Success "Git バージョン: $GitVersion"
} catch {
    Write-Error-Custom "Gitが見つかりません。インストールしてください。"
    exit 1
}

# プロジェクトファイル確認
$ProjectPath = "src\DocOrganizer.UI\DocOrganizer.UI.csproj"
if (-not (Test-Path $ProjectPath)) {
    Write-Error-Custom "プロジェクトファイルが見つかりません: $ProjectPath"
    exit 1
}
Write-Success "プロジェクトファイル確認: $ProjectPath"

# Step 2: Git状態確認
Write-Step "Step 2: Git状態確認"

$GitStatus = git status --porcelain
if ($GitStatus) {
    Write-Warning-Custom "未コミットの変更があります:"
    Write-Host $GitStatus -ForegroundColor Yellow
    
    $Confirmation = Read-Host "続行しますか？ (y/N)"
    if ($Confirmation -ne "y" -and $Confirmation -ne "Y") {
        Write-Host "操作をキャンセルしました。" -ForegroundColor Yellow
        exit 0
    }
}

$CurrentBranch = git branch --show-current
Write-Success "現在のブランチ: $CurrentBranch"

# Step 3: バージョン更新
Write-Step "Step 3: バージョン更新"

# csprojファイルのバージョン更新
$CsprojContent = Get-Content $ProjectPath -Raw
$CsprojContent = $CsprojContent -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>"
$CsprojContent = $CsprojContent -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>"
$CsprojContent = $CsprojContent -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$Version.0</FileVersion>"

Set-Content $ProjectPath -Value $CsprojContent -Encoding UTF8
Write-Success "バージョン更新完了: v$Version"

# Step 4: テスト実行
if (-not $SkipTests) {
    Write-Step "Step 4: テスト実行"
    
    try {
        dotnet test --configuration $Configuration --verbosity minimal
        Write-Success "全テスト成功"
    } catch {
        Write-Error-Custom "テストが失敗しました。"
        exit 1
    }
} else {
    Write-Warning-Custom "Step 4: テストをスキップしました"
}

# Step 5: ビルド実行
if (-not $SkipBuild) {
    Write-Step "Step 5: ビルド実行"
    
    # クリーンアップ
    Write-Host "🧹 クリーンアップ中..." -ForegroundColor Yellow
    dotnet clean --configuration $Configuration
    
    # 復元
    Write-Host "📦 依存関係を復元中..." -ForegroundColor Yellow
    dotnet restore
    
    # ビルド
    Write-Host "🔨 ビルド中..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration --no-restore
    
    # パブリッシュ
    Write-Host "📤 パブリッシュ中..." -ForegroundColor Yellow
    $PublishArgs = @(
        "publish"
        $ProjectPath
        "-c", $Configuration
        "-r", $Runtime
        "--self-contained", "true"
        "-p:PublishSingleFile=true"
        "-p:PublishReadyToRun=true"
        "-p:IncludeNativeLibrariesForSelfExtract=true"
        "-o", "release"
        "--no-restore"
    )
    
    & dotnet @PublishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "ビルドが失敗しました。(Exit Code: $LASTEXITCODE)"
        exit 1
    }
    
    Write-Success "ビルド完了"
} else {
    Write-Warning-Custom "Step 5: ビルドをスキップしました"
}

# Step 6: EXEファイル確認
Write-Step "Step 6: EXEファイル確認"

$ExePath = "release\DocOrganizer.exe"
if (-not (Test-Path $ExePath)) {
    Write-Error-Custom "EXEファイルが生成されていません: $ExePath"
    exit 1
}

$FileInfo = Get-Item $ExePath
$FileSizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
Write-Success "EXEファイル確認: $FileSizeMB MB"

# Step 7: Git コミット
Write-Step "Step 7: Git コミット"

git add .
$CommitMessage = "[Release] DocOrganizer v$Version リリース準備完了

- バージョン更新: v$Version
- ビルド完了: DocOrganizer.exe ($FileSizeMB MB)
- テスト実行: " + $(if ($SkipTests) { "スキップ" } else { "成功" }) + "
- リリース準備: GitHub Release対応

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>"

git commit -m $CommitMessage
git push origin $CurrentBranch

Write-Success "Git コミット・プッシュ完了"

# Step 8: GitHub Release作成
Write-Step "Step 8: GitHub Release作成"

$UploadScriptPath = "scripts\utils\upload-release.ps1"
if (-not (Test-Path $UploadScriptPath)) {
    Write-Error-Custom "アップロードスクリプトが見つかりません: $UploadScriptPath"
    exit 1
}

try {
    & $UploadScriptPath -Version $Version -GitHubToken $GitHubToken -ExePath $ExePath
    Write-Success "GitHub Release作成完了"
} catch {
    Write-Error-Custom "GitHub Release作成失敗: $($_.Exception.Message)"
    exit 1
}

# 完了報告
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Host "`n🎉 DocOrganizer v$Version リリース完了!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "⏱️  実行時間: $($Duration.ToString('mm\:ss'))" -ForegroundColor Yellow
Write-Host "📦 ファイルサイズ: $FileSizeMB MB" -ForegroundColor Yellow
Write-Host "🔗 リリースページ: https://github.com/Rih0z/DocOrganizer/releases/tag/v$Version" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green