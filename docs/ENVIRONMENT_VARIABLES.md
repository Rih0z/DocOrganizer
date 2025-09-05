# DocOrganizer環境変数リファレンス

**最終更新**: 2025-09-04  
**バージョン**: V3.0.031  
**ドキュメント種別**: システム設定・環境変数管理

---

## 📋 概要

DocOrganizerは環境変数による柔軟な設定制御をサポートしています。本ドキュメントでは、全ての環境変数の定義、用途、デフォルト値を詳細に説明します。

### 命名規則
- **標準プレフィックス**: `DOCORGANIZER_*`
- **区切り文字**: アンダースコア (`_`)
- **値の形式**: 大文字小文字を区別（`true`/`false`等）

---

## 📝 環境変数一覧（完全版）

### ログ関連
- `DOCORGANIZER_DEBUG` - デバッグログ出力制御（true/false）
- `DOCORGANIZER_LOG_PATH` - ログ出力ディレクトリ
- `DOCORGANIZER_LOG_DEBUG` - デバッグログファイル名
- `DOCORGANIZER_LOG_STARTUP` - 起動ログファイル名
- `DOCORGANIZER_LOG_ERROR` - エラーログファイル名

### OCR関連
- `DOCORGANIZER_OCR_ENABLED` - OCR機能有効化（true/false）
- `DOCORGANIZER_OCR_PATH` - OCRデータパス

### バージョン
- `DOCORGANIZER_VERSION` - バージョンオーバーライド

### レガシー（非推奨）
- `GS_BIN_PATH` - GhostScript実行パス（V3.0.028以降未使用）
- `GHOSTSCRIPT_BIN` - GhostScript実行パス（V3.0.028以降未使用）

---

## 🔧 デバッグ・ログ制御

### DOCORGANIZER_DEBUG
```bash
# デバッグモードの有効/無効制御
export DOCORGANIZER_DEBUG="true"   # デバッグON
export DOCORGANIZER_DEBUG="false"  # デバッグOFF（デフォルト）
```

**用途**: アプリケーション全体のデバッグログ出力制御  
**デフォルト値**: `false`  
**実装場所**: `src/DocOrganizer.Core/Logging/DebugLogger.cs:91`  
**影響**: 
- デバッグログファイルの出力ON/OFF
- パフォーマンス情報の表示制御
- 詳細エラー情報の出力制御

### DOCORGANIZER_LOG_PATH
```bash
# ログファイルの出力先パス指定
export DOCORGANIZER_LOG_PATH="/custom/path/debug.log"
export DOCORGANIZER_LOG_PATH=".logs/custom.log"  # 相対パス
```

**用途**: デバッグログファイルの出力先パス指定  
**デフォルト値**: `.logs/debug.log`  
**実装場所**: `src/DocOrganizer.Core/Logging/DebugLogger.cs:174`  
**注意事項**:
- 絶対パス・相対パスの両方をサポート
- 指定したディレクトリが存在しない場合は自動作成
- 書き込み権限が必要

---

## 🔍 OCR機能制御

### DOCORGANIZER_OCR_ENABLED
```bash
# OCR（光学文字認識）機能の有効化
export DOCORGANIZER_OCR_ENABLED="true"   # OCR機能ON
export DOCORGANIZER_OCR_ENABLED="false"  # OCR機能OFF（デフォルト）
```

**用途**: OCR機能の有効/無効制御  
**デフォルト値**: `false`  
**実装場所**: `src/DocOrganizer.Core/Config/OcrConfig.cs:15`  
**依存関係**: 
- OCRライブラリのインストールが必要
- 追加のシステムリソースを消費

### DOCORGANIZER_OCR_PATH
```bash
# OCRデータファイルの配置パス
export DOCORGANIZER_OCR_PATH="/path/to/ocr/data"
export DOCORGANIZER_OCR_PATH=".ocr"  # デフォルト（相対パス）
```

**用途**: OCR処理で使用する言語データファイルの配置パス  
**デフォルト値**: `.ocr`  
**実装場所**: `src/DocOrganizer.Core/Config/OcrConfig.cs:21`  
**推奨構成**:
```
.ocr/
├── jpn.traineddata    # 日本語
├── eng.traineddata    # 英語
└── osd.traineddata    # 方向・スクリプト検出
```

---

## 📄 PDF処理制御

### DOCORGANIZER_PDF_CACHE_SIZE
```bash
# PDFキャッシュサイズ（MB単位）
export DOCORGANIZER_PDF_CACHE_SIZE="200"  # 200MB
export DOCORGANIZER_PDF_CACHE_SIZE="50"   # 低メモリ環境用
```

**用途**: PDF処理時のメモリキャッシュサイズ制御  
**デフォルト値**: `100` (MB)  
**推奨値**:
- **低スペック環境**: 50MB
- **標準環境**: 100MB  
- **高スペック環境**: 200MB以上

### DOCORGANIZER_PDF_QUALITY
```bash
# PDF出力品質設定
export DOCORGANIZER_PDF_QUALITY="High"    # 高品質（デフォルト）
export DOCORGANIZER_PDF_QUALITY="Medium"  # 中品質
export DOCORGANIZER_PDF_QUALITY="Low"     # 低品質（高速）
```

**用途**: PDF出力・変換時の品質レベル制御  
**デフォルト値**: `High`  
**品質レベル**:
- **High**: 300DPI, 最高画質, サイズ大
- **Medium**: 150DPI, バランス型
- **Low**: 72DPI, 最小サイズ, 高速処理

---

## 🔧 外部ツール統合（非推奨・後方互換）

### GS_BIN_PATH / GHOSTSCRIPT_BIN (⚠️ 非推奨)
```bash
# 旧GhostScript統合（後方互換のみ）
export GS_BIN_PATH="/usr/bin/gs"
export GHOSTSCRIPT_BIN="/opt/ghostscript/bin/gs"

# 新統一名（推奨）
export DOCORGANIZER_GS_PATH="/usr/bin/gs"
```

**用途**: GhostScriptバイナリパス指定（後方互換性のみ）  
**実装場所**: `src/DocOrganizer.Infrastructure/Services/ImageProcessingService.cs:1139-1140`  
**移行状況**: 
- ✅ V3.0.031で内蔵PDF処理エンジンに完全移行済み
- ⚠️ 6ヶ月間の後方互換サポート継続
- ➡️ `DOCORGANIZER_GS_PATH`への統一移行推奨

---

## 🚀 パフォーマンス最適化

### DOCORGANIZER_THREAD_COUNT
```bash
# 並列処理スレッド数制御
export DOCORGANIZER_THREAD_COUNT="4"   # 4並列
export DOCORGANIZER_THREAD_COUNT="auto" # 自動（CPU数に基づく）
```

**用途**: 画像処理・PDF変換の並列処理数制御  
**デフォルト値**: `auto` (CPU論理コア数)  
**推奨設定**: CPUコア数の50-100%

### DOCORGANIZER_MEMORY_LIMIT
```bash
# メモリ使用量制限（MB）
export DOCORGANIZER_MEMORY_LIMIT="1024"  # 1GB制限
export DOCORGANIZER_MEMORY_LIMIT="2048"  # 2GB制限
```

**用途**: アプリケーションの最大メモリ使用量制御  
**デフォルト値**: `1024` (1GB)  
**注意**: 制限値を超えた場合、一時的な処理停止が発生する可能性

---

## 📱 起動・実行時設定

### 起動スクリプトによる設定
```bash
# デバッグモード起動
./release/run-debug.bat

# 本番モード起動  
./release/run-production.bat

# カスタム環境変数で起動
DOCORGANIZER_DEBUG=true DOCORGANIZER_LOG_PATH="./custom.log" ./DocOrganizer.exe
```

### PowerShellでの設定例
```powershell
# 環境変数設定
$env:DOCORGANIZER_DEBUG = "true"
$env:DOCORGANIZER_LOG_PATH = "C:\Logs\DocOrganizer.log"
$env:DOCORGANIZER_OCR_ENABLED = "false"

# アプリケーション起動
.\DocOrganizer.exe
```

### Windowsバッチファイルでの設定例
```batch
@echo off
set DOCORGANIZER_DEBUG=true
set DOCORGANIZER_LOG_PATH=.logs\debug.log
set DOCORGANIZER_PDF_QUALITY=High
DocOrganizer.exe
```

---

## 🔍 トラブルシューティング

### 環境変数が反映されない場合

1. **設定確認**:
```bash
echo $DOCORGANIZER_DEBUG  # Linux/Mac
echo %DOCORGANIZER_DEBUG%  # Windows Command Prompt
$env:DOCORGANIZER_DEBUG    # PowerShell
```

2. **アプリケーション再起動**: 環境変数変更後は必ずアプリケーションを再起動

3. **権限確認**: ログファイルパスへの書き込み権限を確認

4. **パス確認**: 相対パス・絶対パスの記述形式を確認

### よくある問題と解決策

| 問題 | 原因 | 解決策 |
|------|------|--------|
| ログが出力されない | DEBUGが無効 | `DOCORGANIZER_DEBUG=true`を設定 |
| OCR機能が動作しない | データファイル不足 | OCRデータファイルを`.ocr/`に配置 |
| メモリ不足エラー | キャッシュサイズ過大 | `DOCORGANIZER_PDF_CACHE_SIZE`を削減 |
| 処理速度が遅い | 品質設定が高すぎる | `DOCORGANIZER_PDF_QUALITY=Medium`に変更 |

---

## 📚 関連ドキュメント

- **[デバッグログシステム詳細](Debug_Logging_System_Complete_Report_20250904.md)** - ログ管理の詳細仕様
- **[バージョン管理詳細](rule/version_management.md)** - バージョン更新プロセス  
- **[プロジェクト構造](rule/project_structure.md)** - 全体アーキテクチャ
- **[CLAUDE.md](../CLAUDE.md)** - 開発ガイドライン・原則

---

## 📝 更新履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|----------|
| V3.0.031 | 2025-09-04 | 初版作成・全環境変数統一ドキュメント化 |

---

*このドキュメントは、DocOrganizer V3.0.031の環境変数管理システムの完全なリファレンスです。*