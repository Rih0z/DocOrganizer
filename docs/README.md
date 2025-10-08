# DocOrganizer V3 ドキュメント索引

**最終更新**: 2025-10-07
**対象バージョン**: V3.0.125

> **🎯 3分で理解**: このドキュメントはDocOrganizer V3の全技術資料への統一アクセスポイントです

---

## 📚 ドキュメント構造

```
docs/
├── architecture/      # システムアーキテクチャ（4ファイル）
├── guides/           # 運用ガイド（5ファイル）
├── reports/          # バージョン別レポート（V3.0.101〜V3.0.125）
├── rule/             # 開発規約（4ファイル）
└── archive/          # 過去のレポート（月別整理）
    ├── 2025-10/      # 10月の分析レポート
    ├── 2025-09/      # 9月のバグ修正レポート
    └── 2025-08/      # 8月の初期実装レポート
```

---

## 🚀 クイックスタート

### 新規参加者
1. **[rule/project_structure.md](rule/project_structure.md)** - プロジェクト全体像
2. **[architecture/V3_COMPLETE_ARCHITECTURE.md](architecture/V3_COMPLETE_ARCHITECTURE.md)** - V3アーキテクチャ
3. **[rule/development_principles.md](rule/development_principles.md)** - 開発原則

### 機能実装時
1. **[architecture/](architecture/)** - システム設計確認
2. **[reports/](reports/)** - 最新実装パターン参照
3. **[CLAUDE.md](../CLAUDE.md)** - AI開発原則遵守

### バグ修正時
1. **[archive/](archive/)** - 類似問題の解決事例検索
2. **tmpフォルダ** - 分析レポート作成（[CLAUDE.md 第15条](../CLAUDE.md)）
3. **[rule/debug_logging_system.md](rule/debug_logging_system.md)** - ログ出力

---

## 📂 主要ドキュメント

### 🏗️ [architecture/](architecture/) - アーキテクチャ文書
| ファイル | 内容 |
|---------|------|
| [V3_COMPLETE_ARCHITECTURE.md](architecture/V3_COMPLETE_ARCHITECTURE.md) | V3完全アーキテクチャ解説 |
| [V3_ARCHITECTURE_IMAGE_DISPLAY.md](architecture/V3_ARCHITECTURE_IMAGE_DISPLAY.md) | 画像表示システム詳細 |
| [V3_ROTATION_AND_IMAGE_REPLACEMENT.md](architecture/V3_ROTATION_AND_IMAGE_REPLACEMENT.md) | 回転・画像置換機能 |
| [drag_drop_architecture_analysis.md](architecture/drag_drop_architecture_analysis.md) | ドラッグ&ドロップ技術分析 |

### 📖 [guides/](guides/) - 運用ガイド
| ファイル | 内容 |
|---------|------|
| [environment_variables.md](guides/environment_variables.md) | 環境変数設定 |
| [heic_support_guide.md](guides/heic_support_guide.md) | HEIC画像対応 |
| [pdf_save_guide.md](guides/pdf_save_guide.md) | PDF保存機能 |
| [ghostscript_free_implementation.md](guides/ghostscript_free_implementation.md) | Ghostscript依存削除 |
| [ghostscript_free_solution.md](guides/ghostscript_free_solution.md) | Ghostscript不要化 |

### 📏 [rule/](rule/) - 開発規約
| ファイル | 内容 |
|---------|------|
| [project_structure.md](rule/project_structure.md) | プロジェクト構造・技術スタック |
| [version_management.md](rule/version_management.md) | バージョン管理手順 |
| [debug_logging_system.md](rule/debug_logging_system.md) | デバッグログシステム |
| [development_principles.md](rule/development_principles.md) | 開発原則 |

---

## 🆕 バージョン別レポート

### [reports/](reports/) - V3.0.101以降

#### 主要バージョン（重要な修正）
| バージョン | 内容 | レポート |
|-----------|------|---------|
| **V3.0.125** | ドラッグ自動スクロール | [📁 v3.0.125/](reports/v3.0.125/) |
| **V3.0.124** | - | [📁 v3.0.124/](reports/v3.0.124/) |
| **V3.0.123** | 複数選択移動バグ修正 | [📁 v3.0.123/](reports/v3.0.123/) |
| **V3.0.122** | 複数選択UI修正 | [📁 v3.0.122/](reports/v3.0.122/) |
| **V3.0.110** | ズーム機能完全修正 | [v3.0.110_zoom_feature_fix.md](reports/v3.0.110_zoom_feature_fix.md) |
| **V3.0.103** | 複数選択バグ完全修正 | [v3.0.103_multiple_selection_fix.md](reports/v3.0.103_multiple_selection_fix.md) |
| **V3.0.101** | 回転プレビュー同期修正 | [v3.0.101_rotation_preview_fix.md](reports/v3.0.101_rotation_preview_fix.md) |

#### バージョンフォルダ構成
```
reports/
├── v3.0.125/          # V3.0.125 実行ログ・分析
├── v3.0.124/          # V3.0.124 実行ログ
├── v3.0.123/          # V3.0.123 バグ分析・実行ログ・検証
├── v3.0.122/          # V3.0.122 実行ログ・分析
├── v3.0.110_zoom_feature_fix.md
├── v3.0.103_multiple_selection_fix.md
└── v3.0.101_rotation_preview_fix.md
```

---

## 📁 アーカイブ

### [archive/2025-10/](archive/2025-10/) - 2025年10月
**ドキュメント整理・検証プロジェクト完了**
- docs徹底的整理（60件以上を5階層構造化）
- project_structure.md完全書き換え（V3.0.031→V3.0.123）
- ソースコード完全分析（21バージョン）
- ファクトチェック（92.5%正確度確認）
- V3.0.117〜V3.0.120 複数選択関連バグ分析

**主要ファイル**:
- [final_verification_report_20251006.md](archive/2025-10/final_verification_report_20251006.md) - 最終検証レポート
- [docs_reorganization_complete_report_20251006.md](archive/2025-10/docs_reorganization_complete_report_20251006.md) - 整理完了報告
- [source_code_analysis_complete_20251006.md](archive/2025-10/source_code_analysis_complete_20251006.md) - ソースコード分析
- [fact_check_report_20251006.md](archive/2025-10/fact_check_report_20251006.md) - ファクトチェック

### [archive/2025-09/](archive/2025-09/) - 2025年9月
**バグ修正・機能追加集中期間**
- V3.0.088〜V3.0.114のバグ修正
- 回転プレビュー同期修正シリーズ
- 複数選択バグ修正シリーズ
- Undo/Redo完全実装
- パフォーマンス最適化
- デバッグログシステム実装

### [archive/2025-08/](archive/2025-08/) - 2025年8月
**初期V3実装プロジェクト**
- ドラッグ&ドロップ並び替え機能実装
- UI拡大機能バグ修正
- HEIC PDF出力バグ修正
- Ghostscript依存削除実装
- 初期アーキテクチャ構築

---

## 🎯 主要バージョン履歴（抜粋）

| バージョン | 日付 | 主な変更 |
|-----------|------|----------|
| V3.0.125 | 2025-10-07 | ドラッグ自動スクロール機能 |
| V3.0.123 | 2025-10-06 | 複数選択移動バグ完全修正 |
| V3.0.117 | 2025-10-02 | 複数ページ一括移動完全実装 |
| V3.0.110 | 2025-09-22 | ズーム機能完全修正 |
| V3.0.103 | 2025-09-18 | 複数選択バグ完全修正 |
| V3.0.068 | 2025-09-10 | Undo/Redo完全実装 |
| V3.0.064 | 2025-09-04 | 統一ログ管理システム |
| V3.0.030 | 2025-09-03 | PdfiumViewerエンジン採用 |

[完全な履歴は [CLAUDE.md](../CLAUDE.md) を参照]

---

## 📞 ドキュメント管理ルール

### 新規レポート作成時
1. **分析**: tmpフォルダで実施
2. **完了後**: `docs/archive/YYYY-MM/` に移動
3. **重要な修正**: `docs/reports/vX.X.XXX/` にバージョン別フォルダ作成
4. **README更新**: このファイルにリンク追加

### アーカイブ基準
- **最新レポート**: `reports/` に保持
- **古いレポート**: `archive/YYYY-MM/` に月別移動
- **一時ファイル**: 完了後にarchiveまたは削除

### 品質基準
- **CLAUDE.md準拠**: 第1条〜第17条遵守
- **包括性**: 技術詳細・実装事例・学習事項を網羅
- **追跡可能性**: アーカイブで履歴管理

---

**🔗 関連リンク**:
- [プロジェクトルート README](../README.md)
- [CLAUDE.md - AI開発原則](../CLAUDE.md)
- [GitHub リポジトリ](https://github.com/Rih0z/DocOrganizer)
