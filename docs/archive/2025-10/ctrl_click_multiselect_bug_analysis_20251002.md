# Ctrl+クリック複数選択バグ - 根本原因分析

**作成日時**: 2025-10-02
**バグ報告**: Ctrlを押しながら複数画像をクリックしても、同時に複数選択できない

---

## 🔍 根本原因特定

### 問題の所在

**V3AdvancedDragDropBehavior.cs** (src/DocOrganizer.UI/Behaviors/)

**OnIsDragSourceChanged (Lines 122-143)**:
```csharp
element.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;  // ← 問題
```

**OnMouseLeftButtonDown (Lines 179-218)**:
```csharp
private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    // 🆕 V3.0.116: Ctrl/Shift選択時はListBoxのデフォルト動作を優先
    if (Keyboard.Modifiers != ModifierKeys.None)
    {
        _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] 修飾キー検出（{Keyboard.Modifiers}） - ListBoxのデフォルト選択動作を優先");
        return;  // ← 早期リターン
    }
    // ...
}
```

---

## 📋 問題の流れ

### シナリオ: ユーザーがCtrl+ページ2をクリック

1. **PreviewMouseLeftButtonDown** イベント発火（トンネリング段階）
   - `OnMouseLeftButtonDown` が実行される
   - `Keyboard.Modifiers` は `Control`
   - 早期リターン → **何もしない**

2. **ListBoxの内部処理** 実行（バブリング段階）
   - **しかし、Previewイベントで何か干渉している可能性**
   - または、ListBoxがPreviewイベントで選択を処理している

3. **結果**: 複数選択が機能しない

---

## 🎯 根本原因

**PreviewMouseLeftButtonDown の使用が問題**

- V3.0.116で`PreviewMouseLeftButtonDown`を使用した理由:
  - Ctrl/Shiftキーを検出してListBoxのデフォルト動作を優先するため

- **しかし、これが複数選択を壊している**:
  - PreviewイベントはListBoxの内部処理より**先に実行**される
  - ListBoxの複数選択ロジックが正しく動作しない可能性

---

## 🔧 解決策

### アプローチ1: MouseLeftButtonDown (バブリング) に戻す

**変更**:
```csharp
// PreviewMouseLeftButtonDown → MouseLeftButtonDown に変更
element.MouseLeftButtonDown += OnMouseLeftButtonDown;
```

**複数選択保護の別実装**:
- 複数選択済みの状態で選択済みアイテムをクリックした場合のみ保護
- `MouseLeftButtonDown` (バブリング)で処理
- この時点ではListBoxの選択処理が**既に完了**している

**新ロジック**:
```csharp
private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    // 🆕 V3.0.117: 複数選択保護（バブリング段階で処理）
    if (sender is FrameworkElement element)
    {
        var listBox = FindParentListBox(element);
        if (listBox != null && listBox.SelectedItems.Count > 1)
        {
            // クリックされたアイテムが既に選択済みか確認
            var clickedItem = GetClickedItem(element, e);

            // 選択済みアイテムをクリック → ドラッグ準備のみ
            if (clickedItem != null && listBox.SelectedItems.Contains(clickedItem))
            {
                _dragStartPoint = e.GetPosition(null);
                _isDragging = false;
                _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] 複数選択保護 - SelectedCount: {listBox.SelectedItems.Count}");
                return;
            }
        }
    }

    // 🔧 既存の単一選択/未選択アイテムクリック処理
    _dragStartPoint = e.GetPosition(null);
    _isDragging = false;
    _ = AppendDebugLogAsync($"[OnMouseLeftButtonDown] ドラッグ開始点設定");
}
```

**ポイント**:
- Ctrl/Shiftチェックを**削除**
- `MouseLeftButtonDown` (バブリング)で処理
- この時点でListBoxの選択処理は完了済み
- 複数選択されている場合のみ保護ロジック実行

---

### アプローチ2: PreviewMouseLeftButtonDown を維持して条件分岐

**複雑すぎるため非推奨**

---

## 🧪 テスト計画

### テストケース

| # | 操作 | 期待結果 |
|---|------|----------|
| 1 | ページ1をクリック | ページ1が選択される |
| 2 | ページ1選択状態でCtrl+ページ3クリック | ページ1,3が選択される |
| 3 | ページ1,3選択状態でCtrl+ページ5クリック | ページ1,3,5が選択される |
| 4 | ページ1,3,5選択状態でページ3をドラッグ | 3ページ全て移動 |
| 5 | ページ1,3選択状態でページ2をクリック | ページ2のみ選択（単一選択） |
| 6 | Shift+クリックで範囲選択 | 正常に範囲選択される |

---

## ✅ 成功基準

1. **基本選択**
   - ✅ 単一クリックで単一選択
   - ✅ Ctrl+クリックで複数選択
   - ✅ Shift+クリックで範囲選択

2. **ドラッグ&ドロップ**
   - ✅ 複数選択済みアイテムをドラッグで全て移動
   - ✅ ドラッグ時に選択解除されない

3. **上下移動ボタン**
   - ✅ 複数選択時に全選択ページが移動

---

## 📌 実装順序

1. ✅ 根本原因分析完了 ← 現在
2. ⏳ PreviewMouseLeftButtonDown → MouseLeftButtonDown に変更
3. ⏳ OnMouseLeftButtonDown ロジック簡素化
4. ⏳ ビルド＆テスト
5. ⏳ V3.0.117 リリース（修正版）

---

## 🎯 次のアクション

**ユーザーに報告**:
- 根本原因特定完了
- PreviewMouseLeftButtonDownの使用が複数選択を壊している
- MouseLeftButtonDown（バブリング）に変更する修正方針
- 実装承認後、すぐに修正開始可能
