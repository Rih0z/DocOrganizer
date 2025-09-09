# ログシステムの問題点と修正計画

## 発見された問題

### 1. ハードコードされたログパス（13箇所）
- **DEBUG_LOG.txt**: ImageProcessingService.cs、複数のViewModel
- **STARTUP_LOG.txt**: App.xaml.cs
- **.logs/debug.log**: DebugLogger.cs（設定可能）
- **C:\Users\217216X721451\github\DocOrganizer\release\DEBUG_LOG.txt**: 絶対パス使用

### 2. 統一されていないログ実装
- 各クラスで独自のログメソッド実装
- File.AppendAllText()の直接使用
- DebugLoggerクラスが活用されていない

### 3. デバッグモードのON/OFF制御が効かない
- 環境変数が設定されていても一部のログは出力される
- ハードコードされたログは制御不可能

## 修正方針

### Phase 1: DebugLoggerクラスの強化
1. 設定ファイルからのパス読み込み機能追加
2. ログレベル制御の実装
3. 非同期ログ出力の最適化

### Phase 2: 全ログ出力の統一
1. すべてのFile.AppendAllText()をDebugLogger経由に変更
2. 各ViewModelのログメソッドをDebugLogger呼び出しに統一
3. STARTUP_LOG.txtもDebugLogger経由に統一

### Phase 3: 設定ファイルベースの制御
1. config/LoggingSettings.jsonの作成
2. ログパス、レベル、ON/OFFの設定可能化
3. 実行時の動的設定変更機能

## 影響範囲
- 13ファイルの修正が必要
- テストコードは除外（テスト用ログは別扱い）
- ビルド・デプロイメントへの影響なし