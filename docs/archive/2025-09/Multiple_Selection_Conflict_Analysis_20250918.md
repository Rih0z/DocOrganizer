# 複数選択機能不具合 - 根本原因分析レポート

## 対象概要
- **種別**: バグ修正
- **対象システム**: DocOrganizer V3 ページ複数選択機能
- **分析日時**: 2025-09-18
- **問題**: Ctrl+クリックによる複数選択が機能しない

---

## 🔍 根本原因の特定

### 問題の核心
MainWindow.xaml.cs の PageListBox_SelectionChanged イベントハンドラー内に、**複数選択状態を破壊するコード**が存在する。

### 具体的な問題箇所

#### MainWindow.xaml.cs (行576-585) - ✅ 正しい処理
```csharp
// 全ページの選択状態を更新
foreach (V3PageViewModel page in V3ViewModel.PageOperation.Pages)
{
    bool shouldBeSelected = listBox.SelectedItems.Contains(page);
    if (page.IsSelected != shouldBeSelected)
    {
        page.IsSelected = shouldBeSelected;
        System.Diagnostics.Debug.WriteLine($"[複数選択] Page {page.PageNumber}: IsSelected = {shouldBeSelected}");
    }
}
```
この部分は正しくListBoxの複数選択状態をViewModelに同期している。

#### MainWindow.xaml.cs (行622-625) - ❌ 問題のコード
```csharp
// 選択状態を明示的に設定
foreach (var page in V3ViewModel.Pages)
{
    page.IsSelected = (page == selectedPage);  // ⚠️ 単一選択を強制！
}
```
**この部分が複数選択を破壊している！** selectedPage以外のすべてのページのIsSelectedをfalseにしてしまう。

---

## 📊 処理フローの分析

### 現在の処理フロー（バグあり）
1. ユーザーがCtrl+クリックで複数選択
2. ListBoxが内部的に複数選択状態を保持
3. SelectionChangedイベント発火
4. **前半**: 正しく複数選択状態をViewModelに同期 ✅
5. **後半**: 単一選択を強制し、複数選択を破壊 ❌
6. 結果: 最後にクリックしたアイテムのみ選択される

### V3.0.102の修正が失敗した理由
- IsSelectedバインディングを追加したが、イベントハンドラーが選択状態を上書きするため効果なし
- むしろ双方向バインディングによる競合が発生し、状態が悪化

---

## 🛠️ 推奨修正方法

### 修正方針
問題のコード（行622-625）を削除またはコメントアウトし、複数選択に対応した処理に変更する。

### 具体的な修正

#### MainWindow.xaml.cs の修正
```csharp
private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    try
    {
        _logger?.LogInformation("PageListBox_SelectionChanged event fired");
        
        if (sender is ListBox listBox)
        {
            System.Diagnostics.Debug.WriteLine($"[PageListBox_SelectionChanged] ListBox found, SelectedItems.Count: {listBox.SelectedItems.Count}");
            
            // 🔧 複数選択対応: ListBoxの選択状態をViewModelに同期
            if (V3ViewModel?.PageOperation?.Pages != null)
            {
                // 全ページの選択状態を更新
                foreach (V3PageViewModel page in V3ViewModel.PageOperation.Pages)
                {
                    bool shouldBeSelected = listBox.SelectedItems.Contains(page);
                    if (page.IsSelected != shouldBeSelected)
                    {
                        page.IsSelected = shouldBeSelected;
                        System.Diagnostics.Debug.WriteLine($"[複数選択] Page {page.PageNumber}: IsSelected = {shouldBeSelected}");
                    }
                }
                
                // 選択状態の更新を通知
                V3ViewModel.PageOperation.NotifyPageSelectionChanged();
                
                System.Diagnostics.Debug.WriteLine($"[複数選択] 選択ページ数: {listBox.SelectedItems.Count}");
            }
            
            // プレビュー更新（最初の選択ページまたは最後に選択したページ）
            if (listBox.SelectedItem is V3PageViewModel selectedPage && V3ViewModel != null)
            {
                _logger?.LogInformation($"Selected page for preview: {selectedPage.PageNumber}");
                
                // 🎯 V3対応: MainCompositeViewModel.SelectedPageを更新
                V3ViewModel.SelectedPage = selectedPage;
                
                // デバッグログ
                System.Diagnostics.Debug.WriteLine($"[右側プレビューデバッグ] SelectedPage設定完了: PageNumber={selectedPage.PageNumber}");
                
                // ⚠️ 削除または修正が必要な部分
                // foreach (var page in V3ViewModel.Pages)
                // {
                //     page.IsSelected = (page == selectedPage);  // 複数選択を破壊
                // }
                
                // ページ選択状態を更新（上下移動ボタンの有効化）
                if (V3ViewModel.PageOperation != null)
                {
                    V3ViewModel.PageOperation.NotifyPageSelectionChanged();
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error in PageListBox_SelectionChanged");
    }
}
```

---

## ✅ 修正の効果

### 期待される改善
1. **Ctrl+クリック**: 個別の複数選択が正常動作
2. **Shift+クリック**: 範囲選択が正常動作
3. **選択状態の保持**: 複数選択状態が正しくViewModelに反映
4. **プレビュー表示**: 最後に選択したページのプレビューを表示

### 副作用の評価
- **影響なし**: 単一選択時の動作は従来通り
- **改善**: 複数選択による一括操作（回転、削除など）が可能に

---

## 📝 実装順序

1. **MainWindow.xaml.cs の修正**
   - 行622-625の単一選択強制コードを削除/コメントアウト
   
2. **動作テスト**
   - Ctrl+クリックによる複数選択
   - Shift+クリックによる範囲選択
   - プレビュー表示の確認

3. **バージョン更新**
   - V3.0.102として正式リリース

---

## 💡 追加の考慮事項

### 将来的な改善案
1. **選択モードの切り替え機能**
   - 単一選択モードと複数選択モードの切り替えオプション

2. **選択状態の視覚的フィードバック強化**
   - 選択数の表示
   - 選択アイテムのハイライト強化

3. **キーボード操作の拡張**
   - Ctrl+A による全選択
   - Ctrl+Shift+A による選択解除

---

**分析完了**: 2025-09-18  
**推奨対応**: 即座に修正可能（5分程度）  
**リスク評価**: 低リスク（単純なコード削除/修正）