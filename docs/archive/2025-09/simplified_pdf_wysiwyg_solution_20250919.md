# 簡素化されたPDF WYSIWYG出力ソリューション

## 基本原則
**プレビューに表示されている通りにPDFを出力する** - それだけ。

## 現在の問題の再定義

### 問題1: ズームボタンが動作しない
- **原因**: CommunityToolkit.Mvvm のソースジェネレータ問題
- **解決**: コマンドを明示的に実装

### 問題2: プレビューとPDF出力の不一致
- **現状**: プレビューには余白があるが、PDF出力には余白がない
- **原因**: PDF出力時に常にA4サイズ（595x842）を使用し、画像を最大化している
- **解決**: プレビューの表示状態をそのままPDFに反映

## シンプルな解決策

### フェーズ1: ズームボタンの修正（緊急）

#### PreviewManagementViewModel.cs の修正
```csharp
public PreviewManagementViewModel()
{
    // 明示的にコマンドを初期化（ソースジェネレータを使わない）
    ZoomInCommand = new RelayCommand(ExecuteZoomIn, CanExecuteZoomIn);
    ZoomOutCommand = new RelayCommand(ExecuteZoomOut, CanExecuteZoomOut);
    ZoomResetCommand = new RelayCommand(ExecuteZoomReset);
}

// [RelayCommand] 属性を削除し、メソッドを直接実装
private void ExecuteZoomIn()
{
    // 既存の ZoomIn() メソッドを呼び出す
    ZoomIn();
}

private bool CanExecuteZoomIn()
{
    // 既存の CanZoomIn() メソッドを呼び出す
    return CanZoomIn();
}
```

### フェーズ2: プレビュー状態の保持

#### 新しいプロパティの追加（PreviewManagementViewModel）
```csharp
// プレビューの現在の表示モード
public bool IsOriginalSize { get; private set; }
public double CurrentZoomLevel { get; private set; } = 1.0;
public Rect CurrentViewportRect { get; private set; }
```

### フェーズ3: PDF出力の簡素化

#### PdfExportService の修正
```csharp
public async Task<bool> ExportToPdfAsync(
    IEnumerable<PageEntity> pages,
    string outputPath,
    PreviewState previewState) // 新しいパラメータ
{
    using var document = new PdfDocument();
    
    foreach (var page in pages)
    {
        // プレビューの状態に基づいてページサイズを決定
        XSize pageSize;
        if (previewState.IsOriginalSize)
        {
            // 元画像のサイズをそのまま使用
            pageSize = new XSize(
                page.OriginalWidth * 72 / 96,  // ピクセルからポイントへ
                page.OriginalHeight * 72 / 96
            );
        }
        else
        {
            // A4サイズを使用（現在の動作）
            pageSize = PageSize.A4;
        }
        
        var pdfPage = document.AddPage();
        pdfPage.Width = pageSize.Width;
        pdfPage.Height = pageSize.Height;
        
        using var gfx = XGraphics.FromPdfPage(pdfPage);
        
        // プレビューと同じように画像を描画
        DrawImageAsInPreview(gfx, page, previewState);
    }
    
    document.Save(outputPath);
    return true;
}

private void DrawImageAsInPreview(
    XGraphics gfx,
    PageEntity page,
    PreviewState previewState)
{
    // プレビューの表示ロジックと完全に同じ計算を使用
    var image = LoadImage(page.ImagePath);
    
    if (previewState.IsOriginalSize)
    {
        // 原寸大：そのまま描画（余白なし）
        gfx.DrawImage(image, 0, 0, page.Width, page.Height);
    }
    else
    {
        // A4フィット：プレビューと同じ余白計算
        var drawRect = CalculateFitRectangle(
            new Size(page.OriginalWidth, page.OriginalHeight),
            gfx.PageSize
        );
        
        // 白背景（プレビューと同じ）
        gfx.DrawRectangle(XBrushes.White, 0, 0, gfx.PageSize.Width, gfx.PageSize.Height);
        
        // 中央配置で画像描画
        gfx.DrawImage(image, drawRect);
    }
}
```

## 実装の優先順位

1. **最優先**: ズームボタンの修正（10分で完了可能）
2. **次**: プレビュー状態の取得（30分）
3. **最後**: PDF出力の調整（1時間）

## なぜこの方法がシンプルか

1. **ダイアログ変更不要**: 既存のPDF出力ダイアログはそのまま
2. **新しいUIコントロール不要**: モード選択などの複雑なUIは追加しない
3. **プレビューの状態を利用**: 現在表示されている状態をそのままPDFにする
4. **WYSIWYG原則**: What You See Is What You Get - 見たままを出力

## テスト手順

1. ズームボタンのテスト
   - アプリ起動
   - 画像読み込み
   - ズームイン/アウトボタンクリック
   - 動作確認

2. PDF出力のテスト
   - 各種画像（正方形、縦長、横長、A4）を読み込み
   - プレビュー確認
   - PDF出力
   - プレビューとPDFが一致することを確認

## まとめ

ユーザーの要求は明確です：
- **プレビューに表示されている通りにPDFを出力したい**

複雑な解決策は不要です。プレビューの現在の状態（ズームレベル、表示モード）を取得し、その通りにPDFを生成するだけです。

これにより：
- 余白があるプレビュー → 余白があるPDF
- 余白がないプレビュー → 余白がないPDF
- ズームされたプレビュー → 同じズームのPDF

シンプルで直感的な動作になります。