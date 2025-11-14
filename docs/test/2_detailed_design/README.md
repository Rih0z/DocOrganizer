# テストフレームワーク実装 - 詳細設計

## 📁 このディレクトリについて

このディレクトリには、DocOrganizerのテストフレームワーク実装に関する詳細設計ドキュメントが格納されています。

## 📄 ドキュメント一覧

### 1. 01_test_case_specifications.md
**内容**: 各テストケースの詳細仕様

- V3.0.153検証テスト（V153-001～024）の詳細仕様
- V3.0.145-152回帰テスト（REG-001～004）の詳細仕様
- 核心機能テスト（CORE-001～025）の詳細仕様
- 静的解析テスト（SA-001～002）の詳細仕様
- パフォーマンステスト（PT-001～005）の詳細仕様
- GUI統合テスト（IT-001～006）の詳細仕様

**各テストケースには以下を含む**:
- テストID（トレーサビリティID）
- テスト目的
- 前提条件
- テストデータ（具体的な値）
- テスト手順（ステップバイステップ）
- 期待結果（詳細なアサーション条件）
- 実装例（実際のコード）

### 2. 02_test_helper_classes.md
**内容**: テストヘルパークラスの詳細設計

- TestDataGenerator（完全実装仕様）
- TestDataBuilder（ビルダーパターン）
- AssertionHelper（カスタムアサーション）
- MockHelper（モック生成ヘルパー）
- TestFixtureBase（テストベースクラス）

### 3. 03_performance_baselines.md
**内容**: パフォーマンステストのベースライン設定

- 初回測定手順
- 閾値決定プロセス
- パフォーマンス回帰検出ロジック
- ベンチマーク設定

### 4. 04_gui_integration_tests.md
**内容**: GUI統合テストの詳細実装方法

- FlaUIの具体的な使用方法
- テスト環境セットアップ
- ドラッグ&ドロップ操作の自動化
- スクリーンショット取得・比較

### 5. 05_cicd_pipeline_optimization.md
**内容**: CI/CDパイプラインの最適化詳細設計

- キャッシュ戦略（NuGet、テストデータ）
- 並列実行設定（マトリックス戦略）
- レポート生成（HTML、XML、Markdown）
- 通知設定（失敗時の自動通知）

## 🎯 詳細設計の概要

### フェーズ移行の背景

**基本設計フェーズ完了事項**:
- テストフレームワーク選定（xUnit）
- テストプロジェクト構造設計
- V3.0.153検証方法確定（ILコード解析）
- V3.0.145-152回帰防止方法確定
- CI/CD統合設計

**詳細設計フェーズの目的**:
基本設計で決定した方針を、実装可能なレベルまで詳細化する

### 詳細設計の範囲

#### 1. テストケース詳細仕様

**Phase 1（必須）**:
- V3.0.153検証テスト: 23テストケース
- V3.0.145-152回帰テスト: 7テストケース
- 核心機能テスト: 25テストケース
- 静的解析テスト: 2テストケース
- **合計**: 57テストケース

**Phase 2（推奨）**:
- パフォーマンステスト: 5テストケース
- 追加ユニットテスト: 7テストケース
- **合計**: 12テストケース

**Phase 3（オプション）**:
- GUI統合テスト: 8テストケース
- **合計**: 8テストケース

**総計**: 77テストケース

#### 2. テストヘルパークラス

**TestDataGenerator**:
- PDF生成（10ページ、100ページ、1000ページ）
- 破損PDF生成（4種類のバリエーション）
- 画像生成（jpg, png, heic）
- ランダムPDF生成

**TestDataBuilder**:
- PdfDocumentBuilder
- PdfPageBuilder
- PdfServiceMockBuilder

**AssertionHelper**:
- PdfAssertions（ページ数、回転角度、サイズ検証）
- PerformanceAssertions（実行時間、メモリ使用量検証）
- FileAssertions（ファイル存在、サイズ、タイムスタンプ検証）

**MockHelper**:
- PdfServiceMock
- RotationServiceMock
- UndoRedoServiceMock

#### 3. パフォーマンスベースライン

**測定対象**:
- V3DragDropInfo.CalculateInsertIndex: 5ms以内
- V3DragDropInfo.FindParentListBox: 3ms以内
- RotationService.RotatePageAsync: 50ms以内
- RotationService.RotateMultiplePagesAsync（10ページ）: 500ms以内
- PdfService.LoadPdfAsync（1000ページ）: 3秒以内

**測定方法**:
- BenchmarkDotNetで10回測定
- 平均値 + 2σ を閾値として設定
- CI/CDで継続的に測定・比較

#### 4. GUI統合テスト環境

**FlaUI設定**:
- WinAppDriver不要（UIA3直接使用）
- テスト前にアプリ起動、テスト後に自動クローズ
- スクリーンショット自動取得

**対象操作**:
- アプリケーション起動（5秒以内）
- ドラッグ&ドロップ（3パターン）
- 回転操作（2パターン）

#### 5. CI/CDパイプライン最適化

**キャッシュ戦略**:
- NuGetパッケージ: `~/.nuget/packages`
- テストデータ: `tests/TestData/**`（Git LFS）
- ビルド成果物: `release/`, `release-debug/`

**並列実行**:
- テストプロジェクト単位で並列実行（3並列）
- 推定実行時間: 5分 → 2分に短縮

**レポート生成**:
- JUnit XMLレポート（Azure DevOps互換）
- HTML Coverage Report（ReportGenerator）
- Markdown Summary（PR コメント自動投稿）

## 🔗 関連ドキュメント

### 前フェーズ
- **要件定義**: `docs/test/0_requirement/`
- **基本設計**: `docs/test/1_basic_design/`

### 次のフェーズ
- **実装**: `tests/`（次フェーズで作成予定）
  - テストプロジェクトの実装
  - テストコードの実装
  - CI/CDパイプラインの実装

### 参照ドキュメント
- プロジェクト構造: `docs/rule/project_structure.md`
- V3完全アーキテクチャ: `docs/V3_COMPLETE_ARCHITECTURE.md`

## 🎯 詳細設計の成果物

### 設計ドキュメント（5ファイル）

1. **01_test_case_specifications.md** - 各テストケースの詳細仕様
2. **02_test_helper_classes.md** - テストヘルパークラスの詳細設計
3. **03_performance_baselines.md** - パフォーマンステストのベースライン設定
4. **04_gui_integration_tests.md** - GUI統合テストの詳細実装方法
5. **05_cicd_pipeline_optimization.md** - CI/CDパイプラインの最適化詳細設計

### 実装準備完了の基準

詳細設計フェーズ完了後、以下が明確になっている必要があります：

- ✅ 各テストケースの入力値・期待値が明確
- ✅ テストヘルパークラスの全メソッドシグネチャが確定
- ✅ パフォーマンステストの閾値が決定
- ✅ GUI統合テストの環境構築手順が明確
- ✅ CI/CDパイプラインの全設定が確定

## 📝 更新履歴

- 2025-11-13: 詳細設計フェーズ開始
  - 詳細設計ドキュメント構造確定
  - 5つの設計ドキュメント作成予定
