# 複数選択時の問題 - 根本原因分析報告

**作成日時**: 2025-10-02
**バグ報告**:
1. 複数選択状態でページ移動ボタンを押せない
2. ドラッグ&ドロップしようとすると1つ選択解除される

---

## 🔍 根本原因特定

### **問題1: ページ移動ボタンが複数選択に未対応**

#### 所在
**PageOperationViewModel.cs** (src/DocOrganizer.UI/ViewModels/V3/)

#### 問題コード

**MovePageUpAsync (Lines 372-416)**:
```csharp
private async Task MovePageUpAsync()
{
    // ⚠️ 最初の選択ページのみ取得
    var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
    if (selectedPage == null)
    {
        return;
    }

    var currentIndex = Pages.IndexOf(selectedPage);

    if (currentIndex <= 0)
    {
        return;
    }

    // ⚠️ 単一ページ用コンストラクタのみ使用
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
    // ...
}
```

**MovePageDownAsync (Lines 421-465)**: 同様の問題

#### 問題点
- `FirstOrDefault` で最初の1ページしか取得しない
- 複数ページ用の `MovePagesCommand(PdfDocument, List<(PdfPage, int)>, Action)` コンストラクタが存在するが未使用
- ユーザーが3,5,7を選択しても、ページ3のみが移動される

---

### **問題2: ドラッグ時に選択が解除される原因**

#### 所在
**V3AdvancedDragDropBehavior.cs** (src/DocOrganizer.UI/Behaviors/)

#### 問題コード

**OnMouseLeftButtonDown (Lines 179-191)**:
```csharp
private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    // 🆕 V3.0.116: Ctrl/Shift選択時はListBoxのデフォルト動作を優先
    if (Keyboard.Modifiers != ModifierKeys.None)
    {
        _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] 修飾キー検出（{Keyboard.Modifiers}） - ListBoxのデフォルト選択動作を優先");
        return;
    }

    // ⚠️ ここで_dragStartPointを設定
    _dragStartPoint = e.GetPosition(null);
    _isDragging = false;
    _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] ドラッグ開始点設定 - sender: {sender?.GetType().Name}, Position: X={_dragStartPoint.X:F1}, Y={_dragStartPoint.Y:F1}");
}
```

#### 根本原因の流れ

**シナリオ**: ユーザーがページ3,5,7をCtrl+クリックで選択済み → ページ5をクリックしてドラッグ開始

1. **PreviewMouseLeftButtonDown** イベント発火（トンネリング）
   - `Keyboard.Modifiers` は `None`（Ctrlキーは押されていない）
   - `_dragStartPoint` を設定
   - イベントは続行される

2. **ListBoxのデフォルト選択処理** 実行（バブリング）
   - Ctrlキーが押されていないため、**単一選択モード**として処理
   - ページ5のみが選択状態になる
   - ページ3,7の選択が解除される ⚠️

3. **OnMouseMove** イベント発火
   - 既に選択は1つになっている
   - `V3DragInfo.SelectedItems` には1つしか入らない

#### 問題点
- **既存の複数選択を保護する仕組みがない**
- Ctrl/Shiftキーを**押しながら**クリックした場合のみ複数選択を保護
- 既に複数選択されている状態で通常クリックすると、ListBoxが単一選択に変更してしまう

#### 期待される動作
- ページ3,5,7が選択済み
- ページ5（選択済みアイテム）をクリック
- → **複数選択を維持**したままドラッグ開始
- → 3ページ全てが移動

---

### **問題3: PageListBox_SelectionChanged の影響**

#### 所在
**MainWindow.xaml.cs** (src/DocOrganizer.UI/Views/)

**PageListBox_SelectionChanged (Lines 582-665)**:
```csharp
private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    try
    {
        if (sender is ListBox listBox)
        {
            // ✅ 複数選択対応: ListBoxの選択状態をViewModelに同期
            if (V3ViewModel?.PageOperation?.Pages != null)
            {
                foreach (V3PageViewModel page in V3ViewModel.PageOperation.Pages)
                {
                    bool shouldBeSelected = listBox.SelectedItems.Contains(page);
                    if (page.IsSelected != shouldBeSelected)
                    {
                        page.IsSelected = shouldBeSelected;
                    }
                }

                V3ViewModel.PageOperation.NotifyPageSelectionChanged();
            }
            // ...
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error in PageListBox_SelectionChanged");
    }
}
```

#### 分析結果
- このコード自体は正しく、複数選択を適切に同期している
- ただし、Behaviorの`OnMouseLeftButtonDown`がListBoxの選択変更を引き起こすため、結果的に単一選択になってしまう

---

## 🎯 修正方針

### **修正1: MovePageUpAsync/MovePageDownAsync を複数対応**

#### ファイル
`src/DocOrganizer.UI/ViewModels/V3/PageOperationViewModel.cs`

#### 修正内容
既存の実装計画 (`.tmp/bulk_page_reordering_implementation_plan_20251002.md`) に従って実装：

**MovePageUpAsync 新ロジック**:
```csharp
private async Task MovePageUpAsync()
{
    if (_currentDocument == null || Pages.Count <= 1)
    {
        return;
    }

    // 🆕 全ての選択ページを取得（インデックス順）
    var selectedPages = Pages.Where(p => p.IsSelected)
                             .OrderBy(p => Pages.IndexOf(p))
                             .ToList();

    if (!selectedPages.Any())
    {
        return;
    }

    // 🆕 選択状態を保存（V3.0.115パターン）
    var selectedPageIds = selectedPages.Select(p => p.Id).ToHashSet();

    // 🆕 各ページの移動先を計算
    var pageMoves = new List<(PdfPage page, int newPosition)>();
    for (int i = 0; i < selectedPages.Count; i++)
    {
        var page = selectedPages[i];
        int currentIndex = Pages.IndexOf(page);

        // 先頭ページは移動できない
        if (currentIndex == 0)
            continue;

        int newPosition = currentIndex - 1;

        // 直前のページが選択済みの場合は移動しない（相対位置保持）
        if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex - 1)
            continue;

        pageMoves.Add((page.Page, newPosition));
    }

    // 移動するページがない場合は終了
    if (!pageMoves.Any())
    {
        StatusMessage = "これ以上上に移動できません";
        return;
    }

    // 🆕 複数ページ用コンストラクタ使用
    var command = new MovePagesCommand(
        _currentDocument,
        pageMoves,
        () => {
            RefreshPageListWithSelection(selectedPageIds);
            PagesChanged?.Invoke(this, EventArgs.Empty);
        }
    );

    _undoRedoService.Execute(command);
    StatusMessage = $"{selectedPages.Count}ページを上に移動しました";
}
```

**MovePageDownAsync**: 同様のロジック（降順で処理）

---

### **修正2: V3AdvancedDragDropBehavior - 複数選択保護**

#### ファイル
`src/DocOrganizer.UI/Behaviors/V3AdvancedDragDropBehavior.cs`

#### 修正内容

**OnMouseLeftButtonDown 修正**:
```csharp
private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    // 🆕 V3.0.117: Ctrl/Shift選択時はListBoxのデフォルト動作を優先
    if (Keyboard.Modifiers != ModifierKeys.None)
    {
        _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] 修飾キー検出（{Keyboard.Modifiers}） - ListBoxのデフォルト選択動作を優先");
        return;
    }

    // 🆕 V3.0.117: 既に複数選択されている場合、選択を保護
    if (sender is FrameworkElement element)
    {
        var listBox = FindParentListBox(element);
        if (listBox != null && listBox.SelectedItems.Count > 1)
        {
            // クリックされたアイテムが既に選択済みか確認
            var clickedItem = GetClickedItem(element);
            if (clickedItem != null && listBox.SelectedItems.Contains(clickedItem))
            {
                // 🎯 選択済みアイテムをクリック → ドラッグ準備のみ、選択は変更しない
                _dragStartPoint = e.GetPosition(null);
                _isDragging = false;
                e.Handled = true;  // ListBoxのデフォルト選択処理をブロック
                _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] 複数選択保護 - SelectedCount: {listBox.SelectedItems.Count}");
                return;
            }
        }
    }

    // 🔧 既存の単一選択処理
    _dragStartPoint = e.GetPosition(null);
    _isDragging = false;
    _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] ドラッグ開始点設定 - sender: {sender?.GetType().Name}, Position: X={_dragStartPoint.X:F1}, Y={_dragStartPoint.Y:F1}");
}

// 🆕 ヘルパーメソッド追加
private static ListBox? FindParentListBox(DependencyObject child)
{
    var current = child;
    while (current != null)
    {
        if (current is ListBox listBox)
            return listBox;
        current = VisualTreeHelper.GetParent(current);
    }
    return null;
}

private static object? GetClickedItem(FrameworkElement element)
{
    if (element is ListBoxItem listBoxItem)
        return listBoxItem.DataContext;

    if (element is ListBox listBox)
    {
        // HitTestでクリックされたアイテムを特定
        var position = Mouse.GetPosition(listBox);
        var hitResult = VisualTreeHelper.HitTest(listBox, position);
        if (hitResult?.VisualHit != null)
        {
            var current = hitResult.VisualHit as DependencyObject;
            while (current != null)
            {
                if (current is ListBoxItem item)
                    return item.DataContext;
                current = VisualTreeHelper.GetParent(current);
            }
        }
    }

    return element.DataContext;
}
```

---

## 🧪 テスト計画

### テストケース

| # | 操作 | 期待結果 |
|---|------|----------|
| 1 | ページ3,5,7を選択 → 「上に移動」 | 全3ページが移動（2,4,6になる） |
| 2 | ページ3,5,7を選択 → 「下に移動」 | 全3ページが移動（4,6,8になる） |
| 3 | ページ3,4,5を選択 → 「上に移動」 | ページ3のみ移動（連続保持: 2,3,4） |
| 4 | ページ3,5,7を選択 → ページ5をドラッグ | 3ページ全て移動、選択維持 |
| 5 | ページ3を選択 → ページ5をクリック | ページ5のみ選択（単一選択） |
| 6 | ページ3,5を選択 → Ctrl+ページ7クリック | 3,5,7が選択（追加選択） |

---

## ✅ 成功基準

1. **ページ移動ボタン**
   - ✅ 複数ページ選択時、全選択ページが移動
   - ✅ 相対位置が保持される
   - ✅ 移動後も選択状態が保持される

2. **ドラッグ&ドロップ**
   - ✅ 複数選択済みアイテムをクリックしても選択解除されない
   - ✅ ドラッグ時に全選択ページが移動
   - ✅ ドロップ後も選択状態が保持される

3. **品質**
   - ✅ V3.0.115の選択状態保持パターン準拠
   - ✅ Undo/Redo 正常動作
   - ✅ 単一選択も正常動作（後方互換性）

---

## 📌 実装順序

1. ✅ 根本原因分析完了 ← 現在
2. ⏳ MovePageUpAsync/MovePageDownAsync 修正
3. ⏳ V3AdvancedDragDropBehavior 修正（複数選択保護）
4. ⏳ ビルド＆テスト
5. ⏳ V3.0.117 リリース

---

## 🎯 次のアクション

**ユーザーに報告**:
- 根本原因特定完了
- 問題1: ページ移動ボタンが単一ページしか処理していない
- 問題2: ドラッグ時にListBoxが選択を単一に上書きしてしまう
- 修正方針確定（複数ページ対応 + 複数選択保護）
- 実装承認後、すぐに修正開始可能
