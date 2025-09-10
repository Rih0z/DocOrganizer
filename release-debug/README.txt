DocOrganizer V3.0.068 - 単一実行ファイル版
============================================

【必須ファイル】
- DocOrganizer.exe (158MB) - 統合済み単一実行ファイル
- pdfium.dll (15MB) - PDF描画エンジン
- config\AppSettings.json - 設定ファイル

【デバッグ用ファイル（開発者向け）】
- DocOrganizer.pdb - デバッグシンボル
- eng.user-words - OCRユーザー辞書
- eng.user-patterns - OCRパターン定義
- Tesseract.Native.deployment.json - OCR設定

【起動方法】
エクスプローラーからDocOrganizer.exeをダブルクリックして起動

【注意事項】
- 管理者権限での起動は不要です
- ドラッグ&ドロップ機能を使用する場合は管理者権限で起動しないでください
- .NET Runtimeは不要です（exe内に統合済み）

【新機能 (V3.0.068)】
- Ctrl+Z: 元に戻す (Undo)
- Ctrl+Y: やり直し (Redo)
- 削除・回転・移動操作のUndo/Redo対応

【統合内容】
このEXEには以下が統合されています：
- .NET 8.0 Runtime
- 全てのマネージドDLL
- WPF/XAML関連ライブラリ
- アプリケーションロジック