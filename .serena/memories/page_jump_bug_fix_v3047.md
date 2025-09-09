# ページジャンプバグ修正 - V3.0.047

## 問題
ページ移動時に2つずつジャンプする問題（例：5→3になる）

## 原因
CollectionChangedイベントハンドラーの重複実行
- Pages.Move()時にCollectionChangedイベント発火
- イベント内でUpdateSelectionState()実行
- Move後にも明示的にUpdateSelectionState()実行
- 結果として二重処理

## 解決策
1. CollectionChangedイベントハンドラーを名前付きメソッドに分離
2. _isMovingPageフラグでMove操作中のイベント処理をスキップ
3. スキップした場合は手動でUpdateSelectionState()を呼ぶ

## 修正ファイル
- src/DocOrganizer.UI/ViewModels/V3/PageOperationViewModel.cs
  - OnPagesCollectionChangedメソッド追加
  - MovePageUpAsync/MovePageDownAsync修正

## バージョン
V3.0.047で修正実装済み