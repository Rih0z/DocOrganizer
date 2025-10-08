# Claude.md - DocOrganizer AIコーディング原則

```yaml
ai_coding_principles:
  version: "3.0"
  last_updated: "2025-10-07"
  project: "DocOrganizer - CubePDF Utility互換 汎用PDF編集ツール"
  current_version: "3.0.125"
```

## ⚠️ 必須宣言事項

**全てのコーディング作業開始時に必ず以下の宣言を完全に実行すること - これは絶対的な要件である：**

### 【必ず全ての原則を宣言してから作業開始】

## 第1条: AI原則宣言
**常に思考開始前にCLAUDE.mdの第一条から第十七条のAIコーディング原則を全て宣言してから実施する**

## 第2条: プロフェッショナリズム
**常にプロの世界最高エンジニアとして対応する**

## 第3条: 実装品質
**モックや仮のコード、ハードコードを一切禁止する。Serenaツールを使用してセマンティックなコード理解と編集を行う。新規機能の実装指示を受けたら、まずはtmpフォルダ以下に実装計画を作成する**

## 第4条: エンタープライズレベル
**エンタープライズレベルの実装を実施し、修正は表面的ではなく、全体のアーキテクチャを意識して実施する**

## 第5条: 問題解決
**問題に詰まったら、まずCLAUDE.mdやプロジェクトドキュメント内に解決策がないか確認する**

## 第6条: スクリプト管理
**不要なスクリプトは増やさない。スクリプト作成時は常に既存のスクリプトで使用可能なものがないか確認する**

## 第7条: 段階的実装
**段階的実装を徹底する。完璧を求めず、動作する最小限の機能から始めて、継続的に改善する**

## 第8条: デザイン準拠
**デザインはhttps://atlassian.design/components を読み込み、これに準拠する**

## 第9条: プロジェクト管理
**ビルドの度に新しいフォルダを作成しない。既存のプロジェクトフォルダを更新し続ける**

## 第10条: ファイル構造
**Mac・Windows両環境でプロフェッショナルなファイル構造を維持する**

## 第11条: Git同期
**修正を行ったら必ずgit pullでディレクトリを同期する**

## 第12条: ビルド前確認
**全ての作業開始前にWindows環境での再ビルドを必ず実行する**

## 第13条: 完全実行
**修正を行ったら必ずビルドまで完全実行し、最終的なEXEの完全パスを出力する**

## 第14条: 起動方法
**Windowsアプリケーションはエクスプローラーから直接起動する。管理者権限での起動は厳禁**

## 第15条: バグ修正プロセス
**バグを修正する場合は、serena mcpを利用して原因の分析をし、tmpフォルダ以下に報告資料を作成する。ユーザーに原因について報告し、確認後に修正を実施する**

## 第16条: ログ管理
**デバッグ用ログ出力は統一DebugLoggerクラスを使用する。環境変数でデバッグモードのON/OFFを制御し、ログは.logs/debug.logに出力する**

## 第17条: バージョン管理
**ビルド実行時は必ずバージョン管理システムに従い、現在のバージョン番号を確認し、最後の桁を1増加させてからCLAUDE.md・MainWindow.xaml・AssemblyVersionを更新する**

---

## 📋 作業前必須手順

### 1. Git同期
```bash
git pull origin main
```

### 2. Windowsビルド

#### デフォルト: release-debugビルド（デバッグログ有効）
```bash
cd C:\Users\217216X721451\github\DocOrganizer
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release-debug
```

#### リリース版ビルド（ユーザーから明示的指示がある場合のみ）
```bash
# ログ無効版
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

### 3. 成功確認
- release-debug\DocOrganizer.exe の生成確認（デフォルト）
- release\DocOrganizer.exe の生成確認（リリース版指示時）
- エクスプローラーから起動テスト

---

## ⚡ クイックリファレンス

### 現在の情報
- **バージョン**: V3.0.125
- **GitHub**: https://github.com/Rih0z/DocOrganizer
- **デフォルトEXE**: `C:\Users\217216X721451\github\DocOrganizer\release-debug\DocOrganizer.exe`
- **リリースEXE**: `C:\Users\217216X721451\github\DocOrganizer\release\DocOrganizer.exe`（明示的指示時）

### デバッグ設定
```powershell
# 環境変数
$env:DOCORGANIZER_DEBUG = "true"   # デバッグON
$env:DOCORGANIZER_DEBUG = "false"  # デバッグOFF
```

### 起動スクリプト
```bash
release\run-debug.bat      # デバッグモード
release\run-production.bat  # 本番モード
```

---

## 📚 詳細ドキュメント

### 開発関連
- **[プロジェクト構造](docs/rule/project_structure.md)** - ディレクトリ構成・技術スタック
- **[バージョン管理詳細](docs/rule/version_management.md)** - バージョン更新手順詳細
- **[デバッグログ詳細](docs/rule/debug_logging_system.md)** - ログ出力システム詳細

### アーキテクチャ
- [V3完全アーキテクチャ](docs/V3_COMPLETE_ARCHITECTURE.md)
- [画像表示システム](docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md)

### 最新実装
- [ズーム機能バグ修正完全報告](docs/Zoom_Feature_Bug_Fix_Complete_Report_20250922.md)
- [複数選択バグ修正完全報告](docs/Multiple_Selection_Bug_Fix_Complete_Project_Report_20250918.md)
- [回転プレビュー同期完全修正](docs/Rotation_Preview_Complete_Fix_V3.0.101_Report_20250912.md)
- [パフォーマンス最適化完全報告](docs/V3_Performance_Optimization_Complete_Report_20250911.md)
- [ページ移動機能バグ修正](docs/Page_Movement_Bug_Fix_Complete_Report_20250909.md)
- [UIボタン・アイコンサイズ拡大](docs/UI_Button_Icon_Size_Enhancement_Report_20250905.md)
- [PDFサムネイル表示バグ修正](docs/BugFix_PDF_Thumbnail_Display_Report_20250904.md)
- [統一ログ管理システム](docs/Debug_Logging_System_Complete_Report_20250904.md)
- [ドラッグ&ドロップ実装](docs/V3_Drag_Drop_Complete_Implementation_Report_20250822.md)

---

## 🚨 重要な注意事項

1. **管理者権限で起動しない** - ドラッグ&ドロップが無効になる
2. **ビルド時はバージョン更新** - 第17条に従う
3. **ログはDebugLogger使用** - ハードコード禁止
4. **Git同期を忘れずに** - 作業前後で必ず実行
5. **必ず全原則を宣言** - 第1条〜第17条を確認してから作業

---

## 📋 最新バージョン履歴

| バージョン | 日付 | 主な変更 |
|-----------|------|----------|
| V3.0.125 | 2025-10-07 | ドラッグ中自動スクロール機能実装・1px/イベント最低速度で最大コントロール性実現 |
| V3.0.124 | 2025-10-06 | 画像余白自動削除機能をオプション化・デフォルト無効でV3.0.110以前の動作に復帰/PDF出力も余白保持 |
| V3.0.123 | 2025-10-06 | 複数選択移動バグ完全修正・相対位置保持ロジック削除/処理順序最適化でV3.0.117機能実現 |
| V3.0.122 | 2025-10-06 | 複数選択時上下移動ボタン有効化・V3.0.117実装のUI制御修正 |
| V3.0.121 | 2025-10-06 | 複数選択完全修正・二重バインディングループ削除で3枚以上選択安定化 |
| V3.0.120 | 2025-10-02 | 複数選択修正V2・干渉ゼロのシンプルロジックで標準選択メカニズム完全保持 |
| V3.0.119 | 2025-10-02 | ❌失敗: Ctrl/Shift早期リターンで複数選択機能自体が破壊 |
| V3.0.118 | 2025-10-02 | ❌失敗: ListBoxItemレベルBehaviorで選択完全破壊（緊急ロールバック実施） |
| V3.0.117 | 2025-10-02 | 複数選択一括移動完全実装・上下移動ボタン複数対応/ドラッグ時選択保護 |
| V3.0.116 | 2025-10-02 | 複数ページドラッグ&ドロップ実装・V3DragInfo複数選択対応 |
| V3.0.114 | 2025-09-23 | 横向き画像PDF出力修正・画像の向きに応じてページ向き自動決定（情報削除防止） |
| V3.0.113 | 2025-09-23 | PDFページサイズを画像サイズに完全一致・A4固定を廃止し余白完全排除 |
| V3.0.112 | 2025-09-23 | PDF出力余白完全削除・CalculateDrawingRectangle修正でWYSIWYG実現（プレビューとPDF完全一致） |
| V3.0.111 | 2025-09-23 | 画像余白自動削除機能実装・Magick.NET Trim()で可視域最大化（余白は絶対に必要なし） |
| V3.0.110 | 2025-09-22 | ズーム機能完全修正・ScaleTransform実装でプレビュー拡大/縮小実現 |
| V3.0.103 | 2025-09-18 | 複数選択バグ完全修正・ControlTemplate削除でCtrl/Shift選択実現 |
| V3.0.102 | 2025-09-18 | 複数選択バグ修正試行・単一選択強制コード削除 |
| V3.0.101 | 2025-09-12 | 回転プレビュー同期完全修正・forceUpdate時のPreviewImage再生成 |
| V3.0.100 | 2025-09-12 | 回転プレビュー同期修正試行・OnPageRotated強制更新 |
| V3.0.099 | 2025-09-12 | 回転プレビュー同期修正試行・UpdateFromModelAsync修正 |
| V3.0.094 | 2025-09-12 | 回転プレビュー同期修正試行・_isRotatingPageフラグ追加 |
| V3.0.073 | 2025-09-11 | パフォーマンス最適化・ViewModelの再利用実装 |
| V3.0.071 | 2025-09-10 | 回転→削除→Undoバグ修正・CubePDF互換ショートカット |
| V3.0.068 | 2025-09-10 | Undo/Redo完全実装・単一ファイル配布最適化 |
| V3.0.050 | 2025-09-09 | ページ移動2段階ジャンプ完全修正・単一EXE起動問題解決 |
| V3.0.031 | 2025-09-03 | PDF表示バグ完全修正 |
| V3.0.030 | 2025-09-03 | PdfiumViewerエンジン採用 |

[完全な履歴はdocs/rule/version_management.mdを参照]
- ステップ5: バグ修正・機能追加 実行・進捗管理プロンプト

あなたは実行管理の専門家です。承認されたバグ修正または機能追加の計画に従って実行を管理し、進捗を記録してください。

## 実行対象
- tmpフォルダ内の承認された計画
- docsフォルダ内の実装ガイドライン・コーディング規約
- 既存システムの運用マニュアル

## 指示
計画を段階的に実行し、各ステップの結果を記録してください：

### 1. 実行前準備
- 必要なリソースの確保
- 関係者への周知
- 開始基準の確認

### 2. 段階的実行
- 計画通りの順序で実行
- 各ステップの完了確認
- 問題発生時の対処

### 3. 品質確認
- 成功基準に基づく検証
- 期待通りの結果になっているか
- 追加の調整が必要か

### 4. 進捗記録
- 実行した内容
- 発生した問題と対処法
- 計画との差異

## 実行ログの項目
- **日時**: いつ実行したか
- **作業内容**: 何を実行したか
- **結果**: どうなったか
- **問題**: 何か問題があったか
- **対処**: どう対処したか
- **次のアクション**: 次に何をするか

## 出力ファイル
- `tmp/execution_log_[現在日時].md`
- リアルタイムで更新
- 完了時に最終レポート追加

## 完了基準
- 全ての計画項目が完了
- 品質基準を満たしている
- 関係者の確認・承認を得ている

## 注意点
- 計画からの逸脱は記録と承認を得る
- 問題は隠さずに報告
- 学習事項も記録する
- Ultrathink. Don't hold back. give it your all！