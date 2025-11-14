# CI/CDパイプライン最適化詳細設計

## 概要

GitHub Actionsを使用したCI/CDパイプラインの最適化戦略を定義します。

## 1. キャッシュ戦略

### 1.1 NuGetパッケージキャッシュ

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

**効果**: 依存関係の復元時間を**3分 → 30秒**に短縮

### 1.2 ビルド成果物キャッシュ

```yaml
- name: Cache build outputs
  uses: actions/cache@v4
  with:
    path: |
      release/
      release-debug/
    key: ${{ runner.os }}-build-${{ github.sha }}
    restore-keys: |
      ${{ runner.os }}-build-
```

**効果**: リビルドの回避により**2分**削減

### 1.3 テストデータキャッシュ（Git LFS）

```yaml
- name: Checkout with LFS
  uses: actions/checkout@v4
  with:
    lfs: true

- name: Cache LFS objects
  uses: actions/cache@v4
  with:
    path: .git/lfs
    key: ${{ runner.os }}-lfs-${{ hashFiles('.gitattributes') }}
```

**効果**: テストデータのダウンロード時間を**1分 → 10秒**に短縮

## 2. 並列実行戦略

### 2.1 テストプロジェクト並列実行

```yaml
test:
  runs-on: windows-latest
  strategy:
    matrix:
      test-project:
        - DocOrganizer.Core.Tests
        - DocOrganizer.Application.Tests
        - DocOrganizer.Infrastructure.Tests
        - DocOrganizer.UI.Tests
    fail-fast: false

  steps:
  - name: Run tests for ${{ matrix.test-project }}
    run: |
      dotnet test tests/${{ matrix.test-project }}/ `
        --configuration Release `
        --no-build `
        --logger "trx;LogFileName=${{ matrix.test-project }}.trx" `
        --collect:"XPlat Code Coverage"
```

**効果**: 4つのテストプロジェクトを並列実行し、**5分 → 2分**に短縮

### 2.2 Phase別並列実行

```yaml
jobs:
  phase1-tests:
    runs-on: windows-latest
    steps:
    - name: Run Phase 1 Tests
      run: dotnet test --filter "Phase=Phase1"

  phase2-tests:
    runs-on: windows-latest
    needs: phase1-tests
    if: github.ref == 'refs/heads/main'
    steps:
    - name: Run Phase 2 Tests
      run: dotnet test --filter "Phase=Phase2"
```

## 3. レポート生成

### 3.1 テスト結果レポート（JUnit XML）

```yaml
- name: Publish Test Results
  uses: EnricoMi/publish-unit-test-result-action/composite@v2
  if: always()
  with:
    files: '**/TestResults/*.trx'
    check_name: 'Test Results'
    comment_mode: always
```

**出力**: PRにテスト結果のサマリーを自動投稿

### 3.2 カバレッジレポート（HTML）

```yaml
- name: Generate Coverage Report
  uses: danielpalme/ReportGenerator-GitHub-Action@5.2.0
  with:
    reports: '**/TestResults/*/coverage.cobertura.xml'
    targetdir: 'coverage-report'
    reporttypes: 'HtmlInline;Badges;MarkdownSummaryGithub'

- name: Add Coverage PR Comment
  uses: marocchino/sticky-pull-request-comment@v2
  if: github.event_name == 'pull_request'
  with:
    recreate: true
    path: coverage-report/SummaryGithub.md
```

**出力**: PRにカバレッジレポートを自動投稿

### 3.3 Markdownサマリー

```yaml
- name: Generate Test Summary
  if: always()
  run: |
    echo "## Test Results Summary" >> $GITHUB_STEP_SUMMARY
    echo "" >> $GITHUB_STEP_SUMMARY
    echo "| Project | Tests | Passed | Failed | Skipped |" >> $GITHUB_STEP_SUMMARY
    echo "|---------|-------|--------|--------|---------|" >> $GITHUB_STEP_SUMMARY

    # TRXファイルからサマリーを抽出
    # （PowerShellスクリプトで実装）
```

**出力**: GitHub Actions のサマリータブに見やすい表を表示

## 4. 通知設定

### 4.1 失敗時の自動通知

```yaml
- name: Notify on failure
  if: failure()
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    text: 'Test failed! Check details at ${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}'
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

### 4.2 カバレッジ低下時の警告

```yaml
- name: Check Coverage Threshold
  run: |
    $coverage = [xml](Get-Content '**/TestResults/*/coverage.cobertura.xml' | Select-Object -First 1)
    $lineRate = [double]$coverage.coverage.'line-rate' * 100

    if ($lineRate -lt 70) {
      echo "::error::Coverage $lineRate% is below threshold 70%"
      exit 1
    }

    if ($lineRate -lt 80) {
      echo "::warning::Coverage $lineRate% is below target 80%"
    }
```

## 5. 完全なワークフロー例

```yaml
name: Test & Coverage

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '8.0.x'

jobs:
  build:
    runs-on: windows-latest
    timeout-minutes: 10

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        lfs: true

    - name: Cache NuGet
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore
      run: dotnet restore

    - name: Build Release
      run: dotnet build --configuration Release --no-restore

    - name: Build Debug
      run: dotnet build --configuration Debug --no-restore

    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: build-outputs
        path: |
          release/
          release-debug/

  test-phase1:
    needs: build
    runs-on: windows-latest
    timeout-minutes: 5
    strategy:
      matrix:
        test-project:
          - DocOrganizer.Application.Tests
          - DocOrganizer.Infrastructure.Tests
          - DocOrganizer.UI.Tests
      fail-fast: false

    steps:
    - name: Checkout
      uses: actions/checkout@v4

    - name: Download build artifacts
      uses: actions/download-artifact@v4
      with:
        name: build-outputs

    - name: Run Phase 1 Tests
      run: |
        dotnet test tests/${{ matrix.test-project }}/ `
          --configuration Release `
          --no-build `
          --filter "Phase=Phase1" `
          --logger "trx;LogFileName=${{ matrix.test-project }}.trx" `
          --collect:"XPlat Code Coverage"

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.test-project }}
        path: '**/TestResults/*.trx'

    - name: Upload coverage
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: coverage-${{ matrix.test-project }}
        path: '**/TestResults/*/coverage.cobertura.xml'

  report:
    needs: test-phase1
    runs-on: windows-latest
    if: always()

    steps:
    - name: Download all test results
      uses: actions/download-artifact@v4
      with:
        pattern: test-results-*
        merge-multiple: true

    - name: Download all coverage reports
      uses: actions/download-artifact@v4
      with:
        pattern: coverage-*
        merge-multiple: true

    - name: Publish Test Results
      uses: EnricoMi/publish-unit-test-result-action/composite@v2
      with:
        files: '**/*.trx'
        check_name: 'Phase 1 Test Results'

    - name: Generate Coverage Report
      uses: danielpalme/ReportGenerator-GitHub-Action@5.2.0
      with:
        reports: '**/coverage.cobertura.xml'
        targetdir: 'coverage-report'
        reporttypes: 'HtmlInline;Badges;MarkdownSummaryGithub'

    - name: Add Coverage Comment
      uses: marocchino/sticky-pull-request-comment@v2
      if: github.event_name == 'pull_request'
      with:
        recreate: true
        path: coverage-report/SummaryGithub.md

    - name: Check Coverage Threshold
      run: |
        $coverage = [xml](Get-Content (Get-ChildItem -Recurse -Filter coverage.cobertura.xml | Select-Object -First 1).FullName)
        $lineRate = [double]$coverage.coverage.'line-rate' * 100

        Write-Host "Current Coverage: $lineRate%"

        if ($lineRate -lt 70) {
          Write-Error "Coverage $lineRate% is below threshold 70%"
          exit 1
        }

  performance-test:
    needs: build
    runs-on: windows-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    timeout-minutes: 5

    steps:
    - name: Checkout
      uses: actions/checkout@v4

    - name: Run Performance Tests
      run: |
        dotnet run --project tests/DocOrganizer.Performance.Tests/ `
          --configuration Release `
          --exporters json markdown

    - name: Upload Benchmark Results
      uses: benchmark-action/github-action-benchmark@v1
      with:
        tool: 'benchmarkdotnet'
        output-file-path: BenchmarkDotNet.Artifacts/results/benchmarks.json
        github-token: ${{ secrets.GITHUB_TOKEN }}
        auto-push: true
```

## 6. 最適化効果のまとめ

| 最適化項目 | 最適化前 | 最適化後 | 削減時間 |
|-----------|---------|---------|---------|
| NuGetキャッシュ | 3分 | 30秒 | **2分30秒** |
| 並列実行 | 5分 | 2分 | **3分** |
| ビルドキャッシュ | 2分 | 0秒 | **2分** |
| 合計 | **10分** | **2分30秒** | **7分30秒** |

**総合効果**: CI/CD実行時間を**75%削減**

## まとめ

これらの最適化により、CI/CDパイプラインの実行時間を大幅に短縮し、開発者のフィードバックサイクルを高速化できます。
