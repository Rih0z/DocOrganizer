# DocOrganizer 統合テストベストプラクティス集

**作成日**: 2025-11-15
**対象**: DocOrganizer統合テスト実装者
**ベース**: Week 3 Priority 1実施成果（Day 0～2）

---

## 📋 このドキュメントについて

このベストプラクティス集は、**Week 3 Priority 1（Day 0～2）の実施経験から抽出した、統合テスト実装の実践的ノウハウ集**です。

### 対象読者

- DocOrganizer統合テストを新規実装する開発者
- 既存テストをメンテナンスする開発者
- テストフレームワークを理解したい開発者

### このドキュメントの価値

Week 3 Priority 1では、**3日間で17テスト（100%成功率）を達成**しましたが、その過程で：

- ❌ **6つの技術的課題**に直面
- ✅ **10以上の重要な発見**を記録
- 🔄 **3回のアプローチ修正**を実施

これらの経験を体系化することで、**今後の実装者が同じ試行錯誤を繰り返さない**ことを目指します。

---

## 📁 ドキュメント構成

このベストプラクティス集は以下のドキュメントで構成されています：

### 1. [実装者向けメッセージ](implementer_message.md) ⭐ **最初に読むべき**

**対象**: これから統合テストを書く開発者

**内容**:
- Week 3 Priority 1で学んだ最重要教訓
- 実装前に知っておくべき5つのこと
- 成功のための心構え

**読了時間**: 5分

---

### 2. [テストフレームワーク実践ガイド](framework_guide.md)

**対象**: IntegrationTestFixture・StaFact・TestDataHelperを使う開発者

**内容**:
- IntegrationTestFixture完全ガイド
  - UIスレッド同期パターン
  - サービス取得パターン
  - Dispatcherの正しい使い方
- StaFact属性の使い方
- TestDataHelper活用法

**読了時間**: 15分

---

### 3. [よくある失敗パターンと解決策集](common_failures.md) ⭐ **トラブル時に参照**

**対象**: テスト実装中にエラーに直面した開発者

**内容**:
- Day 0～2で実際に発生した6つの課題
  - StaFactAttribute機能しない → 公式パッケージ採用
  - PdfPage.Indexが存在しない → PageNumber使用
  - ILogger依存関係不足 → AddLogging追加
  - MainCompositeViewModel複雑すぎ → サービスレイヤーテスト
  - SavePdfAsync永続化しない → 仕様理解に基づく戦略変更
  - PageNumber削除後も保持 → 有効性検証に変更
- 各課題の原因・解決策・学習事項

**読了時間**: 10分

---

### 4. [アーキテクチャ理解ガイド](architecture_guide.md)

**対象**: DocOrganizerアーキテクチャを理解したい開発者

**内容**:
- イベント駆動型アーキテクチャの理解
  - DocumentOpened イベント発火 → OnDocumentOpened → LoadPagesAsync
- PdfPageモデル構造の完全理解
  - PageNumberプロパティ（1-based）
  - Width/Heightプロパティ
- サービス層の依存関係マッピング
  - IPdfService / IPdfEditorService
  - IThumbnailGeneratorService / ITextOrientationService
  - IPdfExportService

**読了時間**: 15分

---

### 5. [テスト戦略ガイド](strategy_guide.md)

**対象**: テスト設計・計画を行う開発者

**内容**:
- サービスレイヤーテスト vs ViewModelテスト
  - いつサービスレイヤーをテストすべきか
  - ViewModelテストの複雑性とトレードオフ
- テストデータ生成戦略
  - 動的生成 vs 静的ファイル
  - TestDataHelperパターン
- テストカバレッジの考え方
  - IT-001: 1/10/50ページPDF（境界値テスト）
  - IT-002: 先頭/中間/最終ページ削除（網羅的カバー）

**読了時間**: 20分

---

### 6. [CI/CDベストプラクティス](cicd_guide.md)

**対象**: GitHub Actions設定・カバレッジ測定を行う開発者

**内容**:
- GitHub Actions設定の完全ガイド
  - actions v4推奨（v3は非推奨）
  - XPlat Code Coverage設定
  - ReportGenerator設定
- ローカル実行スクリプト活用法
- テスト実行の最適化
  - `--filter "FullyQualifiedName~IT001"` 個別実行
  - `--collect:"XPlat Code Coverage"` カバレッジ収集

**読了時間**: 15分

---

### 7. [技術的発見事項一覧](technical_discoveries.md)

**対象**: DocOrganizer特有の仕様を理解したい開発者

**内容**:
- SavePdfAsync()動作仕様
  - ページ削除: ✅ 永続化される
  - ページ回転: ❌ 永続化されない
  - ページ並び替え: ❌ 永続化されない
- PageNumberプロパティの挙動
  - 削除後も元の値を保持
  - 並び替え後も元の値を保持
- NoOpTextOrientationService選択理由
  - 統合テストではOCR処理不要

**読了時間**: 10分

---

## 🚀 クイックスタート

### 初めて統合テストを書く場合

1. ✅ **[実装者向けメッセージ](implementer_message.md)** を読む（5分）
2. ✅ **[テストフレームワーク実践ガイド](framework_guide.md)** を読む（15分）
3. ✅ 既存テスト（IT001/IT002/IT003）を参照しながら実装開始
4. ⚠️ 問題発生時は **[よくある失敗パターン](common_failures.md)** を参照

**合計時間**: 約20分（実装開始まで）

---

### 特定の問題に直面した場合

| 問題 | 参照ドキュメント | セクション |
|------|---------------|----------|
| StaFactが機能しない | [よくある失敗パターン](common_failures.md) | 課題1 |
| PdfPage.Indexが存在しない | [よくある失敗パターン](common_failures.md) | 課題2 |
| ILogger依存関係エラー | [よくある失敗パターン](common_failures.md) | 課題3 |
| ViewModelテストが複雑 | [テスト戦略ガイド](strategy_guide.md) | サービスレイヤーテスト |
| SavePdfAsync()が期待通り動かない | [技術的発見事項](technical_discoveries.md) | SavePdfAsync動作仕様 |
| GitHub Actions失敗 | [CI/CDガイド](cicd_guide.md) | トラブルシューティング |

---

### CI/CD設定を行う場合

1. ✅ **[CI/CDベストプラクティス](cicd_guide.md)** を読む（15分）
2. ✅ `.github/workflows/integration-tests.yml` を参照
3. ✅ `run-integration-tests.ps1` をローカル実行

---

### アーキテクチャを理解したい場合

1. ✅ **[アーキテクチャ理解ガイド](architecture_guide.md)** を読む（15分）
2. ✅ **[技術的発見事項](technical_discoveries.md)** を読む（10分）

---

## 📊 Week 3 Priority 1実績サマリー

このベストプラクティス集の基盤となった実績：

| 項目 | 実績 |
|------|------|
| **実施期間** | 2025-11-15（Day 0～2, 3日間） |
| **実施時間** | 8時間（Day 0: 2.5h, Day 1: 2.5h, Day 2: 3h） |
| **実装テスト数** | 17テスト |
| **テスト成功率** | 100% (17/17 Passed) |
| **GitHub Push** | 5回（Day 0: 1回, Day 1: 2回, Day 2: 2回） |
| **発見した課題** | 6件（全て解決済み） |
| **技術的発見** | 10以上（全て記録済み） |

### 実装テスト内訳

- **IT-001**: PDF読み込み統合テスト（5テスト）
  - サービスレイヤーテスト: 3テスト
  - EditorServiceテスト: 2テスト
- **IT-002**: ページ操作統合テスト（8テスト）
  - ページ削除: 3テスト（先頭・中間・最終）
  - ページ回転: 3テスト（90°・180°・270°）
  - ページ並び替え: 2テスト（全ページ逆順・特定ページ入れ替え）
- **IT-003**: PDF保存統合テスト（4テスト）
  - 基本保存・削除後・回転後・並び替え後

---

## 🎯 このドキュメントの使い方

### 実装前（計画フェーズ）

1. [実装者向けメッセージ](implementer_message.md) を読む
2. [テスト戦略ガイド](strategy_guide.md) でテスト設計を学ぶ
3. [アーキテクチャ理解ガイド](architecture_guide.md) でアーキテクチャを理解

### 実装中（コーディングフェーズ）

1. [テストフレームワーク実践ガイド](framework_guide.md) を参照しながら実装
2. 既存テスト（IT001/IT002/IT003）をテンプレートとして活用
3. エラー発生時は [よくある失敗パターン](common_failures.md) を参照

### CI/CD設定時

1. [CI/CDベストプラクティス](cicd_guide.md) を参照
2. 既存の `.github/workflows/integration-tests.yml` をベースに設定

### 実装後（レビューフェーズ）

1. [技術的発見事項](technical_discoveries.md) で新しい発見を記録
2. [よくある失敗パターン](common_failures.md) に新しい課題を追加（必要に応じて）

---

## 🔄 このドキュメントの更新方針

### 更新タイミング

- 新しい技術的課題が発見されたとき
- 新しいベストプラクティスが確立されたとき
- Week 3 Priority 2/3が完了したとき

### 更新責任者

- Week 3 Priority実施担当（Claude）
- DocOrganizer統合テスト実装者

### 更新プロセス

1. 新しい発見を記録（.tmpフォルダに一時記録）
2. ベストプラクティスとして体系化
3. 該当ドキュメントに追記
4. README.mdの更新履歴に記録

---

## 📝 更新履歴

| 日付 | 更新内容 | 更新者 |
|------|---------|--------|
| 2025-11-15 | 初版作成（Week 3 Priority 1 Day 0～2成果ベース） | Claude (Week 3 Priority 1実施担当) |

---

## 📞 フィードバック・質問

このベストプラクティス集に関するフィードバック・質問は：

- GitHub Issues: [DocOrganizer/issues](https://github.com/Rih0z/DocOrganizer/issues)
- Week 3 Priority 1実施担当: Claude

---

**最終更新**: 2025-11-15
**バージョン**: 1.0（Week 3 Priority 1 Day 0～2ベース）
**ステータス**: ✅ 初版完成
