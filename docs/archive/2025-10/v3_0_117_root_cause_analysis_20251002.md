# V3.0.117 複数選択バグ根本原因分析

**作成日時**: 2025-10-02  
**分析対象**: V3.0.115 → V3.0.117 の変更による複数選択機能破損

---

## 🔍 実施した分析

### 1. Git差分の完全確認

```bash
git stash show -p 'stash@{0}' --name-only
```

**変更されたファイル**:
1. `V3AdvancedDragDropBehavior.cs` - コメント追加のみ
2. `V3DragDropInfo.cs` - SelectedItemsプロパティ追加
3. `DragDropHandlerViewModel.cs` - 複数ページドロップ対応
4. `PageOperationViewModel.cs` - ボタンで複数ページ移動対応
5. `MainWindow.xaml` - バージョン表示のみ
6. `DocOrganizer.UI.csproj` - バージョン番号のみ

### 2. 各差分の詳細検証

#### V3AdvancedDragDropBehavior.cs
```diff
+            // 🆕 V3.0.117: MouseLeftButtonDownを使用（ListBoxの選択処理後に実行）
             element.MouseLeftButtonDown += OnMouseLeftButtonDown;
```
- **実質的変更**: なし（コメント追加のみ）
- **選択への影響**: なし

#### V3DragDropInfo.cs
```csharp
+        // 🆕 V3.0.116: 複数選択対応プロパティ
+        public List<object>? SelectedItems { get; private set; }

+        // 🆕 V3.0.116: 親ListBoxから複数選択を取得
+        var listBox = FindAncestor<ListBox>(listBoxItem);
+        if (listBox != null && listBox.SelectedItems.Count > 0)
+        {
+            SelectedItems = listBox.SelectedItems.Cast<object>().ToList();
+        }
```
- **変更内容**: SelectedItemsプロパティ追加とコンストラクタでの設定
- **選択への影響**: **読み取り専用** - ListBoxの選択を読むだけで変更しない

#### DragDropHandlerViewModel.cs
```csharp
-        private static readonly Dictionary<string, V3PageViewModel> _dragCache = new();
+        // 🎯 V3.0.116: 複数ページ対応 - object型でV3PageViewModelまたはList<V3PageViewModel>を格納
+        private static readonly Dictionary<string, object> _dragCache = new();
```
- **変更内容**: ドロップ処理で複数ページ対応
- **選択への影響**: **ドロップ後の処理** - ドラッグ開始時の選択には影響しない

#### PageOperationViewModel.cs
```csharp
-            var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
+            var selectedPages = Pages.Where(p => p.IsSelected)
+                                     .OrderBy(p => Pages.IndexOf(p))
+                                     .ToList();
```
- **変更内容**: ボタン処理で複数ページ対応
- **選択への影響**: **読み取り専用** - 選択を読むだけで変更しない

---

## ❌ 重大な結論

### **コード変更は複数選択を壊さない**

全ての差分を検証した結果：
- ✅ V3AdvancedDragDropBehavior.cs: 実質変更なし
- ✅ V3DragDropInfo.cs: 読み取り専用の追加機能
- ✅ DragDropHandlerViewModel.cs: ドロップ後の処理のみ
- ✅ PageOperationViewModel.cs: 読み取り専用の処理

**いずれもListBoxの選択メカニズムを変更・干渉していない**

---

## 🎯 真の原因仮説

### 仮説1: V3.0.115も複数選択は動作していなかった

**可能性**: ユーザーが「V3.0.115は動いていた」と思っているが、実際は動いていなかった

**検証方法**:
```bash
# V3.0.115を実際にpublishして動作確認
git checkout 3b2dfd3
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o test-v3.0.115
```

### 仮説2: MainWindow.xaml.cs が関与している

**可能性**: MainWindow.xaml.cs（git statusで変更検出）に複数選択を妨げる変更があった

**検証方法**:
```bash
git diff 3b2dfd3 HEAD -- src/DocOrganizer.UI/Views/MainWindow.xaml.cs
```

### 仮説3: ビルド時の環境問題

**可能性**: .NETランタイム、NuGetパッケージ、またはキャッシュの問題

**検証方法**:
```bash
git clean -xfd
dotnet restore
dotnet build
```

---

## 📋 次のアクションプラン

### アクション1: V3.0.115の実際の動作確認
```bash
git checkout 3b2dfd3
dotnet clean
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o test-v3.0.115
# 実際にCtrl+クリック複数選択をテスト
```

### アクション2: MainWindow.xaml.cs の差分確認
```bash
git diff 3b2dfd3 HEAD -- src/DocOrganizer.UI/Views/MainWindow.xaml.cs
```

### アクション3: 全変更ファイルの確認
```bash
git status
# .serena/cache以外の変更ファイルを確認
```

### アクション4: クリーンビルドテスト
```bash
git stash pop
git clean -xfd -e .tmp -e .logs
dotnet restore
dotnet build
```

---

## 🚨 重要な学び

### コード差分だけでは原因が見つからない

- stashした差分には複数選択を壊す変更がない
- しかしユーザーは「複数選択できない」と報告
- → **コード以外の要因** または **元々動いていなかった** 可能性

### 動作確認の重要性

- 仮定ではなく **実際の動作** で検証すべき
- V3.0.115のEXEを実際に起動して確認が必要

---

## 💡 推奨される対応

1. **V3.0.115を実際にビルド**して動作確認
2. MainWindow.xaml.csの変更を確認
3. クリーンビルドでキャッシュ問題を排除
4. 上記で解決しない場合は **ListBoxのXAML設定** を再確認

---

**作成者**: Claude (Serena MCP使用)  
**次のステップ**: V3.0.115の実動作確認
