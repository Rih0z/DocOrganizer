# ページ追加時の重複LoadPagesAsyncバグ修正 (2025-01-19)

## 問題の症状
- 既存ドキュメントに画像を追加すると、サムネイルが一瞬表示された後全て消える
- ページ並び替え（内包）操作時にサムネイル表示がおかしくなる

## 根本原因
LoadPagesAsyncメソッドが2回連続で呼ばれていた：
1. 最初: OnFileAdditionCompleted → LoadPagesAsync（完全リロードモード）
2. 次に: OnFilesAddedToDocument → LoadPagesAsync（増分更新モード）

最初の呼び出しで全ページがクリアされ、2回目の増分更新時には既に0ページになっていた。

## 実施した修正

### MainCompositeViewModel.cs
1. **OnFileAdditionCompletedイベントハンドラーを削除**
   - DragDropHandler.FileAdditionCompleted += OnFileAdditionCompleted; をコメントアウト
   - OnFileAdditionCompletedメソッド自体もコメントアウト

2. **OnFilesAddedToDocumentメソッドを強化**
   - ステータス更新とページ選択ロジックを追加
   - 新規追加ページの選択機能を統合

## 修正後の動作
- FilesAddedToDocumentイベントのみで適切に処理
- LoadPagesAsyncは増分更新モードで1回のみ実行
- 既存サムネイルが保持され、新規ページが追加される

## 成果物
- 修正済みEXEファイル: C:\Users\217216X721451\github\DocOrganizer\release\DocOrganizer.exe
- バグ分析レポート: tmp\DocOrganizer_ページ追加時の重複LoadPagesAsyncバグ分析_20250119_完全版.md