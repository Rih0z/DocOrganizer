# GUI統合テスト詳細実装方法

## 概要

FlaUIを使用したWPFアプリケーションのGUI統合テストの詳細実装方法を定義します。

## 1. FlaUIセットアップ

### 1.1 依存パッケージ

```xml
<ItemGroup>
  <PackageReference Include="FlaUI.Core" Version="4.0.0" />
  <PackageReference Include="FlaUI.UIA3" Version="4.0.0" />
</ItemGroup>
```

### 1.2 テストベースクラス

```csharp
public abstract class GuiTestBase : IDisposable
{
    protected Application App;
    protected Window MainWindow;
    protected readonly ITestOutputHelper Output;

    protected GuiTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    protected void LaunchApp()
    {
        var exePath = GetDocOrganizerExePath();
        App = Application.Launch(exePath);

        // MainWindow が表示されるまで待機（最大5秒）
        var automation = new UIA3Automation();
        MainWindow = App.GetMainWindow(automation, TimeSpan.FromSeconds(5));

        MainWindow.Should().NotBeNull("アプリケーションが5秒以内に起動する必要があります");
    }

    protected string GetDocOrganizerExePath()
    {
        var solutionRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\.."));
        return Path.Combine(solutionRoot, @"release\DocOrganizer.exe");
    }

    protected void TakeScreenshot(string fileName)
    {
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            $"DocOrganizerTests_{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        Capture.Screen().ToFile(screenshotPath);
        Output.WriteLine($"Screenshot saved: {screenshotPath}");
    }

    public virtual void Dispose()
    {
        App?.Close();
        App?.Dispose();
    }
}
```

## 2. GUI要素の取得

### 2.1 AutomationId による取得

```csharp
protected ListBox GetPageListBox()
{
    return MainWindow.FindFirstDescendant(cf =>
        cf.ByAutomationId("PageListBox"))?.AsListBox();
}

protected Button GetRotateButton()
{
    return MainWindow.FindFirstDescendant(cf =>
        cf.ByAutomationId("RotateButton"))?.AsButton();
}
```

### 2.2 名前による取得

```csharp
protected Menu GetFileMenu()
{
    return MainWindow.FindFirstDescendant(cf =>
        cf.ByName("ファイル"))?.AsMenu();
}
```

## 3. ドラッグ&ドロップ操作

### 3.1 シンプルなドラッグ&ドロップ

```csharp
[Fact]
[Trait("Category", "GUI-Integration")]
[Trait("TraceabilityID", "IT-004")]
[Trait("Phase", "Phase3")]
public void DragDropPage_FromIndex0ToIndex5_ShouldReorderPages()
{
    // Arrange
    LaunchApp();
    LoadTestPdf("sample_10pages.pdf");

    var pageListBox = GetPageListBox();
    var sourcePage = pageListBox.Items[0];
    var targetPage = pageListBox.Items[5];

    // Act: ドラッグ&ドロップ
    var sourceRect = sourcePage.BoundingRectangle;
    var targetRect = targetPage.BoundingRectangle;

    Mouse.MoveTo(sourceRect.Center);
    Mouse.Down(MouseButton.Left);

    // ゆっくり移動（アニメーション考慮）
    Mouse.MoveTo(targetRect.Center);
    Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));

    Mouse.Up(MouseButton.Left);
    Wait.UntilInputIsProcessed();

    // Assert
    TakeScreenshot("after_drag_drop");

    // ページ順序が変更されたことを検証
    // （実際のページ順序は MainWindow の ViewModel から取得）
}
```

### 3.2 複数ページのドラッグ&ドロップ

```csharp
[Fact]
[Trait("Category", "GUI-Integration")]
[Trait("TraceabilityID", "IT-005")]
[Trait("Phase", "Phase3")]
public void DragDropMultiplePages_ShouldMoveAllSelectedPages()
{
    // Arrange
    LaunchApp();
    LoadTestPdf("sample_10pages.pdf");

    var pageListBox = GetPageListBox();

    // Ctrl+クリックで複数選択
    Keyboard.Press(VirtualKeyShort.CONTROL);

    pageListBox.Items[0].Click();
    pageListBox.Items[2].Click();
    pageListBox.Items[4].Click();

    Keyboard.Release(VirtualKeyShort.CONTROL);

    // Act: 選択した3ページを7ページ目の位置にドラッグ
    var sourceRect = pageListBox.Items[0].BoundingRectangle;
    var targetRect = pageListBox.Items[7].BoundingRectangle;

    Mouse.MoveTo(sourceRect.Center);
    Mouse.Down(MouseButton.Left);
    Mouse.MoveTo(targetRect.Center);
    Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    Mouse.Up(MouseButton.Left);

    // Assert
    TakeScreenshot("after_multi_drag_drop");
}
```

## 4. 回転操作テスト

```csharp
[Fact]
[Trait("Category", "GUI-Integration")]
[Trait("TraceabilityID", "IT-006")]
[Trait("Phase", "Phase3")]
public void RotatePage_UsingShortcut_ShouldRotatePage()
{
    // Arrange
    LaunchApp();
    LoadTestPdf("sample_10pages.pdf");

    var pageListBox = GetPageListBox();
    pageListBox.Items[4].Click(); // 5ページ目を選択

    // Act: Ctrl+R で回転
    Keyboard.Press(VirtualKeyShort.CONTROL);
    Keyboard.Type(VirtualKeyShort.KEY_R);
    Keyboard.Release(VirtualKeyShort.CONTROL);

    Wait.UntilInputIsProcessed();

    // Assert
    TakeScreenshot("after_rotation");

    // 回転後のプレビュー画像が変更されたことを確認
    // （実際には画像比較が必要）
}
```

## 5. アプリケーション起動テスト

```csharp
[Fact]
[Trait("Category", "GUI-Integration")]
[Trait("TraceabilityID", "IT-001")]
[Trait("Phase", "Phase3")]
public void AppStartup_ShouldCompleteWithin5Seconds()
{
    // Act
    var stopwatch = Stopwatch.StartNew();
    LaunchApp();
    stopwatch.Stop();

    // Assert
    stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
        "IT-001: アプリケーションが5秒以内に起動する必要があります");

    MainWindow.Title.Should().Contain("DocOrganizer",
        "IT-002: タイトルバーに「DocOrganizer」が表示される必要があります");
}

[Fact]
[Trait("Category", "GUI-Integration")]
[Trait("TraceabilityID", "IT-003")]
[Trait("Phase", "Phase3")]
public void AppStartup_InitialState_ShouldBeEmpty()
{
    // Arrange
    LaunchApp();

    // Act
    var pageListBox = GetPageListBox();
    var rotateButton = GetRotateButton();

    // Assert
    pageListBox.Items.Should().BeEmpty(
        "IT-003: 初期状態ではページリストが空である必要があります");

    rotateButton.IsEnabled.Should().BeFalse(
        "IT-003: 初期状態では回転ボタンが無効である必要があります");
}
```

## 6. ヘルパーメソッド

```csharp
protected void LoadTestPdf(string fileName)
{
    var testPdfPath = GetTestPdfPath(fileName);

    // ファイルメニュー → 開く
    var fileMenu = GetFileMenu();
    fileMenu.Click();

    var openMenuItem = fileMenu.FindFirstDescendant(cf =>
        cf.ByName("開く"))?.AsMenuItem();
    openMenuItem.Click();

    Wait.UntilInputIsProcessed();

    // ファイル選択ダイアログにファイルパスを入力
    // （注: これは環境依存の実装になるため、代替方法も検討）
    Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_L);
    Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(100));

    Keyboard.Type(testPdfPath);
    Keyboard.Type(VirtualKeyShort.RETURN);

    Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(2));
}
```

## 7. スクリーンショット比較（オプション）

```csharp
protected bool CompareScreenshots(string expected, string actual, double threshold = 0.95)
{
    using var expectedImage = new Bitmap(expected);
    using var actualImage = new Bitmap(actual);

    // 画像比較ライブラリ（ImageSharpなど）を使用
    var similarity = ImageComparer.Compare(expectedImage, actualImage);

    return similarity >= threshold;
}
```

## 8. CI/CD環境での実行

### GitHub Actions設定（Windows環境）

```yaml
gui-integration-tests:
  runs-on: windows-latest
  if: github.event_name == 'push' && github.ref == 'refs/heads/main'

  steps:
  - name: Run GUI Integration Tests
    run: |
      dotnet test tests/DocOrganizer.UI.Tests/ `
        --configuration Release `
        --filter "Category=GUI-Integration" `
        --logger "trx;LogFileName=gui_test_results.trx"
    timeout-minutes: 10
```

**注意**: GUI統合テストはCI/CD環境では不安定になりやすいため、Phase 3（オプション）としています。

## まとめ

FlaUIを使用することで、WPFアプリケーションのGUI統合テストを自動化できます。ただし、環境依存やFlakyテストのリスクがあるため、重要な操作のみをテスト対象とすることを推奨します。
