# 複数ページ一括移動機能実装計画

**作成日時**: 2025-10-02
**対象バージョン**: V3.0.116
**目的**: 複数選択ページの上下移動ボタン対応

---

## 📋 現状分析

### ✅ 既に実装済み
- **ドラッグ&ドロップ**: 複数ページ選択対応済み
  - `HandlePageReorderAsync(List<V3PageViewModel> pagesToMove, ...)`
  - `ReorderPagesAsync` の両オーバーロードが `List<V3PageViewModel>` を受け取る
  - `MovePagesCommand` に複数ページ用コンストラクタ存在

### ❌ 未実装（問題箇所）
- **上下移動ボタン**: 最初の選択ページのみ移動
  - `MovePageUpAsync` (PageOperationViewModel.cs:372-416)
  - `MovePageDownAsync` (PageOperationViewModel.cs:418-465)

**問題コード**:
```csharp
// 最初の選択ページのみ取得
var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);

// 単一ページ用コンストラクタ使用
var command = new MovePagesCommand(
    _currentDocument,
    selectedPage.Page,
    currentIndex - 1,
    ...
);
```

---

## 🎯 実装目標

**ユーザーの要望**:
> 複数画像を選択して同時に画像の順番を入れ替えられるようにしたい

**具体例**:
- ページ 3, 5, 7 を選択
- 「上に移動」ボタンクリック
- → 3ページがまとめて1つ上に移動（相対位置を保持）

---

## 🔧 実装計画

### 1️⃣ MovePageUpAsync 修正

#### 変更箇所: PageOperationViewModel.cs:372-416

#### 現在のロジック
```csharp
// 最初の選択ページのみ取得
var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
var currentIndex = Pages.IndexOf(selectedPage);

// 単一移動
var command = new MovePagesCommand(_currentDocument, selectedPage.Page, currentIndex - 1, ...);
```

#### 新ロジック
```csharp
// 全ての選択ページを取得（インデックス順）
var selectedPages = Pages.Where(p => p.IsSelected)
                         .OrderBy(p => Pages.IndexOf(p))
                         .ToList();

// 選択状態を保存（V3.0.115パターン）
var selectedPageIds = selectedPages.Select(p => p.Id).ToHashSet();

// 各ページの移動先を計算
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

// 複数ページ用コンストラクタ使用
var command = new MovePagesCommand(
    _currentDocument,
    pageMoves,
    () => {
        // V3.0.115: 選択状態を保持してリフレッシュ
        RefreshPageListWithSelection(selectedPageIds);
        PagesChanged?.Invoke(this, EventArgs.Empty);
    }
);
```

#### エッジケース処理
| ケース | 処理 |
|--------|------|
| 連続ページ (3,4,5) | 一番上のページ(3)のみ移動、他は相対位置保持 |
| 非連続ページ (3,7,9) | それぞれ1つ上に移動 |
| 先頭ページ含む (1,3,5) | ページ1はスキップ、3と5のみ移動 |
| 全選択 | 移動不可メッセージ |

---

### 2️⃣ MovePageDownAsync 修正

#### 変更箇所: PageOperationViewModel.cs:418-465

#### 新ロジック
```csharp
// 全ての選択ページを取得（インデックス降順）
var selectedPages = Pages.Where(p => p.IsSelected)
                         .OrderByDescending(p => Pages.IndexOf(p))
                         .ToList();

// 選択状態を保存
var selectedPageIds = selectedPages.Select(p => p.Id).ToHashSet();

// 各ページの移動先を計算（下から処理）
var pageMoves = new List<(PdfPage page, int newPosition)>();
for (int i = 0; i < selectedPages.Count; i++)
{
    var page = selectedPages[i];
    int currentIndex = Pages.IndexOf(page);

    // 末尾ページは移動できない
    if (currentIndex >= Pages.Count - 1)
        continue;

    int newPosition = currentIndex + 1;

    // 直後のページが選択済みの場合は移動しない（相対位置保持）
    if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex + 1)
        continue;

    pageMoves.Add((page.Page, newPosition));
}

// 移動するページがない場合は終了
if (!pageMoves.Any())
{
    StatusMessage = "これ以上下に移動できません";
    return;
}

// 複数ページ用コンストラクタ使用
var command = new MovePagesCommand(
    _currentDocument,
    pageMoves,
    () => {
        // V3.0.115: 選択状態を保持してリフレッシュ
        RefreshPageListWithSelection(selectedPageIds);
        PagesChanged?.Invoke(this, EventArgs.Empty);
    }
);
```

---

## 🧪 テスト計画

### テストケース

| # | 選択ページ | 操作 | 期待結果 |
|---|-----------|------|----------|
| 1 | 3,4,5 | 上移動 | → 2,3,4 (連続保持) |
| 2 | 3,7,9 | 上移動 | → 2,6,8 (各1つ上) |
| 3 | 1,3,5 | 上移動 | → 1,2,4 (1はスキップ) |
| 4 | 3,4,5 | 下移動 | → 4,5,6 (連続保持) |
| 5 | 3,7,9 | 下移動 | → 4,8,10 (各1つ下) |
| 6 | 5,7,10 | 下移動 | → 6,8,10 (10はスキップ) |
| 7 | 全選択 | 上移動 | 移動不可メッセージ |
| 8 | 全選択 | 下移動 | 移動不可メッセージ |
| 9 | 3,5,7 | 上移動→下移動 | 選択状態保持確認 |

### 選択状態保持確認
- ✅ 移動後も同じページが選択されている
- ✅ Ctrl+クリックで追加選択可能
- ✅ 連続操作（回転→移動→削除）が可能

---

## 📝 実装手順

### Step 1: 分析完了報告
- [x] 現状分析完了
- [x] 実装計画作成

### Step 2: ユーザー確認
- [ ] 実装計画をユーザーに報告
- [ ] 実装方針の承認を得る

### Step 3: 実装
- [ ] MovePageUpAsync 修正
- [ ] MovePageDownAsync 修正
- [ ] ビルド実行（V3.0.116）

### Step 4: テスト
- [ ] 連続ページ移動テスト
- [ ] 非連続ページ移動テスト
- [ ] エッジケーステスト
- [ ] 選択状態保持確認

### Step 5: リリース
- [ ] GitHub push
- [ ] バージョン更新（CLAUDE.md, MainWindow.xaml, AssemblyVersion）

---

## 🔍 技術的注意点

### MovePagesCommand 複数ページコンストラクタ
```csharp
// MovePagesCommand.cs:70-93 に既存
public MovePagesCommand(
    PdfDocument document,
    List<(PdfPage page, int newPosition)> pageMoves,
    Action onPagesChanged
)
```

### RefreshPageListWithSelection パターン
```csharp
// V3.0.115で確立されたパターン
var selectedPageIds = Pages.Where(p => p.IsSelected)
                           .Select(p => p.Id)
                           .ToHashSet();

// ... 処理 ...

RefreshPageListWithSelection(selectedPageIds);  // Dispatcher.InvokeAsync内部で実装
```

### Dispatcher 非同期パターン（必要な場合）
```csharp
// RefreshPageListWithSelection内部で既に実装済み
System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
{
    _enableSelectionEvents?.Invoke();
}, System.Windows.Threading.DispatcherPriority.Loaded);
```

---

## ✅ 成功基準

1. **機能要件**
   - ✅ 複数ページ選択時、上下移動ボタンで全選択ページが移動
   - ✅ 相対位置が保持される（連続ページは連続のまま）
   - ✅ エッジケース（先頭・末尾含む選択）が正しく処理される

2. **品質要件**
   - ✅ 選択状態が移動後も保持される
   - ✅ Undo/Redo が正常動作
   - ✅ 連続操作（回転→移動→削除）が可能

3. **コード品質**
   - ✅ V3.0.115で確立されたパターンに準拠
   - ✅ ハードコード・モックコード禁止
   - ✅ 既存のMovePagesCommand複数ページコンストラクタを活用

---

## 📌 参考資料

- **V3.0.115 選択状態保持修正**: Dispatcher.InvokeAsync パターン確立
- **MovePagesCommand**: `src/DocOrganizer.Core/Commands/MovePagesCommand.cs`
- **PageOperationViewModel**: `src/DocOrganizer.UI/ViewModels/V3/PageOperationViewModel.cs`
- **CLAUDE.md 第15条**: バグ修正プロセス（分析→報告→確認→修正）

---

## 🎯 次のアクション

**ユーザーに報告内容**:
1. 現状分析結果（ドラッグ&ドロップは対応済み、上下ボタンのみ未対応）
2. 実装方針（MovePagesCommand複数ページコンストラクタ活用、V3.0.115パターン準拠）
3. テスト計画（9種類のテストケース）
4. 実装承認の確認

**承認後の作業**:
- MovePageUpAsync/MovePageDownAsync 修正
- ビルド＆テスト実行
- V3.0.116 リリース
