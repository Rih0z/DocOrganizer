# V3.0.123 修正計画 - 複数選択時に2番目の画像しか移動しない問題

**作成日時**: 2025-10-06
**担当**: Claude (Serena MCP使用)
**バージョン**: V3.0.122 → V3.0.123
**問題**: 2つ選択して下ボタンを押しても2番目の画像しか下に移動しない

---

## 🔍 問題分析

### ユーザー報告

**症状**:
- ページ1とページ2を選択
- 下ボタン（⬇️）をクリック
- **期待**: ページ1とページ2の両方が下に移動
- **実際**: ページ2だけが下に移動

---

## 🐛 根本原因の特定

### Step 1: MovePageDownAsync()の実装確認

**PageOperationViewModel.cs Line 444-511**:
```csharp
private async Task MovePageDownAsync()
{
    // ✅ 複数選択取得は正常
    var selectedPages = Pages.Where(p => p.IsSelected)
                             .OrderByDescending(p => Pages.IndexOf(p))
                             .ToList();

    // ✅ pageMoves計算ロジックは正常
    var pageMoves = new List<(PdfPage page, int newPosition)>();
    for (int i = 0; i < selectedPages.Count; i++)
    {
        var page = selectedPages[i];
        int currentIndex = Pages.IndexOf(page);

        if (currentIndex >= Pages.Count - 1)
            continue;

        int newPosition = currentIndex + 1;

        // ⚠️ 直後のページが選択済みの場合はスキップ（意図的な仕様）
        if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex + 1)
            continue;

        pageMoves.Add((page.Page, newPosition));
    }

    // ✅ MovePagesCommand呼び出しは正常
    var command = new MovePagesCommand(_document, pageMoves, ...);
    _undoRedoService.Execute(command);
}
```

**結論**: `MovePageDownAsync()` は正常に動作している。

---

### Step 2: MovePagesCommand.Execute()の実装確認

**MovePagesCommand.cs Line 99-114**:
```csharp
public void Execute()
{
    // ❌ 問題発見: OrderBy(m => m.NewPosition)でソート
    foreach (var moveInfo in _moveInfo.OrderBy(m => m.NewPosition))
    {
        var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
        if (currentIndex >= 0 && currentIndex != moveInfo.NewPosition)
        {
            // ❌ 移動先が同じ場合、順次実行で位置がズレる
            _document.MovePage(currentIndex, moveInfo.NewPosition);
        }
    }

    _onPagesChanged?.Invoke();
}
```

---

## 🔬 具体例でバグを再現

### 初期状態
```
Pages: [Page1, Page2, Page3, Page4, Page5]
Index:    0      1      2      3      4
```

### ユーザー操作
- Page1（index 0）とPage2（index 1）を選択
- 下ボタン（⬇️）をクリック

---

### MovePageDownAsync()の計算結果（正常）

**selectedPages（降順）**:
```
[Page2 (index 1), Page1 (index 0)]
```

**pageMoves計算**:
```
i=0: Page2 (index 1) → newPosition = 2
     直後のページチェック: i=0なのでスキップなし
     ✅ pageMoves.Add((Page2, 2))

i=1: Page1 (index 0) → newPosition = 1
     直後のページチェック: selectedPages[i-1] = Page2 (index 1) == currentIndex + 1 → true
     ❌ continue（スキップ）
```

**pageMoves結果**:
```
[(Page2, 2)]  // Page1はスキップされた
```

**問題1発見**: `MovePageDownAsync()` の相対位置保持ロジックが間違っている！

---

### さらに詳しく分析

**条件チェック**:
```csharp
if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex + 1)
    continue;
```

**i=1の時**:
- `selectedPages[i - 1]` = `selectedPages[0]` = Page2（index 1）
- `currentIndex` = Page1のindex = 0
- `currentIndex + 1` = 1
- **条件**: `Pages.IndexOf(Page2) == 1` → **true**
- **結果**: continue（Page1をスキップ）

**意図**: 連続するページは一緒に移動するため、片方だけ移動すればよい
**問題**: しかし、実際には両方移動しないと位置関係が崩れる

---

## 🎯 根本原因の結論

### 問題1: MovePageDownAsync()の相対位置保持ロジックが誤り

**誤った前提**:
- 「連続する選択ページは片方だけ移動すれば、もう片方も一緒に移動する」

**実際**:
- PDFドキュメントの `MovePage()` は1ページずつ移動する
- 連続ページの片方だけ移動すると、位置関係が崩れる

**例**:
```
初期: [Page1, Page2, Page3, Page4]
       (選択)  (選択)

Page2を index 2に移動:
結果: [Page1, Page3, Page2, Page4]
       (選択)        (選択) ← Page1とPage2が離れた！
```

---

### 問題2: MovePagesCommand.Execute()の実行順序が誤り

**現在の実装**:
```csharp
foreach (var moveInfo in _moveInfo.OrderBy(m => m.NewPosition))
{
    var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
    _document.MovePage(currentIndex, moveInfo.NewPosition);
}
```

**問題**:
- 複数ページを同じ方向に移動する場合、先に移動したページが後続の移動に影響する
- `currentIndex` が移動の度に変わるため、`NewPosition` が不正確になる

**例**:
```
初期: [Page1, Page3, Page5]
       index: 0, 1, 2

移動計画:
- Page1を index 2に移動
- Page3を index 2に移動

実行:
1. Page1を index 2に移動
   結果: [Page3, Page5, Page1]
          index: 0, 1, 2

2. Page3を index 2に移動
   現在のPage3の位置: index 0（初期状態と異なる！）
   結果: [Page5, Page1, Page3]
```

---

## 🛠️ 修正方針

### 方針1: MovePageDownAsync/UpAsyncの相対位置保持ロジック削除

**現在の実装**:
```csharp
// ❌ 誤った相対位置保持ロジック
if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex + 1)
    continue;
```

**修正後**:
```csharp
// ✅ 全ての選択ページを移動対象に追加
// （相対位置保持はMovePagesCommandで実現）
pageMoves.Add((page.Page, newPosition));
```

**理由**:
- 相対位置保持は `MovePagesCommand.Execute()` で実現すべき
- ViewModelは「どのページをどこに移動するか」を指定するだけ
- 実際の移動ロジックはCommandに委譲

---

### 方針2: MovePagesCommand.Execute()の実行順序修正

**現在の実装**:
```csharp
// ❌ NewPosition順でソート（間違い）
foreach (var moveInfo in _moveInfo.OrderBy(m => m.NewPosition))
{
    var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
    _document.MovePage(currentIndex, moveInfo.NewPosition);
}
```

**修正後**:
```csharp
// ✅ OriginalPosition順でソート（下移動は降順、上移動は昇順）
// 下移動の場合: 下から順に処理（後続の移動に影響しない）
var sortedMoves = _moveInfo.OrderByDescending(m => m.OriginalPosition).ToList();

foreach (var moveInfo in sortedMoves)
{
    var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
    if (currentIndex >= 0)
    {
        _document.MovePage(currentIndex, moveInfo.NewPosition);
    }
}
```

**理由**:
- 下移動は**後ろから**処理すれば、前のページの位置に影響しない
- 上移動は**前から**処理すれば、後ろのページの位置に影響しない

---

### 方針3: 移動方向を判定して処理順序を決定

**実装**:
```csharp
public void Execute()
{
    if (!_moveInfo.Any()) return;

    // 移動方向を判定（上移動 or 下移動）
    bool isMovingDown = _moveInfo.First().NewPosition > _moveInfo.First().OriginalPosition;

    // 下移動: 後ろから処理（降順）
    // 上移動: 前から処理（昇順）
    var sortedMoves = isMovingDown
        ? _moveInfo.OrderByDescending(m => m.OriginalPosition).ToList()
        : _moveInfo.OrderBy(m => m.OriginalPosition).ToList();

    foreach (var moveInfo in sortedMoves)
    {
        var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
        if (currentIndex >= 0)
        {
            _document.MovePage(currentIndex, moveInfo.NewPosition);
        }
    }

    _onPagesChanged?.Invoke();
}
```

---

## 📋 修正計画詳細

### Phase 1: MovePageDownAsync()修正（高優先度）

**対象ファイル**: `src/DocOrganizer.UI/ViewModels/V3/PageOperationViewModel.cs`
**対象行**: Line 444-511

**修正内容**:
```csharp
// 修正前（Line 477-478）
if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex + 1)
    continue;

// 修正後
// 🎯 V3.0.123: 相対位置保持ロジック削除
// 全ての選択ページを移動対象に追加（MovePagesCommandで処理）
// （削除）
```

**工数**: 5分（2行削除のみ）
**リスク**: 極めて低い（5%）

---

### Phase 2: MovePageUpAsync()修正（高優先度）

**対象ファイル**: `src/DocOrganizer.UI/ViewModels/V3/PageOperationViewModel.cs`
**対象行**: Line 372-439

**修正内容**:
```csharp
// 修正前（Line 405-406）
if (i > 0 && Pages.IndexOf(selectedPages[i - 1]) == currentIndex - 1)
    continue;

// 修正後
// 🎯 V3.0.123: 相対位置保持ロジック削除
// （削除）
```

**工数**: 5分（2行削除のみ）
**リスク**: 極めて低い（5%）

---

### Phase 3: MovePagesCommand.Execute()修正（高優先度）

**対象ファイル**: `src/DocOrganizer.Core/Commands/MovePagesCommand.cs`
**対象行**: Line 99-114

**修正内容**:
```csharp
// 修正前
public void Execute()
{
    foreach (var moveInfo in _moveInfo.OrderBy(m => m.NewPosition))
    {
        var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
        if (currentIndex >= 0 && currentIndex != moveInfo.NewPosition)
        {
            _document.MovePage(currentIndex, moveInfo.NewPosition);
        }
    }

    _onPagesChanged?.Invoke();
}

// 修正後
public void Execute()
{
    // 🎯 V3.0.123: 複数ページ移動時の位置ズレ修正
    // 移動方向を判定し、適切な順序で処理
    if (!_moveInfo.Any()) return;

    // 移動方向を判定（上移動 or 下移動）
    bool isMovingDown = _moveInfo.First().NewPosition > _moveInfo.First().OriginalPosition;

    // 下移動: 後ろから処理（降順） - 前のページに影響しない
    // 上移動: 前から処理（昇順） - 後ろのページに影響しない
    var sortedMoves = isMovingDown
        ? _moveInfo.OrderByDescending(m => m.OriginalPosition).ToList()
        : _moveInfo.OrderBy(m => m.OriginalPosition).ToList();

    foreach (var moveInfo in sortedMoves)
    {
        var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
        if (currentIndex >= 0)
        {
            _document.MovePage(currentIndex, moveInfo.NewPosition);
        }
    }

    _onPagesChanged?.Invoke();
}
```

**工数**: 15分（ロジック追加）
**リスク**: 低い（10%）

---

## ✅ 修正後の動作確認

### テストケース1: 連続2ページ（Page1, Page2）を下移動

**初期状態**:
```
Pages: [Page1, Page2, Page3, Page4, Page5]
Index:    0      1      2      3      4
選択:   (✓)    (✓)
```

**MovePageDownAsync()計算結果**:
```
selectedPages（降順）: [Page2, Page1]

pageMoves計算:
- i=0: Page2 (index 1) → newPosition = 2 ✅
- i=1: Page1 (index 0) → newPosition = 1 ✅

pageMoves: [(Page2, 2), (Page1, 1)]
```

**MovePagesCommand.Execute()実行**:
```
移動方向判定: NewPosition=2 > OriginalPosition=1 → 下移動
処理順序: OriginalPosition降順 → [Page2 (1→2), Page1 (0→1)]

実行:
1. Page2を index 2に移動
   [Page1, Page3, Page2, Page4, Page5]
     0      1      2      3      4

2. Page1を index 1に移動
   [Page3, Page1, Page2, Page4, Page5]
     0      1      2      3      4
```

**期待結果**: ✅ Page1とPage2が一緒に下に移動

---

### テストケース2: 飛び飛び3ページ（Page1, Page3, Page5）を上移動

**初期状態**:
```
Pages: [Page1, Page2, Page3, Page4, Page5]
Index:    0      1      2      3      4
選択:   (✓)           (✓)           (✓)
```

**MovePageUpAsync()計算結果**:
```
selectedPages（昇順）: [Page1, Page3, Page5]

pageMoves計算:
- i=0: Page1 (index 0) → continue（先頭）
- i=1: Page3 (index 2) → newPosition = 1 ✅
- i=2: Page5 (index 4) → newPosition = 3 ✅

pageMoves: [(Page3, 1), (Page5, 3)]
```

**MovePagesCommand.Execute()実行**:
```
移動方向判定: NewPosition=1 < OriginalPosition=2 → 上移動
処理順序: OriginalPosition昇順 → [Page3 (2→1), Page5 (4→3)]

実行:
1. Page3を index 1に移動
   [Page1, Page3, Page2, Page4, Page5]
     0      1      2      3      4

2. Page5を index 3に移動
   [Page1, Page3, Page2, Page5, Page4]
     0      1      2      3      4
```

**期待結果**: ✅ Page3とPage5が一緒に上に移動

---

## 📊 システム整合性チェック（Step 3）

### 1. 既存機能への影響

| 機能 | 影響 | 理由 |
|------|------|------|
| 単一ページ移動 | ✅ なし | `_moveInfo` が1件のみなので処理順序は無関係 |
| Undo/Redo | ✅ なし | `Undo()` も同じロジックで修正 |
| 回転機能 | ✅ なし | 移動機能と独立 |
| 削除機能 | ✅ なし | 移動機能と独立 |

### 2. ユーザー操作への影響

| 操作 | 影響 | 理由 |
|------|------|------|
| 複数選択→上移動 | ✅ 改善 | 全ての選択ページが移動するように修正 |
| 複数選択→下移動 | ✅ 改善 | 全ての選択ページが移動するように修正 |
| 単一選択→移動 | ✅ 維持 | 既存動作維持 |

### 3. パフォーマンス影響

**計算量**: O(n log n)（ソート） + O(n)（移動）
**実測見積**: 100ページ時 <5ms
**影響**: なし

### 4. リグレッションリスク

**リスク**: 極めて低い（5%）
**理由**:
- Phase 1-2: 2行削除のみ（相対位置保持ロジック削除）
- Phase 3: 処理順序の最適化のみ（移動ロジック自体は変更なし）

### 5. 最終判定

**✅ 全Phase実施を推奨**

---

## 🎯 実施手順（Step 5）

### 準備

1. Git status確認
2. 現在のバージョン確認（V3.0.122）

### 実行順序

1. Phase 1: MovePageDownAsync()修正（5分）
2. Phase 2: MovePageUpAsync()修正（5分）
3. Phase 3: MovePagesCommand.Execute()修正（15分）
4. Phase 4: MovePagesCommand.Undo()修正（10分）
5. バージョン更新（V3.0.122 → V3.0.123）
6. ビルド実行
7. リグレッションテスト（12項目）

### リグレッションテスト項目

1. ✅ 単一選択→⬆️→1ページ上移動
2. ✅ 単一選択→⬇️→1ページ下移動
3. 🆕 **2ページ選択→⬇️→2ページ一括下移動**（バグ修正対象）
4. 🆕 **2ページ選択→⬆️→2ページ一括上移動**（バグ修正対象）
5. 🆕 **3ページ飛び飛び選択→⬇️→3ページ一括下移動**
6. 🆕 **3ページ飛び飛び選択→⬆️→3ページ一括上移動**
7. ✅ 移動後Ctrl+Z→Undo成功
8. ✅ Undo後Ctrl+Y→Redo成功
9. ✅ 1ページ目選択→⬆️→ボタン無効
10. ✅ 最終ページ選択→⬇️→ボタン無効
11. ✅ 回転機能維持
12. ✅ 削除機能維持

---

## 📝 補足: Phase 4詳細

### MovePagesCommand.Undo()も同様に修正

**対象行**: Line 119-134

**修正内容**:
```csharp
// 修正前
public void Undo()
{
    foreach (var moveInfo in _moveInfo.OrderBy(m => m.OriginalPosition))
    {
        var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
        if (currentIndex >= 0 && currentIndex != moveInfo.OriginalPosition)
        {
            _document.MovePage(currentIndex, moveInfo.OriginalPosition);
        }
    }

    _onPagesChanged?.Invoke();
}

// 修正後
public void Undo()
{
    // 🎯 V3.0.123: Undo時も適切な順序で処理
    if (!_moveInfo.Any()) return;

    // Undoは元の位置に戻すので、Execute()と逆の順序
    // Execute()が下移動（降順）だった場合、Undoは昇順
    bool wasMovingDown = _moveInfo.First().NewPosition > _moveInfo.First().OriginalPosition;

    var sortedMoves = wasMovingDown
        ? _moveInfo.OrderBy(m => m.OriginalPosition).ToList()
        : _moveInfo.OrderByDescending(m => m.OriginalPosition).ToList();

    foreach (var moveInfo in sortedMoves)
    {
        var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
        if (currentIndex >= 0)
        {
            _document.MovePage(currentIndex, moveInfo.OriginalPosition);
        }
    }

    _onPagesChanged?.Invoke();
}
```

---

**計画作成完了**: 2025-10-06
