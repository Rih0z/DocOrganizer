# ログ出力問題の根本分析 - 2025-09-12

## 現在の状況
- **バージョン**: V3.0.095
- **問題**: ログ出力が一切機能しない
- **環境**: Windows 10, .NET 8, リリースビルド

## 確認済み事実

### 1. 実装状況
✅ **DebugLogger.cs**: 正常に実装済み（診断コード含む）
✅ **ENABLE_LOGGING フラグ**: csprojで正しく設定 (`TRACE;ENABLE_LOGGING`)
✅ **App.xaml.cs**: 複数のDebugLogger呼び出し存在
✅ **環境変数**: DOCORGANIZER_DEBUG=true で設定
✅ **アプリケーション**: 正常に起動し、動作する

### 2. 問題の特定
❌ **診断ファイル**: 一切作成されない
❌ **ログフォルダ**: .logsフォルダが作成されない
❌ **ログファイル**: debug.log, startup.log等が作成されない

## 重要な発見
診断ファイル（debug_diagnostic.txt, isdebugenabled_diagnostic.txt, logpath_diagnostic.txt）が一切作成されないことから、**DebugLoggerクラスのメソッドが全く呼び出されていない**ことが確定。

## 推測される原因

### 最有力仮説: 静的リンク問題
1. **単一ファイル配布**: PublishSingleFile=true設定
2. **アセンブリの最適化**: Releaseビルドでの未使用コード除去
3. **静的初期化の遅延**: DebugLoggerクラスの初期化タイミング

### その他の可能性
1. **コンパイル時除去**: ENABLE_LOGGINGフラグが実際には無効
2. **例外発生**: App.xaml.cs内でDebugLogger呼び出し前に例外
3. **アセンブリロード問題**: Core.dllの依存関係問題

## 解決アプローチ
1. **DebugLoggerの強制初期化**: 静的コンストラクタ追加
2. **より早期の呼び出し**: App()コンストラクタでの呼び出し
3. **単純化テスト**: 最小限のテストケース作成