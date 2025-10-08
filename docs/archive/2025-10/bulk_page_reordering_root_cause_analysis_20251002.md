# 複数ページ一括移動バグ - 根本原因分析報告

**作成日時**: 2025-10-02
**バグ報告**: ドラッグ&ドロップで複数画像選択時、1枚しか移動できない

---

## 🔍 根本原因特定

### 問題の所在

**V3DragInfo クラス** (`src/DocOrganizer.UI/Models/V3/V3DragDropInfo.cs:254-303`)

```csharp
public class V3DragInfo : IAdvancedDragInfo
{
    public FrameworkElement SourceElement { get; private set; }
    public Point StartPosition { get; private set; }
    public object SourceItem { get; private set; }  // ⚠️ 単一アイテムのみ
    public MouseEventArgs MouseEventArgs { get; private set; }

    public V3DragInfo(FrameworkElement sourceElement, MouseEventArgs mouseEventArgs)
    {
        // ...
        // ListBoxItem の DataContext を取得（単一アイテム）
        if (sourceElement is ListBoxItem listBoxItem)
        {
            SourceItem = listBoxItem.DataContext;  // ⚠️ 1つのページしか取得しない
        }
        // ...
    }
}
```

**問題点**:
- `SourceItem` は `object` 型で単一アイテムのみ保持
- ListBoxの複数選択（`SelectedItems`）を取得する仕組みがない
- ドラッグ開始時にクリックされたアイテムしか取得できない

---

## 📋 調査結果

### ✅ 正しく動作している部分

#### 1. MainWindow.xaml - ListBox設定
```xaml
<ListBox x:Name="PageListBox"
         SelectionMode="Extended"  <!-- ✅ 複数選択有効 -->
         AllowDrop="True"
         ...>
```

#### 2. ThumbnailList_PreviewMouseMove (MainWindow.xaml.cs:245-280)
```csharp
// ✅ 選択されたページを取得（複数対応）
var selectedPages = listBox.SelectedItems.Cast<V3PageViewModel>().ToList();

if (selectedPages.Any())
{
    // ✅ ドラッグデータを作成（複数ページ）
    System.Windows.DataObject dragData = new System.Windows.DataObject();
    dragData.SetData("PageViewModels", selectedPages);

    // ドラッグ操作を開始
    DragDrop.DoDragDrop(listBoxItem, dragData, System.Windows.DragDropEffects.Move);
}
```

**注意**: この`ThumbnailList_PreviewMouseMove`は**現在使用されていません**！
代わりに`V3AdvancedDragDropBehavior`が使用されています。

#### 3. ReorderPagesAsync (PageOperationViewModel.cs:471-521)
```csharp
// ✅ 複数ページ対応済み
public async Task ReorderPagesAsync(List<V3PageViewModel> pagesToMove, V3PageViewModel targetPage)
{
    // 全ページを正しく移動
    foreach (var page in pagesToMove.OrderByDescending(p => Pages.IndexOf(p)))
    {
        // ...移動処理...
    }
}
```

---

### ❌ 問題がある部分

#### 1. V3AdvancedDragDropBehavior (現在使用中)

**StartDragAsync呼び出し** (V3AdvancedDragDropBehavior.cs:253-306):
```csharp
var dragInfo = new V3DragInfo(source, e);  // ⚠️ 単一アイテムのみ取得
var dragData = await dragHandler.StartDragAsync(dragInfo);
```

#### 2. DragDropHandlerViewModel.StartDragAsync (line 282-312)
```csharp
public async Task<object> StartDragAsync(IAdvancedDragInfo dragInfo)
{
    // ⚠️ 単一ページのみ取得
    if (dragInfo.SourceItem is V3PageViewModel pageViewModel)
    {
        var dragId = Guid.NewGuid().ToString();
        _dragCache[dragId] = pageViewModel;  // ⚠️ 1ページのみキャッシュ

        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Text, dragId);

        return dataObject;
    }

    return null;
}
```

#### 3. OnDrop処理 (V3AdvancedDragDropBehavior.cs:364-416)
```csharp
private static async Task OnDrop(UIElement target, DragEventArgs e)
{
    // dropHandler.HandleDropAsync 経由で処理
    // _dragCache から取得するのは単一ページのみ
}
```

---

## 🎯 修正方針

### 修正1: V3DragInfo に複数選択対応プロパティ追加

**ファイル**: `src/DocOrganizer.UI/Models/V3/V3DragDropInfo.cs`

```csharp
public class V3DragInfo : IAdvancedDragInfo
{
    public FrameworkElement SourceElement { get; private set; }
    public Point StartPosition { get; private set; }
    public object SourceItem { get; private set; }
    public MouseEventArgs MouseEventArgs { get; private set; }

    // 🆕 追加: 複数選択対応
    public IEnumerable<object>? SelectedItems { get; private set; }

    public V3DragInfo(FrameworkElement sourceElement, MouseEventArgs mouseEventArgs)
    {
        SourceElement = sourceElement;
        MouseEventArgs = mouseEventArgs;
        StartPosition = mouseEventArgs.GetPosition(sourceElement);

        if (sourceElement is ListBoxItem listBoxItem)
        {
            SourceItem = listBoxItem.DataContext;

            // 🆕 親ListBoxから複数選択を取得
            var listBox = FindAncestor<ListBox>(listBoxItem);
            if (listBox != null && listBox.SelectedItems.Count > 0)
            {
                SelectedItems = listBox.SelectedItems.Cast<object>().ToList();
            }
        }
        else if (sourceElement is ListBox listBox)
        {
            // ListBox直接の場合
            var position = mouseEventArgs.GetPosition(listBox);
            var hitResult = VisualTreeHelper.HitTest(listBox, position);
            // ... 既存のSourceItem取得処理 ...

            // 🆕 複数選択を取得
            if (listBox.SelectedItems.Count > 0)
            {
                SelectedItems = listBox.SelectedItems.Cast<object>().ToList();
            }
        }
        else
        {
            SourceItem = sourceElement.DataContext;
        }
    }

    // 🆕 ヘルパーメソッド追加
    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T ancestor)
                return ancestor;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
```

---

### 修正2: DragDropHandlerViewModel.StartDragAsync を複数対応

**ファイル**: `src/DocOrganizer.UI/ViewModels/V3/DragDropHandlerViewModel.cs`

```csharp
public async Task<object> StartDragAsync(IAdvancedDragInfo dragInfo)
{
    try
    {
        // 🆕 複数選択対応
        if (dragInfo is V3DragInfo v3DragInfo &&
            v3DragInfo.SelectedItems != null &&
            v3DragInfo.SelectedItems.Any())
        {
            // 複数ページ選択時
            var selectedPages = v3DragInfo.SelectedItems
                .OfType<V3PageViewModel>()
                .ToList();

            if (selectedPages.Count > 1)
            {
                // 🆕 複数ページをキャッシュ
                var dragId = Guid.NewGuid().ToString();
                _dragCache[dragId] = selectedPages;  // List<V3PageViewModel>として保存

                await AppendDebugLogAsync($"[StartDragAsync] Multiple pages drag started - DragID: {dragId}, Count: {selectedPages.Count}");

                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.Text, dragId);

                StatusMessage = $"{selectedPages.Count} ページをドラッグ中...";

                return dataObject;
            }
        }

        // 🔧 既存の単一ページ処理（フォールバック）
        if (dragInfo.SourceItem is V3PageViewModel pageViewModel)
        {
            var dragId = Guid.NewGuid().ToString();
            _dragCache[dragId] = pageViewModel;

            await AppendDebugLogAsync($"[StartDragAsync] Single page drag started - DragID: {dragId}, Page: {pageViewModel.PageNumber}");

            var dataObject = new DataObject();
            dataObject.SetData(DataFormats.Text, dragId);

            StatusMessage = $"ページ {pageViewModel.PageNumber} をドラッグ中...";

            return dataObject;
        }

        await AppendDebugLogAsync("[StartDragAsync] No draggable item detected");
        return null;
    }
    catch (Exception ex)
    {
        await AppendDebugLogAsync($"[StartDragAsync] Error: {ex.Message}");
        return null;
    }
}
```

---

### 修正3: HandleDropAsync を複数対応

**ファイル**: `src/DocOrganizer.UI/ViewModels/V3/DragDropHandlerViewModel.cs`

既存の`HandleDropAsync`で`_dragCache`から取得時、`List<V3PageViewModel>`も処理できるように修正：

```csharp
public async Task HandleDropAsync(IAdvancedDropInfo dropInfo)
{
    try
    {
        // ... 既存の処理 ...

        if (_dragCache.TryGetValue(dragId, out var cachedItem))
        {
            // 🆕 複数ページ対応
            if (cachedItem is List<V3PageViewModel> pageList)
            {
                // 複数ページのドロップ処理
                if (dropInfo.TargetItem is V3PageViewModel targetPage)
                {
                    await HandlePageReorderAsync(pageList, targetPage);
                }
            }
            else if (cachedItem is V3PageViewModel singlePage)
            {
                // 既存の単一ページ処理
                if (dropInfo.TargetItem is V3PageViewModel targetPage)
                {
                    await HandlePageReorderAsync(new List<V3PageViewModel> { singlePage }, targetPage);
                }
            }

            _dragCache.Remove(dragId);
        }
    }
    catch (Exception ex)
    {
        // ...
    }
}
```

---

### 修正4: MovePageUpAsync/MovePageDownAsync も複数対応

**ファイル**: `src/DocOrganizer.UI/ViewModels/V3/PageOperationViewModel.cs`

既存の実装計画通り、上下移動ボタンも複数ページ対応する。

---

## 🧪 テスト計画

### テストケース

| # | 操作 | 選択ページ | 期待結果 |
|---|------|-----------|----------|
| 1 | ドラッグ&ドロップ | 3,5,7 | 3ページ全て移動 |
| 2 | ドラッグ&ドロップ | 1,2,3 | 連続3ページ移動 |
| 3 | ドラッグ&ドロップ | 単一(5) | 1ページ移動（互換性） |
| 4 | 上移動ボタン | 3,7,9 | → 2,6,8 |
| 5 | 下移動ボタン | 3,7,9 | → 4,8,10 |
| 6 | 連続操作 | 3,5選択→回転→移動 | 選択維持で操作可能 |

---

## ✅ 成功基準

1. **ドラッグ&ドロップ**
   - ✅ 複数ページ選択時、全選択ページが移動
   - ✅ 単一ページ選択時も正常動作（後方互換性）
   - ✅ 選択状態が移動後も保持

2. **上下移動ボタン**
   - ✅ 複数ページ選択時、全選択ページが移動
   - ✅ 相対位置が保持される

3. **品質**
   - ✅ V3.0.115の選択状態保持パターン準拠
   - ✅ Undo/Redo 正常動作
   - ✅ エンタープライズレベルの実装

---

## 📌 実装順序

1. ✅ **根本原因分析完了** ← 現在
2. ⏳ V3DragInfo 修正
3. ⏳ StartDragAsync 修正
4. ⏳ HandleDropAsync 修正
5. ⏳ MovePageUpAsync/MovePageDownAsync 修正
6. ⏳ ビルド＆テスト
7. ⏳ V3.0.116 リリース

---

## 🎯 次のアクション

**ユーザーに報告**:
- 根本原因特定完了
- V3AdvancedDragDropBehaviorが単一ページしか取得していない
- 修正方針確定（V3DragInfoに複数選択プロパティ追加）
- 実装承認後、すぐに修正開始可能
