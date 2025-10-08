# V3.0.117 アプローチ分析 - 複数選択問題

**作成日時**: 2025-10-02
**状況**: 複数の修正試行後も複数選択が正常動作しない

---

## 🔄 修正試行の履歴

### 試行1: PreviewMouseLeftButtonDown + 複数選択保護
**実装**: V3.0.116初期
- `PreviewMouseLeftButtonDown` 使用
- Ctrl/Shift検出で早期リターン
- 複数選択保護ロジック実装

**結果**: ❌
- Ctrl+クリック複数選択ができない

---

### 試行2: MouseLeftButtonDown + シンプル化
**実装**: V3.0.117現在
- `MouseLeftButtonDown` (バブリング) に変更
- 全てのロジックを削除してシンプル化

**結果**: ❌
- Ctrl+クリックで追加選択できない
- 選択が1つに上書きされる

---

## 🔍 根本原因

### ListBoxの選択処理タイミング

**PreviewMouseLeftButtonDown（トンネリング）**:
- イベント順序: Behavior → ListBox内部処理
- 問題: Behaviorで早期リターンしても、ListBoxの複数選択ロジックが正しく動作しない

**MouseLeftButtonDown（バブリング）**:
- イベント順序: ListBox内部処理 → Behavior
- 問題: この時点でListBoxが既に選択を処理済み

### ListBoxの内部実装問題

ListBoxの複数選択は**OnPreviewMouseLeftButtonDown内で処理**されているため：
- `PreviewMouseLeftButtonDown` に何か登録すると干渉する
- `MouseLeftButtonDown` では遅すぎる

---

## 🎯 正しいアプローチ

### アプローチ1: Behaviorをカスタマイズ（推奨）

**ListBoxItemにBehaviorをアタッチ**する方法：

**現在**:
```xaml
<ListBox behaviors:V3AdvancedDragDropBehavior.IsDragSource="True">
```

**変更後**:
```xaml
<ListBox>
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="behaviors:V3AdvancedDragDropBehavior.IsDragSource" Value="True"/>
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
```

**メリット**:
- 各ListBoxItemに個別にBehaviorがアタッチ
- ListBoxの選択処理と干渉しない

**デメリット**:
- 大きな変更が必要
- テストが必要

---

### アプローチ2: V3.0.116の問題だけを修正（現実的）

V3.0.116では以下が動作していた：
- ✅ Ctrl+クリック複数選択
- ✅ ドラッグ&ドロップ
- ❌ ドラッグ時に選択が1つに解除される

**この最後の問題だけを修正**:

**原因**:
- ページ1,3,5を選択
- ページ3をクリック（Ctrl無し）
- → ListBoxが単一選択に変更してしまう

**解決策**:
- 複数選択されている状態で選択済みアイテムをクリックした場合
- **マウスイベントをキャンセル**して選択変更を防ぐ
- ただし、ドラッグは許可

**実装**:
```csharp
private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    // Ctrl/Shift選択時はListBoxに任せる
    if (Keyboard.Modifiers != ModifierKeys.None)
    {
        return;
    }

    // 複数選択保護: 選択済みアイテムのクリックはキャンセル
    if (sender is FrameworkElement element)
    {
        var listBox = FindParentListBox(element);
        if (listBox != null && listBox.SelectedItems.Count > 1)
        {
            var clickedItem = GetClickedItem(element, e);
            if (clickedItem != null && listBox.SelectedItems.Contains(clickedItem))
            {
                // 選択変更をキャンセル
                e.Handled = true;
            }
        }
    }

    _dragStartPoint = e.GetPosition(null);
    _isDragging = false;
}
```

**重要**: `PreviewMouseLeftButtonDown` を使用

---

### アプローチ3: 完全な再設計（長期的）

GongSolutions.WPF.DragDrop ライブラリの使用を検討

---

## 📌 推奨アクション

**短期的**: アプローチ2（V3.0.116ベースの修正）
- PreviewMouseLeftButtonDown に戻す
- 複数選択保護ロジックのみ実装
- `e.Handled = true` で選択変更をブロック

**長期的**: アプローチ1（ListBoxItemへの適用）
- より堅牢な実装
- 次のメジャーバージョンで検討

---

## 🎯 次のステップ

1. V3.0.116の実装に戻す
2. 複数選択保護ロジックを追加（`e.Handled = true`）
3. テスト実行
