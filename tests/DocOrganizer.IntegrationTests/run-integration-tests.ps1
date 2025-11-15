# 統合テスト実行スクリプト
# Week 3 Priority 1 - ローカルで統合テストを実行し、カバレッジレポートを生成

Write-Host "統合テスト実行開始..." -ForegroundColor Green
Write-Host ""

# 1. 統合テストのみ実行
Write-Host "[1/3] 統合テストを実行中..." -ForegroundColor Cyan
dotnet test --filter "Category=Integration" --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "統合テストが失敗しました。" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/3] カバレッジ付きで統合テストを実行中..." -ForegroundColor Cyan

# 2. カバレッジ付き実行
dotnet test `
  --collect:"XPlat Code Coverage" `
  --results-directory:../../.coverage/local/ `
  --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "カバレッジ測定が失敗しました。" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[3/3] カバレッジレポートを生成中..." -ForegroundColor Cyan

# 3. カバレッジレポート生成（ReportGeneratorがインストールされている場合）
if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
    reportgenerator `
      -reports:../../.coverage/local/**/coverage.cobertura.xml `
      -targetdir:../../.coverage/local/report `
      -reporttypes:Html

    Write-Host ""
    Write-Host "完了！カバレッジレポート: .coverage/local/report/index.html" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "完了！（ReportGeneratorがインストールされていないため、HTMLレポートは生成されませんでした）" -ForegroundColor Yellow
    Write-Host "ReportGeneratorをインストールするには:" -ForegroundColor Yellow
    Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "統合テスト実行完了。" -ForegroundColor Green
