# DocOrganizer V3.0.009 サムネイル拡大・サイズ変更機能バグ分析

**作成日時**: 2025-08-21  
**バージョン**: V3.0.009  
**重要度**: 中  
**分類**: UI機能不具合（表示サイズ制御）

## 🔍 **問題の詳細**

### ❌ **報告されたバグ**
ユーザー報告（日本語）:
> "kakudaibotanya gazouno ookisano hirituwo kaerarerukinounituite, gamennjoubuni sono botanya risutode senntakudekiruyouna kinougaaru. sikasi, sorerawoositemo samuneiruhyouzino ookisaga kawattarisinai."

**翻訳**: 拡大ボタンや画像の大きさの比率を変えられる機能について、画面上にそのボタンやリストで選択できるような機能がある。しかし、それらを押してもサムネイル表示の大きさが変わったりしない。

### 🎯 **問題箇所特定**

#### **UI要素の存在確認** ✅
```xml
<!-- MainWindow.xaml:227-238 - 拡大・縮小ボタン -->
<Button Command="{Binding ZoomInCommand}" ToolTip="拡大" Padding="4">
    <TextBlock Text="🔍+" FontSize="14"/>
</Button>
<Button Command="{Binding ZoomOutCommand}" ToolTip="縮小" Padding="4">
    <TextBlock Text="🔍-" FontSize="14"/>
</Button>

<!-- MainWindow.xaml:239-246 - ズームレベル選択ComboBox -->
<ComboBox Width="80" SelectedItem="{Binding PreviewManagement.ZoomLevel}" Margin="4,0">
    <ComboBoxItem>50%</ComboBoxItem>
    <ComboBoxItem>75%</ComboBoxItem>
    <ComboBoxItem IsSelected="True">100%</ComboBoxItem>
    <ComboBoxItem>125%</ComboBoxItem>
    <ComboBoxItem>150%</ComboBoxItem>
    <ComboBoxItem>200%</ComboBoxItem>
</ComboBox>
```

#### **コマンド実装の確認** ✅
```csharp
// PreviewManagementViewModel.cs:109-115 - ZoomIn実装
[RelayCommand]
private void ZoomIn()
{
    var currentZoom = GetCurrentZoomPercentage();
    var newZoom = Math.Min(currentZoom * 1.25, 500); // 最大500%
    ApplyZoom(newZoom);
}

// PreviewManagementViewModel.cs:120-126 - ZoomOut実装
[RelayCommand]
private void ZoomOut()
{
    var currentZoom = GetCurrentZoomPercentage();
    var newZoom = Math.Max(currentZoom * 0.8, 25); // 最小25%
    ApplyZoom(newZoom);
}
```

#### **ApplyZoom実装の確認** ✅
```csharp
// PreviewManagementViewModel.cs:473-483
private void ApplyZoom(double zoomPercentage)
{
    ZoomLevel = $"{zoomPercentage:F0}%";

    if (_selectedPage?.PreviewImage is System.Windows.Media.Imaging.BitmapImage bitmap)
    {
        var scale = zoomPercentage / 100.0;
        PreviewWidth = bitmap.PixelWidth * scale;
        PreviewHeight = bitmap.PixelHeight * scale;
    }
}
```

## 🚨 **根本原因発見**

### ❌ **致命的な問題**: ApplyZoomが`PreviewImage`のみを対象としている

**問題のコード分析**:
```csharp
// ❌ 問題: PreviewImageのみでズーム適用
if (_selectedPage?.PreviewImage is System.Windows.Media.Imaging.BitmapImage bitmap)
{
    var scale = zoomPercentage / 100.0;
    PreviewWidth = bitmap.PixelWidth * scale;    // プレビュー画像のサイズ変更
    PreviewHeight = bitmap.PixelHeight * scale;  // プレビュー画像のサイズ変更
}
```

### 📊 **UI構造の相違**

| UI要素 | 対象画像 | ズーム処理 | 結果 |
|--------|----------|-----------|------|
| **左側サムネイルリスト** | `ThumbnailImage` | ❌ **処理対象外** | サイズ変更されない |
| **右側プレビューエリア** | `PreviewImage` | ✅ **処理対象** | サイズ変更される |

### 🔍 **サムネイルサイズが固定される仕組み**

```xml
<!-- MainWindow.xaml:391-396 - サムネイル表示部分 -->
<Border Grid.Row="1" Background="White" Margin="4">
    <Image Source="{Binding ThumbnailImage}" 
           Stretch="Uniform"
           RenderOptions.BitmapScalingMode="HighQuality"/>
</Border>
```

**固定サイズの原因**:
```xml
<!-- MainWindow.xaml:380 - 固定高さ120px -->
<RowDefinition Height="120"/>
```

## 🎯 **修正方針**

### 修正案1: サムネイルサイズを動的制御
```csharp
// PreviewManagementViewModel.cs - ApplyZoom修正版
private void ApplyZoom(double zoomPercentage)
{
    ZoomLevel = $"{zoomPercentage:F0}%";
    
    // ✅ プレビューエリアのズーム（既存）
    if (_selectedPage?.PreviewImage is System.Windows.Media.Imaging.BitmapImage bitmap)
    {
        var scale = zoomPercentage / 100.0;
        PreviewWidth = bitmap.PixelWidth * scale;
        PreviewHeight = bitmap.PixelHeight * scale;
    }
    
    // ✅ サムネイルサイズの動的制御（新規追加）
    ThumbnailSize = 120 * (zoomPercentage / 100.0); // 基準120pxからスケール
    OnPropertyChanged(nameof(ThumbnailSize)); // UI更新通知
}
```

### 修正案2: XAML側でのバインディング対応
```xml
<!-- MainWindow.xaml - サムネイル部分修正版 -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="{Binding PreviewManagement.ThumbnailSize}"/> <!-- ✅ 動的高さ -->
</Grid.RowDefinitions>
```

### 修正案3: ThumbnailSizeプロパティ追加
```csharp
// PreviewManagementViewModel.cs - 新規プロパティ
[ObservableProperty]
private double thumbnailSize = 120.0; // デフォルト120px
```

## 🚀 **実装手順**

### Phase 1: ThumbnailSizeプロパティ追加
1. PreviewManagementViewModelにThumbnailSizeプロパティ追加
2. 初期値120.0を設定
3. ObservablePropertyで自動通知設定

### Phase 2: ApplyZoom修正
1. ApplyZoom内でThumbnailSizeも更新
2. ズーム倍率に応じてサムネイルサイズ計算
3. プロパティ変更通知確認

### Phase 3: XAML バインディング修正
1. MainWindow.xamlのRowDefinition修正
2. Height="120"を Height="{Binding PreviewManagement.ThumbnailSize}"に変更
3. バインディングパス確認

### Phase 4: テスト確認
1. 拡大ボタンでサムネイルサイズ増加確認
2. 縮小ボタンでサムネイルサイズ減少確認
3. ComboBox選択でサイズ変更確認

## 🎯 **期待結果**

1. **拡大ボタン**: サムネイルサイズが1.25倍に拡大
2. **縮小ボタン**: サムネイルサイズが0.8倍に縮小
3. **ComboBox選択**: 選択した倍率でサムネイルサイズ変更
4. **連動動作**: プレビューエリアとサムネイルが同期してサイズ変更

## 📊 **影響範囲**

### 変更ファイル
- `src/DocOrganizer.UI/ViewModels/V3/PreviewManagementViewModel.cs`（ThumbnailSizeプロパティ追加・ApplyZoom修正）
- `src/DocOrganizer.UI/Views/MainWindow.xaml`（RowDefinitionバインディング修正）

### リスク評価
- **低リスク**: 新規プロパティ追加と既存メソッド修正のみ
- **下位互換性**: 既存機能への影響なし
- **テスト範囲**: UI表示のみ、データ処理への影響なし

---

**結論**: サムネイルサイズ制御機能が**PreviewImage対象の処理のみ**で、**ThumbnailImage が処理対象外**となっているため、ユーザーが期待するサムネイルサイズ変更が動作しない。ThumbnailSizeプロパティ追加とApplyZoom修正により解決可能。