# テストフレームワーク実装 - 基本設計

## 📁 このディレクトリについて

このディレクトリには、DocOrganizerのテストフレームワーク実装に関する基本設計ドキュメントが格納されています。

## 📄 ドキュメント一覧

### 1. design.md

**内容**: テストフレームワーク実装の基本設計書

**主要セクション**:
1. テストプロジェクト構造設計
2. テストフレームワーク選定（xUnit）
3. 依存パッケージ定義
4. V3.0.153検証テストの設計（ILコード解析）
5. V3.0.145-152回帰テストの設計（静的解析+動的テスト）
6. 核心機能テストの設計（CORE-001～025）
7. 静的解析ルール設計（Roslynアナライザー）
8. CI/CD統合設計（GitHub Actions）
9. テストデータ準備（リポジトリ管理+動的生成）
10. 次フェーズへの引き継ぎ事項

## 🎯 基本設計の概要

### テストフレームワーク最終選定: xUnit

**選定理由**:
- 並列実行がデフォルト（CI/CDで5分以内達成に貢献）
- BenchmarkDotNetとの統合が簡単
- シンプルな構文（`[Fact]`, `[Theory]`）
- コミュニティサポートが活発
- FluentAssertionsとの相性抜群

### テストプロジェクト構造

```
tests/
├── DocOrganizer.Core.Tests/              # Phase 2
├── DocOrganizer.Application.Tests/       # Phase 1（核心機能）
├── DocOrganizer.Infrastructure.Tests/    # Phase 1（V3.0.153検証、回帰防止）
├── DocOrganizer.UI.Tests/                # Phase 1 & 3
├── DocOrganizer.Performance.Tests/       # Phase 2（ベンチマーク）
├── DocOrganizer.StaticAnalysis/          # Phase 1（Roslynアナライザー）
└── TestData/                             # テストデータ（Git LFS）
```

### V3.0.153検証方法

**戦略**: ILコード解析（Mono.Cecil使用）

1. **リリース版EXE読み込み**
2. **対象クラス・メソッドを取得**（V153-001～024）
3. **IL命令をスキャン**
4. **Debug.WriteLine / File.WriteAllText が存在しないことを検証**
5. **デバッグ版でも同様にスキャン**（存在することを検証）

### V3.0.145-152回帰防止方法

**戦略**: 静的解析 + 動的テスト

#### 静的解析
- **REG-001**: `RefreshPageListWithSelection` で `Pages.Clear()` 不使用を検証
- **REG-002**: `SyncSelectionFromViewModel` で `SelectedItems.Clear()` 不使用を検証
- **REG-003**: `OnPageRotated` で `Pages[pageIndex] = e.Page` 不使用を検証

#### 動的テスト
- **REG-004**: 回転後に選択が維持されることを統合テストで検証

### 核心機能テスト設計

#### CORE-001～005: PDF読み込み
- 正常なPDF読み込み
- 空PDFの処理
- 破損PDFの処理
- 大容量PDFの処理（メモリリークチェック）
- 同時読み込み

#### CORE-006～010: ページ回転
- 90度・180度・270度・360度回転
- 複数ページ一括回転（50ms以内）

#### CORE-011～015: Undo/Redo
- 回転後Undo
- Undo後Redo
- 複数回Undo
- 100回操作のUndo/Redo
- 削除後Undo

#### CORE-016～020: PDF保存
- 回転後保存
- ページ削除後保存
- ページ並び替え後保存
- 上書き保存
- 別名保存

#### CORE-021～025: ドラッグ&ドロップ
- 空ListBoxでのInsertIndex
- 上半分ドロップ
- 下半分ドロップ
- 末尾より下ドロップ
- null入力処理

### 静的解析ルール

**Roslynアナライザー実装**:

- **DA001**: `Debug.WriteLine` は `#if DEBUG` で囲む必要があります
- **DA002**: `File.WriteAllText` は `#if ENABLE_LOGGING` で囲む必要があります

**除外パターン**:
- `tests/` フォルダ内は除外

### CI/CD統合

**GitHub Actions ワークフロー**:

```yaml
name: Test & Coverage

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: windows-latest
    timeout-minutes: 10

    steps:
    - Run Phase 1 Tests (必須) - 5分以内
    - Upload Test Results
    - Upload Coverage Reports
    - Check Coverage Threshold (70%以上)

  performance-test:
    needs: test
    if: github.ref == 'refs/heads/main'
    - Run Performance Tests (Phase 2)
    - Upload Benchmark Results
```

**最適化戦略**:
- 並列実行（xUnitデフォルト）
- フィルタリング（Phase 1のみCI/CD）
- NuGetキャッシュ
- 各ジョブ5分タイムアウト

### テストデータ管理

#### リポジトリ管理（Git LFS）
- `tests/TestData/Pdfs/sample_10pages.pdf` (500KB)
- `tests/TestData/Pdfs/sample_empty.pdf` (0ページ)
- `tests/TestData/Pdfs/sample_corrupted.pdf` (破損)
- `tests/TestData/Images/` (jpg, png, heic)

#### 動的生成
- 1000ページPDF（パフォーマンステスト用）
- 破損PDFバリエーション（エラーハンドリングテスト用）
- 大容量画像（メモリリークテスト用）

## 📊 依存パッケージ

### 共通パッケージ
- xunit 2.6.2
- xunit.runner.visualstudio 2.5.4
- FluentAssertions 6.12.0
- Moq 4.20.70
- coverlet.collector 6.0.0
- Microsoft.NET.Test.Sdk 17.8.0

### UI Tests固有
- FlaUI.Core 4.0.0（Phase 3のみ）
- FlaUI.UIA3 4.0.0（Phase 3のみ）

### Performance Tests固有
- BenchmarkDotNet 0.13.12
- BenchmarkDotNet.Diagnostics.Windows 0.13.12

### StaticAnalysis固有
- Microsoft.CodeAnalysis.CSharp 4.8.0
- Microsoft.CodeAnalysis.Analyzers 3.3.4

## 🔗 関連ドキュメント

### 前フェーズ
- **要件定義**: `docs/test/0_requirement/` （完了）

### 次のフェーズ
- **詳細設計**: `docs/test/2_detailed_design/` （次フェーズで作成予定）
  - 各テストケースの詳細仕様
  - テストヘルパークラスの詳細設計
  - パフォーマンステストのベースライン設定
  - GUI統合テストの詳細実装方法

### 参照ドキュメント
- プロジェクト構造: `docs/rule/project_structure.md`
- V3完全アーキテクチャ: `docs/V3_COMPLETE_ARCHITECTURE.md`

## 🎯 主要決定事項

### 1. テストフレームワーク: xUnit
- 理由: 並列実行、BenchmarkDotNet統合、シンプル、コミュニティサポート

### 2. テストプロジェクト構造
- 5つのテストプロジェクト + 1つの静的解析プロジェクト
- Phase別に実装優先度を明確化

### 3. V3.0.153検証方法: ILコード解析
- Mono.Cecilでアセンブリ解析
- Release/Debugでの差異を検証

### 4. V3.0.145-152回帰防止: 静的解析 + 動的テスト
- ソースコード静的解析で禁止パターン検出
- 動的テストで選択維持を検証

### 5. CI/CD統合: GitHub Actions
- Phase 1のみ自動実行（5分以内）
- カバレッジ70%以上を必須

## 🚀 次のステップ

### 詳細設計フェーズで具体化

1. **各テストケースの詳細仕様**
   - テストデータの具体的な値
   - アサーションの詳細条件
   - モックの詳細設定

2. **テストヘルパークラスの設計**
   - テストデータ生成ヘルパー
   - アサーションヘルパー
   - モック生成ヘルパー

3. **パフォーマンステストの閾値決定**
   - 初回測定による実測値取得
   - ベースライン設定

4. **GUI統合テストの詳細実装方法**
   - FlaUIの具体的な使用方法
   - テスト環境のセットアップ

5. **CI/CDパイプラインの最適化**
   - キャッシュ戦略の詳細
   - 並列実行の詳細設定

## 📝 更新履歴

- 2025-11-13: 基本設計完了
  - テストフレームワーク選定（xUnit）
  - テストプロジェクト構造設計
  - V3.0.153検証方法確定（ILコード解析）
  - V3.0.145-152回帰防止方法確定
  - CI/CD統合設計
