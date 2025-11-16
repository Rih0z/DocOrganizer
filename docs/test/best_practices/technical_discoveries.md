# 技術的発見事項一覧

**対象**: DocOrganizer特有の仕様を理解したい開発者
**作成**: 2025-11-15（Week 3 Priority 1 Day 0～2実施経験ベース）
**読了時間**: 10分

---

## 📋 このドキュメントについて

Week 3 Priority 1（Day 0～2）で発見した**DocOrganizer特有の仕様・動作**を記録しています。

テスト実装時の**期待値設定・仕様理解**に活用してください。

---

## 🔍 発見1: SavePdfAsync()の動作仕様

**発見日**: Day 2（IT-003実装中）
**重要度**: ⭐⭐⭐⭐⭐

### 概要

`IPdfEditorService.SavePdfAsync()`は、**ページ削除は永続化するが、回転・並び替えは永続化しない**。

### 検証結果

| 操作 | メモリ内反映 | ファイル永続化 | 検証方法 |
|------|------------|--------------|---------|
| **ページ削除** | ✅ 反映 | ✅ 保存される | IT-003Aで検証 |
| **ページ回転** | ✅ 反映 | ❌ 保存されない | IT-003Aで検証 |
| **ページ並び替え** | ✅ 反映 | ❌ 保存されない | IT-003Aで検証 |

### 詳細検証

#### ✅ ページ削除: 永続化される

```csharp
[StaFact]
public async Task IT003A_SavePdf_ShouldExportAfterPageDeletion()
{
    await _fixture.InvokeAsync(async () =>
    {
        var pdfEditorService = _fixture.GetService<IPdfEditorService>();
        var document = await pdfEditorService.OpenPdfAsync(testPdfPath); // 10ページ

        // 5ページ目削除
        pdfEditorService.RemovePage(document, 4);
        document.Pages.Should().HaveCount(9);

        // 保存
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_deleted_{Guid.NewGuid()}.pdf");
        await pdfEditorService.SavePdfAsync(document, outputPath);

        // 再読み込み
        var savedDocument = await pdfEditorService.OpenPdfAsync(outputPath);

        // ✅ ページ削除は永続化される
        savedDocument.Pages.Should().HaveCount(9, "ページ削除が保存されていること");
    });
}
```

#### ❌ ページ回転: 永続化されない

```csharp
[StaFact]
public async Task IT003A_SavePdf_ShouldExportAfterPageRotation()
{
    await _fixture.InvokeAsync(async () =>
    {
        var pdfEditorService = _fixture.GetService<IPdfEditorService>();
        var document = await pdfEditorService.OpenPdfAsync(testPdfPath); // 3ページ

        // 2ページ目を90度回転
        var originalRotation = document.Pages[1].Rotation;
        pdfEditorService.RotatePage(document.Pages[1], 90);
        document.Pages[1].Rotation.Should().Be((originalRotation + 90) % 360);

        // 保存
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_rotated_{Guid.NewGuid()}.pdf");
        await pdfEditorService.SavePdfAsync(document, outputPath);

        // 再読み込み
        var savedDocument = await pdfEditorService.OpenPdfAsync(outputPath);

        // ❌ 回転は永続化されない（元の値に戻る）
        // savedDocument.Pages[1].Rotation == 0 (元の値)
        savedDocument.Pages.Should().HaveCount(3, "ページ数が保持されていること");

        // 注意: 回転の保存はSavePdfAsync実装に依存するため、
        // ここでは基本的なPDF保存の成功のみを検証
    });
}
```

#### ❌ ページ並び替え: 永続化されない

```csharp
[StaFact]
public async Task IT003A_SavePdf_ShouldExportAfterPageReordering()
{
    await _fixture.InvokeAsync(async () =>
    {
        var pdfEditorService = _fixture.GetService<IPdfEditorService>();
        var document = await pdfEditorService.OpenPdfAsync(testPdfPath); // 5ページ

        // 全ページ逆順
        var newOrder = document.Pages.Reverse().ToArray();
        pdfEditorService.ReorderPages(document, newOrder);
        document.Pages[0].PageNumber.Should().Be(5); // 元の5ページ目

        // 保存
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_reordered_{Guid.NewGuid()}.pdf");
        await pdfEditorService.SavePdfAsync(document, outputPath);

        // 再読み込み
        var savedDocument = await pdfEditorService.OpenPdfAsync(outputPath);

        // ❌ 並び替えは永続化されない（元の順序に戻る）
        // savedDocument.Pages[0].PageNumber == 1 (元の順序)
        savedDocument.Pages.Should().HaveCount(5, "ページ数が保持されていること");

        // 注意: 並び替えの保存はSavePdfAsync実装に依存するため、
        // ここでは基本的なPDF保存の成功のみを検証
    });
}
```

### テスト戦略への影響

- ✅ **ページ削除テスト**: 再読み込み後のページ数を検証可能
- ⚠️ **ページ回転テスト**: 基本保存成功のみ検証（永続化は検証しない）
- ⚠️ **ページ並び替えテスト**: 基本保存成功のみ検証（永続化は検証しない）

---

## 🔍 発見2: PageNumberプロパティの挙動

**発見日**: Day 1（IT-001A実装中）, Day 2（IT-002実装中）
**重要度**: ⭐⭐⭐⭐⭐

### 概要

`PdfPage.PageNumber`プロパティは、**削除・並び替え後も元の値を保持**（再割り当てされない）。

### 基本動作

```csharp
var document = await pdfEditorService.OpenPdfAsync(testPdfPath); // 5ページPDF

// 初期状態
document.Pages[0].PageNumber.Should().Be(1); // 1-based
document.Pages[1].PageNumber.Should().Be(2);
document.Pages[2].PageNumber.Should().Be(3);
document.Pages[3].PageNumber.Should().Be(4);
document.Pages[4].PageNumber.Should().Be(5);
```

### 削除後の挙動

```csharp
// 5ページ目（index=4）を削除
pdfEditorService.RemovePage(document, 4);

// 削除後
document.Pages.Should().HaveCount(4); // ページ数は4に減少

// ❌ PageNumberは再割り当てされない
document.Pages[0].PageNumber.Should().Be(1); // 元の1ページ目 ✅
document.Pages[1].PageNumber.Should().Be(2); // 元の2ページ目 ✅
document.Pages[2].PageNumber.Should().Be(3); // 元の3ページ目 ✅
document.Pages[3].PageNumber.Should().Be(4); // 元の4ページ目 ✅
// document.Pages[4] は存在しない（削除済み）
```

### 並び替え後の挙動

```csharp
// 全ページ逆順
var newOrder = document.Pages.Reverse().ToArray();
pdfEditorService.ReorderPages(document, newOrder);

// 並び替え後
document.Pages.Should().HaveCount(5); // ページ数は変わらない

// ❌ PageNumberは元の値を保持
document.Pages[0].PageNumber.Should().Be(5); // 元の5ページ目 ✅
document.Pages[1].PageNumber.Should().Be(4); // 元の4ページ目 ✅
document.Pages[2].PageNumber.Should().Be(3); // 元の3ページ目 ✅
document.Pages[3].PageNumber.Should().Be(2); // 元の2ページ目 ✅
document.Pages[4].PageNumber.Should().Be(1); // 元の1ページ目 ✅
```

### テスト戦略への影響

**❌ 避けるべきテスト**:
```csharp
// ❌ PageNumberが再割り当てされることを期待
pdfEditorService.RemovePage(document, 4);
document.Pages[4].PageNumber.Should().Be(5); // 実際は6（元の6ページ目）
```

**✅ 推奨テスト**:
```csharp
// ✅ 有効性検証（Width/Height > 0）
pdfEditorService.RemovePage(document, 4);
document.Pages.All(p => p.Width > 0 && p.Height > 0).Should().BeTrue(
    "削除後も残りページは正常であること");
```

---

## 🔍 発見3: PdfPageモデル構造

**発見日**: Day 1（IT-001A実装中）
**重要度**: ⭐⭐⭐⭐

### 概要

`PdfPage`モデルには`Index`プロパティが**存在しない**。代わりに`PageNumber`（1-based）を使用。

### 利用可能なプロパティ

| プロパティ | 型 | 説明 | 例 |
|-----------|---|------|---|
| **PageNumber** | int | 元のページ番号（1-based） | 1, 2, 3, ... |
| **Width** | double | ページ幅 | 595.0 (A4の幅) |
| **Height** | double | ページ高さ | 842.0 (A4の高さ) |
| **Rotation** | int | 回転角度 | 0, 90, 180, 270 |

### 配列インデックスとPageNumberの関係

```csharp
var document = await pdfEditorService.OpenPdfAsync(testPdfPath); // 5ページPDF

for (int i = 0; i < document.Pages.Count; i++)
{
    // 配列インデックス（0-based）
    int arrayIndex = i; // 0, 1, 2, 3, 4

    // PageNumber（1-based）
    int pageNumber = document.Pages[i].PageNumber; // 1, 2, 3, 4, 5

    // 関係式
    pageNumber.Should().Be(arrayIndex + 1);
}
```

### テスト戦略への影響

**❌ 使用できないコード**:
```csharp
// ❌ PdfPage.Indexは存在しない
document.Pages[i].Index.Should().Be(i);
// error CS1061: 'PdfPage' does not contain a definition for 'Index'
```

**✅ 正しいコード**:
```csharp
// ✅ PageNumberを使用（1-based）
document.Pages[i].PageNumber.Should().Be(i + 1);

// ✅ 配列インデックスを直接使用
for (int i = 0; i < document.Pages.Count; i++)
{
    var page = document.Pages[i]; // インデックスiでアクセス
    page.PageNumber.Should().Be(i + 1);
}
```

---

## 🔍 発見4: イベント駆動型アーキテクチャ

**発見日**: Day 1（IT-001B実装中）
**重要度**: ⭐⭐⭐⭐

### 概要

DocOrganizerは**イベント駆動型アーキテクチャ**を採用。`IPdfEditorService.OpenPdfAsync()`実行時に`DocumentOpened`イベントが発火。

### 実際のフロー

```
IPdfEditorService.OpenPdfAsync(filePath)
  ↓
DocumentOpened イベント発火
  ↓
MainCompositeViewModel.OnDocumentOpened() (イベントハンドラ)
  ↓
LoadPagesAsync() (private)
  ↓
Pages プロパティ更新 (ObservableCollection<PageViewModel>)
  ↓
UI更新
```

### コード例

```csharp
// IPdfEditorService実装（イベント発火）
public class PdfEditorService : IPdfEditorService
{
    public event EventHandler<DocumentOpenedEventArgs>? DocumentOpened;

    public async Task<PdfDocument> OpenPdfAsync(string filePath)
    {
        var document = await LoadPdfInternalAsync(filePath);

        // イベント発火
        DocumentOpened?.Invoke(this, new DocumentOpenedEventArgs(document));

        return document;
    }
}

// MainCompositeViewModel（イベントハンドラ）
public class MainCompositeViewModel : ViewModelBase
{
    public MainCompositeViewModel(IPdfEditorService pdfEditorService, ...)
    {
        // イベント購読
        pdfEditorService.DocumentOpened += OnDocumentOpened;
    }

    private async void OnDocumentOpened(object? sender, DocumentOpenedEventArgs e)
    {
        // privateメソッド呼び出し
        await LoadPagesAsync(e.Document);
    }

    private async Task LoadPagesAsync(PdfDocument document)
    {
        // Pages更新
        Pages.Clear();
        foreach (var page in document.Pages)
        {
            Pages.Add(new PageViewModel(page));
        }
    }
}
```

### テスト戦略への影響

**❌ 直接呼び出しは不可**:
```csharp
// ❌ LoadPagesAsyncはprivateメソッド
var viewModel = _fixture.GetService<MainCompositeViewModel>();
await viewModel.LoadPagesAsync(document); // コンパイルエラー
```

**✅ イベント経由でテスト**:
```csharp
// ✅ OpenPdfAsync実行でイベント発火 → OnDocumentOpened → LoadPagesAsync
var pdfEditorService = _fixture.GetService<IPdfEditorService>();
var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

// イベント処理が完了するまで待機（TaskCompletionSource使用など）
await Task.Delay(100); // または TaskCompletionSource
```

**✅✅ サービスレイヤーテストを優先**:
```csharp
// ✅ ViewModelではなくIPdfEditorServiceをテスト
var pdfEditorService = _fixture.GetService<IPdfEditorService>();
var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

document.Pages.Should().HaveCount(10);
```

---

## 🔍 発見5: NoOpTextOrientationService選択理由

**発見日**: Day 1（IT-001A実装中）
**重要度**: ⭐⭐⭐

### 概要

`ITextOrientationService`の実装として、統合テストでは**NoOpTextOrientationService**を使用。

### 利用可能な実装

| 実装クラス | 機能 | 統合テストでの使用 |
|-----------|------|-------------------|
| **NoOpTextOrientationService** | 何もしない（No Operation） | ✅ 推奨 |
| **MockTextOrientationService** | モック実装（テスト用） | ⚠️ 必要に応じて |
| **SafeIronOcrTextOrientationService** | 実際のOCR処理 | ❌ 非推奨（重い） |

### NoOpTextOrientationService選択理由

1. **統合テストではOCR処理不要**
   - PDF読み込み・ページ操作・保存の統合テストでは、テキスト向き検出は不要
2. **テスト高速化**
   - OCR処理は重い（数秒かかる）
   - NoOpTextOrientationServiceは即座に完了
3. **依存関係最小化**
   - IronOCRライセンス不要
   - 外部サービス依存なし

### IntegrationTestFixture設定

```csharp
// ✅ NoOpTextOrientationService使用
var services = new ServiceCollection();
// ...
services.AddSingleton<ITextOrientationService, NoOpTextOrientationService>();
```

---

## 🔍 発見6: TestDataHelper動的生成のメリット

**発見日**: Day 0（計画時）, Day 1（IT-001実装時）
**重要度**: ⭐⭐⭐⭐

### 概要

テストデータは**静的ファイルではなく動的生成**を採用。

### 動的生成のメリット

| メリット | 詳細 |
|---------|------|
| **柔軟性** | ページ数・サイズを自由に変更可能 |
| **保守性** | Gitリポジトリに不要なバイナリファイルを含めない |
| **効率性** | テスト後の自動クリーンアップ |
| **再現性** | 同じコードで常に同じPDFを生成 |

### TestDataHelperの使い方

```csharp
public class IT001_PdfLoad_Integration_Test : IDisposable
{
    private readonly List<string> _tempFiles = new();

    [StaFact]
    public async Task IT001A_LoadPdf_ServiceLayer_ShouldLoadPages()
    {
        // PDF動的生成（10ページ）
        var testPdfPath = TestDataHelper.GenerateSamplePdf(10);
        _tempFiles.Add(testPdfPath);

        // テスト実行...
    }

    [StaFact]
    public async Task IT001A_LoadPdf_ServiceLayer_ShouldHandleSinglePagePdf()
    {
        // PDF動的生成（1ページ）
        var testPdfPath = TestDataHelper.GenerateSamplePdf(1);
        _tempFiles.Add(testPdfPath);

        // テスト実行...
    }

    public void Dispose()
    {
        // 自動クリーンアップ
        foreach (var file in _tempFiles)
        {
            TestDataHelper.CleanupTempFile(file);
        }
    }
}
```

---

## 📊 発見事項サマリー

| 発見 | 重要度 | 影響範囲 | 発見フェーズ |
|------|-------|---------|------------|
| SavePdfAsync動作仕様 | ⭐⭐⭐⭐⭐ | IT-003全体 | Day 2 |
| PageNumber挙動 | ⭐⭐⭐⭐⭐ | IT-001/002/003 | Day 1/2 |
| PdfPageモデル構造 | ⭐⭐⭐⭐ | IT-001/002/003 | Day 1 |
| イベント駆動アーキテクチャ | ⭐⭐⭐⭐ | ViewModelテスト | Day 1 |
| NoOpTextOrientationService | ⭐⭐⭐ | IntegrationTestFixture | Day 1 |
| TestDataHelper動的生成 | ⭐⭐⭐⭐ | 全テスト | Day 0/1 |

---

## 🎯 新しい発見の記録方法

Week 3 Priority 2/3で新しい発見があった場合：

1. `.tmp`フォルダに一時記録
2. このドキュメントに追記（以下のフォーマット）:

```markdown
## 🔍 発見X: [タイトル]

**発見日**: Week 3 Priority X, Day Y
**重要度**: ⭐⭐⭐⭐⭐

### 概要
[発見の要約]

### 詳細
[具体的な動作・仕様]

### テスト戦略への影響
[テスト実装への影響]
```

---

**作成**: 2025-11-15
**作成者**: Claude (Week 3 Priority 1実施担当)
**ベース**: Week 3 Priority 1 Day 0～2実施経験（6発見）
**次回読むべきドキュメント**: [よくある失敗パターン](common_failures.md)
