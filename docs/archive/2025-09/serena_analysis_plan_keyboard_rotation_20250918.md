# Ctrl+L キーボードショートカット回転時の選択解除問題 - Serena MCPアーキテクチャ分析計画

## 分析日時: 2025-09-18 23:45

## 1. 問題の詳細分析

### 現象の比較
| 操作方法 | コマンド | 選択状態 | 実装場所 |
|---------|---------|---------|----------|
| ボタンクリック回転 | RotateLeftCommand | ❌解除される→✅修正済(V3.0.106) | RotateLeftAsync |
| Ctrl+L回転 | RotateLeftCommand | ❌依然として解除される | 同上 |
| Ctrl+B移動 | MovePageUpCommand | ✅維持される | MovePageUpAsync |

### 問題の核心
- **同じRotateLeftCommandを呼んでいるにも関わらず、Ctrl+Lでは選択が解除される**
- MovePageUpCommandは正常に動作している

## 2. アーキテクチャ分析

### 2.1 コマンド実行パス分析

#### RotateLeftAsync (V3.0.106修正済み)
```csharp
private async Task RotateLeftAsync()
{
    // 選択状態を保存
    var selectedPageIds = Pages.Where(p => p.IsSelected)
                              .Select(p => p.Id)
                              .ToHashSet();
    
    var command = new RotatePagesCommand(
        selectedPages,
        270,
        () => {
            RefreshPageList();
            RestoreSelection(selectedPageIds);  // ← ここで復元
            PagesChanged?.Invoke(this, EventArgs.Empty);
        }
    );
    
    _undoRedoService.Execute(command);
}
```

#### MovePageUpAsync (正常動作)
```csharp
private async Task MovePageUpAsync()
{
    var command = new MovePagesCommand(
        _currentDocument,
        selectedPage.Page,
        currentIndex - 1,
        () => {
            RefreshPageList();
            PagesChanged?.Invoke(this, EventArgs.Empty);
        }
    );
    
    _undoRedoService.Execute(command);
    
    // 選択状態を復元 - コマンド実行後に直接設定
    if (currentIndex - 1 < Pages.Count)
    {
        Pages[currentIndex - 1].IsSelected = true;
    }
    UpdateSelectionState();
}
```

### 2.2 重要な違いの発見

**MovePageUpAsync**: コマンド実行後、メソッド内で直接選択を復元
**RotateLeftAsync**: コマンドのコールバック内で選択を復元

### 2.3 問題の仮説

1. **タイミング問題**: コールバック内での復元が、何らかの理由で無効化されている
2. **非同期処理の競合**: AsyncRelayCommandとコールバックのタイミング
3. **キーボード処理の特殊性**: キーボード入力時の追加処理が選択をリセット

## 3. 根本原因の特定

### 3.1 AsyncRelayCommandの実装確認
```csharp
RotateLeftCommand = new AsyncRelayCommand(RotateLeftAsync);
```
- AsyncRelayCommandは非同期処理を適切に待機しているか？
- キーボードショートカット実行時の特別な処理があるか？

### 3.2 RefreshPageListのタイミング
- RefreshPageList()が完了する前にRestoreSelection()が呼ばれている可能性
- Pages.Clear()とPages.Add()の間でUIが更新されている可能性

## 4. 推奨される修正アプローチ

### アプローチ1: MovePageUpAsyncと同じパターンに統一（推奨）

```csharp
private async Task RotateLeftAsync()
{
    if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
    {
        return;
    }
    
    // 選択状態を保存
    var selectedPageIds = Pages.Where(p => p.IsSelected)
                              .Select(p => p.Id)
                              .ToHashSet();
    
    var selectedPages = Pages.Where(p => p.IsSelected)
        .Select(vm => vm.Page)
        .ToList();
    
    var selectedViewModels = Pages.Where(p => p.IsSelected).ToList();
    
    var command = new RotatePagesCommand(
        selectedPages,
        270,
        () => {
            RefreshPageList();
            PagesChanged?.Invoke(this, EventArgs.Empty);
            
            // PageRotatedイベント処理
            var updatedViewModels = Pages.Where(vm => selectedPageIds.Contains(vm.Id)).ToList();
            foreach (var pageViewModel in updatedViewModels)
            {
                PageRotated?.Invoke(this, new PageOperationEventArgs(pageViewModel));
            }
        }
    );
    
    _undoRedoService.Execute(command);
    
    // コマンド実行後に選択状態を復元（MovePageUpAsyncと同じパターン）
    RestoreSelection(selectedPageIds);
    
    StatusMessage = "選択したページを左回転しました";
}
```

### アプローチ2: 遅延実行による確実な復元

```csharp
private async Task RotateLeftAsync()
{
    // ... 既存のコード ...
    
    _undoRedoService.Execute(command);
    
    // UIスレッドで次のフレームまで待機して選択を復元
    await Task.Delay(10);
    Application.Current.Dispatcher.Invoke(() => {
        RestoreSelection(selectedPageIds);
    });
}
```

## 5. OSS参考実装調査

### WPF MVVMサンプル
- **Prism Library**: コマンド実行後の状態管理パターン
- **ReactiveUI**: 非同期コマンドと状態同期のベストプラクティス

### 類似の問題と解決策
- Stack Overflow: [WPF ListBox selection lost after refresh](https://stackoverflow.com/questions/4831395/)
  - 解決策: RefreshPageList後に確実に選択を復元

## 6. リスク評価

### 技術的リスク
| リスク | 影響度 | 発生確率 | 対策 |
|--------|-------|---------|------|
| 非同期処理の競合 | 高 | 中 | 同期的な選択復元 |
| UIスレッドのブロック | 中 | 低 | 適切な非同期処理 |
| Undo/Redoの不整合 | 中 | 低 | 十分なテスト |

## 7. 実装計画

### Phase 1: 即座の修正（5分）
1. RotateLeftAsync/RotateRightAsyncをMovePageUpAsyncパターンに変更
2. コマンド実行後に直接選択を復元

### Phase 2: テスト（10分）
1. Ctrl+Lでの回転テスト
2. ボタンクリックでの回転テスト
3. 連続操作のテスト

### Phase 3: 他のショートカット確認（5分）
1. Ctrl+Rの確認
2. その他のキーボードショートカット確認

## 8. テスト項目

### 機能テスト
- [ ] Ctrl+L → 選択維持
- [ ] Ctrl+R → 選択維持
- [ ] ボタンクリック回転 → 選択維持
- [ ] Ctrl+B（移動）→ 選択維持（既存動作確認）
- [ ] 連続回転操作
- [ ] Undo/Redo後の選択状態

## 9. 推奨実装コード

```csharp
// PageOperationViewModel.cs

private async Task RotateLeftAsync()
{
    if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
    {
        return;
    }
    
    // V3.0.107: 選択状態を保存
    var selectedPageIds = Pages.Where(p => p.IsSelected)
                              .Select(p => p.Id)
                              .ToHashSet();
    
    var selectedPages = Pages.Where(p => p.IsSelected)
        .Select(vm => vm.Page)
        .ToList();
    
    var selectedViewModels = Pages.Where(p => p.IsSelected).ToList();
    
    var command = new RotatePagesCommand(
        selectedPages,
        270, // 左回転 = 270度（反時計回り）
        () => {
            RefreshPageList();
            PagesChanged?.Invoke(this, EventArgs.Empty);
            
            // V3.0.088: ID再検索方式で最新インスタンス取得
            var updatedViewModels = Pages.Where(vm => selectedPageIds.Contains(vm.Id)).ToList();
            
            foreach (var pageViewModel in updatedViewModels)
            {
                PageRotated?.Invoke(this, new PageOperationEventArgs(pageViewModel));
            }
        }
    );
    
    _undoRedoService.Execute(command);
    
    // V3.0.107: コマンド実行後に選択状態を復元（MovePageUpAsyncと同じパターン）
    RestoreSelection(selectedPageIds);
    
    StatusMessage = "選択したページを左回転しました";
}

// 同様にRotateRightAsyncも修正
```

## 10. まとめ

### 問題の本質
コマンドのコールバック内での選択復元が、キーボードショートカット実行時に正しく動作していない

### 解決策
MovePageUpAsyncと同じパターンで、コマンド実行後に直接選択を復元

### 期待される効果
- Ctrl+L/Ctrl+Rでも選択状態が維持される
- ボタンクリック操作との一貫性確保
- 連続操作の効率向上

### 実装時間見積もり
- 修正: 5分
- テスト: 10分
- **合計: 15分**

### 信頼度
**90%** - MovePageUpAsyncの成功パターンを適用することで高確率で解決

---

**分析完了**: 2025-09-18 23:50
**Serena MCP Architecture Analysis System**