# DocOrganizer V3.0 - 回転・画像入れ替え機能仕様書

## 概要
DocOrganizer V3.0では、PDF編集の基本機能として画像の回転と入れ替え機能を実装しています。
これらの機能は、V3アーキテクチャに基づいた堅牢な設計により、サムネイルとプレビューの完全な同期を実現しています。

## バージョン情報
- **バージョン**: 3.0.0
- **リリース日**: 2025-01-19
- **主要機能**: 90度回転、画像入れ替え、ドラッグ&ドロップ

## アーキテクチャ

### 1. 回転機能の実装

#### 1.1 インターフェース定義
```csharp
// DocOrganizer.Application/Interfaces/V3/IThumbnailGeneratorService.cs
public interface IThumbnailGeneratorService
{
    Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath, int rotation = 0);
    Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int rotation = 0, int maxWidth = 1920, int maxHeight = 1080);
}
```

#### 1.2 回転処理の実装
回転機能は2つの異なる技術を使用して実装されています：

**サムネイル用（ImageSharp）**
```csharp
// 左側パネルのサムネイル生成
using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(filePath);

// 回転処理
if (rotation > 0)
{
    image.Mutate(x => x.Rotate(rotation));
}

// リサイズ処理
image.Mutate(x => x.Resize(new ResizeOptions
{
    Size = new Size(targetWidth, targetHeight),
    Mode = ResizeMode.Max
}));
```

**プレビュー用（WPF TransformedBitmap）**
```csharp
// 右側プレビューの回転
if (rotation > 0 && imageSource is BitmapSource bitmapSource)
{
    var transform = new RotateTransform(rotation);
    var rotatedBitmap = new TransformedBitmap(bitmapSource, transform);
    rotatedBitmap.Freeze(); // UIスレッドでの使用のためFreeze
    return rotatedBitmap;
}
```

#### 1.3 回転角度の管理
```csharp
// ViewModels/V3PageViewModel.cs
private int _rotation;
public int Rotation
{
    get => _rotation;
    set
    {
        if (SetProperty(ref _rotation, value))
        {
            _ = LoadLeftThumbnailAsync();  // サムネイル再生成
            _ = LoadRightPreviewAsync();   // プレビュー再生成
        }
    }
}
```

### 2. 画像入れ替え機能

#### 2.1 ドラッグ&ドロップによる入れ替え
```csharp
// ViewModels/V3/PageOperationViewModel.cs
public async Task ReplacePageAsync(V3PageViewModel targetPage, string newImagePath)
{
    // 新しい画像パスを設定
    targetPage.Page.SourceImagePath = newImagePath;
    
    // サムネイルとプレビューを再生成
    await targetPage.LoadLeftThumbnailAsync();
    await targetPage.LoadRightPreviewAsync();
    
    // UIに変更を通知
    OnPropertyChanged(nameof(Pages));
}
```

#### 2.2 ファイル選択ダイアログによる入れ替え
```csharp
private async Task ExecuteReplaceImageCommand(V3PageViewModel page)
{
    var dialog = new OpenFileDialog
    {
        Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.heic)|*.jpg;*.jpeg;*.png;*.bmp;*.heic|PDF files (*.pdf)|*.pdf",
        Title = "画像を選択してください"
    };
    
    if (dialog.ShowDialog() == true)
    {
        await ReplacePageAsync(page, dialog.FileName);
    }
}
```

### 3. UI層の実装

#### 3.1 回転ボタン
```xml
<!-- Views/MainWindow.xaml -->
<!-- 左回転ボタン -->
<Button Command="{Binding PageOperation.RotateLeftCommand}"
        CommandParameter="{Binding ElementName=PageListBox, Path=SelectedItem}"
        ToolTip="左に90度回転">
    <TextBlock Text="↺" FontSize="20"/>
</Button>

<!-- 右回転ボタン -->
<Button Command="{Binding PageOperation.RotateRightCommand}"
        CommandParameter="{Binding ElementName=PageListBox, Path=SelectedItem}"
        ToolTip="右に90度回転">
    <TextBlock Text="↻" FontSize="20"/>
</Button>
```

#### 3.2 コンテキストメニュー
```xml
<ListBox.ContextMenu>
    <ContextMenu>
        <MenuItem Header="画像を入れ替え" 
                  Command="{Binding DataContext.PageOperation.ReplaceImageCommand, 
                            RelativeSource={RelativeSource AncestorType=Window}}"
                  CommandParameter="{Binding}"/>
        <MenuItem Header="左に90度回転" 
                  Command="{Binding DataContext.PageOperation.RotateLeftCommand, 
                            RelativeSource={RelativeSource AncestorType=Window}}"
                  CommandParameter="{Binding}"/>
        <MenuItem Header="右に90度回転" 
                  Command="{Binding DataContext.PageOperation.RotateRightCommand, 
                            RelativeSource={RelativeSource AncestorType=Window}}"
                  CommandParameter="{Binding}"/>
    </ContextMenu>
</ListBox.ContextMenu>
```

## 技術的詳細

### メモリ管理
- **BitmapSource.Freeze()**: UIスレッドでの安全な使用のため、全てのBitmapSourceをFreeze
- **非同期処理**: 画像処理は全て非同期で実行し、UIのブロッキングを防止
- **メモリリーク防止**: 画像リソースの適切な破棄

### パフォーマンス最適化
- **増分更新**: 変更があったページのみ再生成
- **キャッシュ**: 生成済みのサムネイルはメモリにキャッシュ
- **並列処理**: 複数ページの処理は並列実行

### エラーハンドリング
```csharp
try
{
    var thumbnailImageSource = await _thumbnailService.GenerateLeftPanelThumbnailAsync(
        _page.SourceImagePath, 
        Rotation
    );
    ThumbnailImage = thumbnailImageSource;
}
catch (Exception ex)
{
    await AppendDebugLogAsync($"[V3PageViewModel] サムネイル生成エラー: {ex.Message}");
    // デフォルト画像を表示
    ThumbnailImage = GetDefaultErrorImage();
}
```

## 使用方法

### 1. 画像の回転
1. 回転したいページを選択
2. ツールバーの回転ボタン（↺ または ↻）をクリック
3. または右クリックメニューから「左に90度回転」「右に90度回転」を選択

### 2. 画像の入れ替え
1. 入れ替えたいページを右クリック
2. 「画像を入れ替え」を選択
3. ファイル選択ダイアログから新しい画像を選択
4. または新しい画像ファイルを直接ページ上にドラッグ&ドロップ

## 対応ファイル形式
- **画像**: JPG, JPEG, PNG, BMP, HEIC
- **PDF**: 単一ページPDF（複数ページPDFは最初のページのみ）

## 制限事項
- 回転は90度単位のみ（任意角度の回転は未対応）
- HEICファイルの回転はImageMagickを使用（要インストール）
- 大きなファイルの処理には時間がかかる場合がある

## トラブルシューティング

### 回転が反映されない場合
1. アプリケーションを再起動
2. DEBUG_LOG.txtでエラーを確認
3. 画像ファイルの破損をチェック

### 画像入れ替えができない場合
1. ファイルの読み取り権限を確認
2. 対応ファイル形式であることを確認
3. ディスク容量を確認

## 今後の機能拡張予定
- [ ] 任意角度の回転
- [ ] 回転のアンドゥ/リドゥ
- [ ] バッチ回転（複数ページ一括）
- [ ] 自動回転検出（EXIF情報基準）
- [ ] 画像の切り抜き機能

## 関連ドキュメント
- [V3アーキテクチャ概要](./V3_ARCHITECTURE_IMAGE_DISPLAY.md)
- [README](../README.md)
- [CLAUDE.md](../CLAUDE.md)

## 更新履歴
- **V3.0.0** (2025-01-19): 初回リリース
  - 90度回転機能実装
  - 画像入れ替え機能実装
  - サムネイル・プレビュー同期