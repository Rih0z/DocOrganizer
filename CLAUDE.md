# Claude.md - DocOrganizer AIコーディング原則

```yaml
ai_coding_principles:
  version: "3.0"
  last_updated: "2025-09-04"
  project: "DocOrganizer - CubePDF Utility互換 汎用PDF編集ツール"
  current_version: "3.0.031"
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
- **バージョン**: V3.0.031
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
| V3.0.031 | 2025-09-03 | PDF表示バグ完全修正 |
| V3.0.030 | 2025-09-03 | PdfiumViewerエンジン採用 |
| V3.0.028 | 2025-09-03 | GhostScript依存排除 |

[完全な履歴はdocs/rule/version_management.mdを参照]