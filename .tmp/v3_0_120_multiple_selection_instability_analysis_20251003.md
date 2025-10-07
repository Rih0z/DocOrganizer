# V3.0.120 複数選択不安定問題・完全分析レポート

**日時**: 2025-10-03  
**バージョン**: V3.0.120  
**分析ツール**: Serena MCP  
**問題**: 3枚以上の複数選択ができない・選択が不安定

---

## 🔍 発見された根本原因

### 1️⃣ **MainWindow.xaml.cs `PageListBox_SelectionChanged`の二重バインディング問題**

**ファイル**: `src/DocOrganizer.UI/Views/MainWindow.xaml.cs` (Line 583-665)

**問題コード**:
```csharp
private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ListBox listBox)
    {
        // ⚠️ 問題1: 選択状態の手動同期がバインディングループを引き起こす
        foreach (V3PageViewModel page in V3ViewModel.PageOperation.Pages)
        {
            bool shouldBeSelected = listBox.SelectedItems.Contains(page);
            if (page.IsSelected != shouldBeSelected)
            {
                page.IsSelected = shouldBeSelected;  // ❌ これが新しいSelectionChangedを発火
            }
        }
    }
}
```

**なぜ問題なのか**:
1. ListBoxの`SelectionChanged`イベント発火
2. コードビハインドで`page.IsSelected`を変更
3. `IsSelected`は`TwoWayBinding`されている（MainWindow.xaml Line 504）
4. ViewModelの変更がListBoxに伝播
5. ListBoxが再び`SelectionChanged`を発火 → **無限ループの危険性**

**MainWindow.xaml該当箇所**:
```xaml
<Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"/>
```

---

### 2️⃣ **二重の選択状態管理システム**

現在のアーキテクチャでは選択状態が**2箇所**で管理されています：

| 管理場所 | 説明 | 更新タイミング |
|---------|------|--------------|
| **ListBox.SelectedItems** | WPF標準の選択コレクション | ユーザーのCtrl+クリック時 |
| **V3PageViewModel.IsSelected** | ViewModelプロパティ | バインディング経由 |

この二重管理が以下の問題を引き起こします：

1. **タイミングの不一致**: ListBoxの選択とViewModelの同期にラグがある
2. **イベントの連鎖**: 片方の変更がもう片方を変更し、それが再び片方を変更...
3. **競合状態**: 高速なクリック時に状態が不整合になる

---

### 3️⃣ **V3AdvancedDragDropBehaviorは無罪**

**検証結果**: V3.0.120の`OnMouseLeftButtonDown`は**最小限の処理のみ**：

```csharp
private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    _dragStartPoint = e.GetPosition(null);
    _isDragging = false;
}
```

- `e.Handled = true`を使用していない → イベント伝播をブロックしない
- Ctrl/Shiftチェックなし → 標準選択ロジックに干渉しない
- **結論**: Behaviorは複数選択問題の原因ではない

---

## 🎯 根本原因のメカニズム図

```
ユーザーがCtrl+クリック
       ↓
ListBoxの標準SelectionChanged発火
       ↓
PageListBox_SelectionChanged実行
       ↓
foreach (page) { page.IsSelected = shouldBeSelected; }
       ↓
TwoWayBindingによりListBoxに変更伝播
       ↓
再びSelectionChangedが発火（タイミング次第）
       ↓
競合状態・選択解除・不安定動作
```

---

## 💡 解決策

### **推奨アプローチ**: 二重管理の廃止

#### オプションA: ViewModelの`IsSelected`のみを信頼する

**変更箇所1**: MainWindow.xaml.cs
```csharp
private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    // ❌ 削除: 手動同期ロジック全体を削除
    // foreach (page) { page.IsSelected = ... }
    
    // ✅ 単一選択時のプレビュー更新のみ残す
    if (listBox.SelectedItem is V3PageViewModel selectedPage)
    {
        V3ViewModel.SelectedPage = selectedPage;
    }
}
```

**理由**: `TwoWayBinding`が既に同期を保証しているため、手動同期は不要かつ有害

#### オプションB: `SelectionChanged`の完全削除

**変更箇所**: MainWindow.xaml
```xaml
<!-- ❌ 削除 -->
<!-- SelectionChanged="PageListBox_SelectionChanged" -->
```

**変更箇所**: V3PageViewModelにプロパティ変更通知を追加
```csharp
public bool IsSelected
{
    get => _isSelected;
    set
    {
        if (SetProperty(ref _isSelected, value))
        {
            // プレビュー更新をここで実行
            if (value) UpdatePreview();
        }
    }
}
```

**理由**: バインディングに完全依存し、コードビハインドのイベント処理を排除

---

## 🔬 検証が必要な追加要因

### 1. `NotifyPageSelectionChanged()`の影響

**ファイル**: PageOperationViewModel.cs

```csharp
public void NotifyPageSelectionChanged()
{
    OnPropertyChanged(nameof(HasSelectedPages));
    OnPropertyChanged(nameof(CanMoveUp));
    OnPropertyChanged(nameof(CanMoveDown));
    // ⚠️ これが追加のUIイベントを発火させている可能性
}
```

### 2. ObservableCollection変更通知の影響

V3PageViewModelの`IsSelected`変更が`Pages`コレクション全体の再描画を引き起こしている可能性

---

## 📋 実装優先順位

### Phase 1: 最小限の修正（推奨）

1. ✅ `PageListBox_SelectionChanged`内のforeachループを削除
2. ✅ 単一選択プレビュー更新のみ残す
3. ✅ バージョン3.0.121でビルド＆テスト

**期待結果**: バインディングループが解消され、複数選択が安定化

### Phase 2: 完全リファクタリング（必要に応じて）

1. SelectionChangedイベント完全削除
2. ViewModelベースの選択管理に一本化
3. MVVMパターンの純粋な実装

---

## 🚨 警告：やってはいけないこと

### ❌ **ListBoxItem-levelのBehavior設定**
- V3.0.118の失敗から学習
- 個別アイテムへのMouseLeftButtonDown登録は選択を完全破壊

### ❌ **Ctrl/Shiftキーの早期リターン**
- V3.0.119の失敗から学習
- MouseLeftButtonDownでの早期returnは逆効果

### ❌ **e.Handled = trueの安易な使用**
- イベント伝播を止めると予期しない副作用

---

## 📊 テストケース

修正後は以下を全て検証すること：

| # | 操作 | 期待動作 |
|---|------|---------|
| 1 | 1枚目クリック | 1枚選択 |
| 2 | Ctrl+2枚目クリック | 2枚選択（1枚目も維持） |
| 3 | Ctrl+3枚目クリック | 3枚選択（1,2枚目も維持） |
| 4 | Ctrl+4枚目クリック | 4枚選択（全て維持） |
| 5 | Shift+5枚目クリック | 1-5枚目範囲選択 |
| 6 | 空白部分クリック | 全選択解除 |
| 7 | 選択済みアイテムをCtrlなしクリック | そのアイテムのみ選択 |
| 8 | 選択済み複数アイテムをドラッグ | 全選択維持してドラッグ開始 |

---

## 🎓 学んだ教訓

1. **TwoWayBindingと手動同期の併用は禁止**: どちらか一方に統一
2. **WPFの標準動作を信頼する**: 独自ロジックで上書きしない
3. **イベントハンドラは最小限に**: コードビハインドは極力避ける
4. **MVVMパターンの徹底**: ViewはViewModelの変更に反応するだけ

---

## 次のアクション

1. **Phase 1実装**: `PageListBox_SelectionChanged`のforeachループ削除
2. **V3.0.121ビルド**: 修正版の作成
3. **完全テスト**: 8つのテストケース全てを検証
4. **ユーザー確認**: 3枚以上の複数選択が安定動作することを確認

---

**分析完了**: 根本原因は`PageListBox_SelectionChanged`内の二重バインディングによる競合状態  
**解決策**: 手動同期ロジックの削除、TwoWayBindingのみに依存
