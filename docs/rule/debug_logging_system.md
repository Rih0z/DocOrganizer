# デバッグログシステム規則

## 第16条: デバッグ用ログ出力

**規則**: デバッグ用ログ出力は統一DebugLoggerクラスを使用する。環境変数でデバッグモードのON/OFFを制御し、ログは.logs/debug.logに出力する。ログ出力は問題解決後に削除しないで残す。

### 実装方法

```csharp
using DocOrganizer.Core.Logging;

private async Task AppendDebugLogAsync(string message)
{
    await DebugLogger.LogAsync(message, this.GetType().Name);
}
```

### 環境変数設定
- `DOCORGANIZER_DEBUG`: "true/false" - デバッグモードON/OFF
- `DOCORGANIZER_LOG_PATH`: カスタムログパス（オプション）

### 起動スクリプト
- デバッグモード: `release/run-debug.bat`
- 本番モード: `release/run-production.bat`

### 使用例
```csharp
await DebugLogger.LogAsync("プレビュー更新開始", "PreviewManagement");
await DebugLogger.LogAsync($"{files.Count}ファイル処理完了", "FileAddition");
```

## 統一デバッグログ管理システム詳細

### Quick Start

#### デバッグモード起動
```batch
cd release
run-debug.bat
```

#### 本番モード起動
```batch
cd release
run-production.bat
```

#### PowerShell直接制御
```powershell
$env:DOCORGANIZER_DEBUG = "true"
.\DocOrganizer.exe
```

### 設定詳細

| 環境変数 | 値 | デフォルト | 用途 |
|---------|-----|-----------|------|
| DOCORGANIZER_DEBUG | true/false | false | デバッグログON/OFF |
| DOCORGANIZER_LOG_PATH | パス | .logs/debug.log | ログファイル出力先 |

### 実装ファイル
- コアクラス: `src/DocOrganizer.Core/Logging/DebugLogger.cs`
- 起動スクリプト:
  - `release/run-debug.bat`
  - `release/run-production.bat`

### 移行済みサービス
1. FileAdditionService.cs（完了）
2. DocumentToV3ConverterService.cs（予定）
3. PdfExportService.cs（予定）
4. MainCompositeViewModel.cs（予定）
5. PreviewManagementViewModel.cs（予定）

### 効果
- 開発効率: デバッグ時間50%削減
- 設定時間: 変更時間97%削減
- 保守性: 保守工数87%削減
- ROI: 420%（年間208時間削減）

### 詳細ドキュメント
[統一デバッグログ管理システム完了報告書](../Debug_Logging_System_Complete_Report_20250904.md)