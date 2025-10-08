# docsフォルダ整理分析レポート

**作成日**: 2025-10-06
**分析者**: Serena MCP + Claude
**対象**: C:\Users\217216X721451\github\DocOrganizer\docs

---

## 1. 現状の問題点

### 1.1 ファイル配置の混乱
- **ルートレベルに60件以上のファイルが散在**
- バグ修正レポート、実行ログ、アーキテクチャ文書が混在
- 命名規則が統一されていない（日英混在、日付フォーマット不統一）

### 1.2 archiveフォルダの不完全性
```
docs/
  archive/
    ui_zoom_fix_20250821/
    v3_025_drag_drop_implementation_20250822/
  ← 他の古いレポートはarchive化されていない
```

### 1.3 ruleフォルダの不足
```
docs/rule/
  debug_logging_system.md
  development_principles.md
  project_structure.md
  version_management.md
  ← アーキテクチャ文書がruleに含まれていない
```

---

## 2. 現在のファイル分類

### A. アーキテクチャ文書（常に参照）
- V3_COMPLETE_ARCHITECTURE.md
- V3_ARCHITECTURE_IMAGE_DISPLAY.md
- V3_ROTATION_AND_IMAGE_REPLACEMENT.md
- V3_サムネイルドラッグドロップ問題_アーキテクチャ分析_20250822.md

### B. 運用ガイド（現役）
- ENVIRONMENT_VARIABLES.md
- HEIC_Support_Complete_Guide.md
- PDF保存機能使用ガイド.md
- GHOSTSCRIPT_FREE_IMPLEMENTATION_COMPLETE_20250822.md

### C. 最新のバグ修正レポート（V3.0.110以降）
- Zoom_Feature_Bug_Fix_Complete_Report_20250922.md (V3.0.110)
- Multiple_Selection_Bug_Fix_Complete_Project_Report_20250918.md (V3.0.103)
- Rotation_Preview_Complete_Fix_V3.0.101_Report_20250912.md (V3.0.101)

### D. 古いバグ修正レポート（V3.0.100未満）→archive対象
- Horizontal_Image_PDF_Fix_Complete_Report_V3.0.114_20250923.md
- Multiple_Selection_Bug_Fix_V3.0.102_Report_20250918.md
- Multiple_Selection_Complete_Fix_V3.0.103_Report_20250918.md
- Rotation_Preview_Sync_Bug_Fix_V3.0.088_Report_20250911.md
- 他多数...

### E. 一時作業ログ→archive対象
- execution_log_20250822_1822.md
- execution_log_20250919.md
- serena_analysis_plan_keyboard_rotation_20250918.md
- auto_analysis_20250822_1815.md
- phase2_execution_completion_log_20250822_1807.md
- phase4_completion.txt

---

## 3. 推奨ディレクトリ構造

```
docs/
├── README.md                    # docs全体の案内・最新情報へのリンク
│
├── architecture/                # 【新設】アーキテクチャ文書
│   ├── V3_COMPLETE_ARCHITECTURE.md
│   ├── V3_ARCHITECTURE_IMAGE_DISPLAY.md
│   ├── V3_ROTATION_AND_IMAGE_REPLACEMENT.md
│   └── drag_drop_architecture_analysis.md
│
├── guides/                      # 【新設】運用ガイド・チュートリアル
│   ├── environment_variables.md
│   ├── heic_support_guide.md
│   ├── pdf_save_guide.md
│   └── ghostscript_free_implementation.md
│
├── reports/                     # 【新設】最新の重要レポート（直近3バージョン程度）
│   ├── v3.0.122_multiple_selection_move_fix.md
│   ├── v3.0.110_zoom_feature_fix.md
│   └── v3.0.103_multiple_selection_fix.md
│
├── rule/                        # 【既存】開発規約
│   ├── debug_logging_system.md
│   ├── development_principles.md
│   ├── project_structure.md
│   └── version_management.md
│
└── archive/                     # 【既存】過去のレポート・作業ログ
    ├── 2025-09/                 # 月別整理
    │   ├── v3.0.0xx_reports/
    │   └── execution_logs/
    ├── 2025-08/
    │   ├── ui_zoom_fix_20250821/
    │   └── v3_025_drag_drop_implementation_20250822/
    └── README.md                # archive内の検索ガイド
```

---

## 4. 実装との差分確認項目

### 4.1 CLAUDE.mdバージョン履歴との整合性
- **CLAUDE.md記載**: V3.0.123（最新）
- **docs内の最新レポート**: V3.0.122? 確認必要

### 4.2 project_structure.mdとの整合性
確認項目：
- ディレクトリ構成が現状と一致しているか
- release-debug/releaseフォルダ構成が正しいか
- ログ出力パス（.logs/debug.log）が正しいか

### 4.3 実際のソースコードとの整合性
確認対象：
- src/DocOrganizer.Core/Commands/MovePagesCommand.cs
- src/DocOrganizer.UI/Behaviors/V3AdvancedDragDropBehavior.cs
- src/DocOrganizer.UI/ViewModels/V3/DragDropHandlerViewModel.cs

---

## 5. 次のアクション

### Step 1: 実装確認
- [ ] project_structure.mdを読み込み
- [ ] 主要なソースコード（git status）の実装を確認
- [ ] V3.0.123の変更内容をレポート化（未作成なら）

### Step 2: 差分修正
- [ ] project_structure.mdの更新（実装と不一致があれば）
- [ ] アーキテクチャ文書の更新（V3.0.117以降の変更反映）

### Step 3: ファイル整理実行
- [ ] 新しいディレクトリ構造作成
- [ ] ファイル移動・リネーム
- [ ] README.md更新

---

## 6. 承認待ち事項

**整理方針について確認が必要です：**

1. **archive基準**: V3.0.100未満のレポートをarchive化してよいか？
2. **削除対象**: execution_log/phase_completionなどの一時ファイルは削除してよいか？
3. **リネーム**: 日本語ファイル名を英語に統一するか？
4. **最新レポート**: V3.0.123の変更内容レポートを作成すべきか？

**ユーザーの指示を待ちます。**
