# DocOrganizer V3.0.009 プレビューエリア拡大修正報告書

**日時**: 2025-08-21 00:20  
**問題**: 右側プレビューエリアの拡大が動作しない  
**ユーザー要求**: 虫眼鏡ボタンで**右側のプレビュー**を拡大したい  

## 🔍 **根本原因分析**

### 問題1: XAML MaxWidth/MaxHeight制限
**現在のXAML**: `MainWindow.xaml:431-432`
```xml
<Image Source="{Binding PreviewManagement.CurrentPageImage}" 
       Stretch="Uniform"
       MaxWidth="{Binding PreviewManagement.PreviewWidth}"
       MaxHeight="{Binding PreviewManagement.PreviewHeight}"
       RenderOptions.BitmapScalingMode="HighQuality"/>
```

**問題**: 
- `MaxWidth`/`MaxHeight`は**上限制限**のみ
- 実際のサイズ制御には`Width`/`Height`が必要
- `Stretch="Uniform"`により比率維持されるが、明示的サイズ指定なし

### 問題2: 間違った修正方向
**現在の実装**: サムネイルサイズを拡大
**ユーザー期待**: プレビューエリアを拡大

### 問題3: ApplyZoom実装問題
**現在のApplyZoom**: `PreviewManagementViewModel.cs:499-503`
```csharp
if (_selectedPage?.PreviewImage is System.Windows.Media.Imaging.BitmapImage bitmap)
{
    var scale = zoomPercentage / 100.0;
    PreviewWidth = bitmap.PixelWidth * scale;
    PreviewHeight = bitmap.PixelHeight * scale;
}
```

**問題**: 
- `_selectedPage?.PreviewImage`が`null`の場合は動作しない
- `CurrentPageImage`を使用すべき

## 🛠️ **修正方針**

### 修正1: XAML修正
```xml
<!-- 修正前 -->
<Image MaxWidth="{Binding PreviewManagement.PreviewWidth}"
       MaxHeight="{Binding PreviewManagement.PreviewHeight}"/>

<!-- 修正後 -->
<Image Width="{Binding PreviewManagement.PreviewWidth}"
       Height="{Binding PreviewManagement.PreviewHeight}"/>
```

### 修正2: ApplyZoom修正
```csharp
// 修正前
if (_selectedPage?.PreviewImage is BitmapImage bitmap)

// 修正後  
if (CurrentPageImage is BitmapImage bitmap)
```

### 修正3: ThumbnailSize削除
サムネイルサイズは変更せず、プレビューエリアのみ拡大

## 📋 **実装手順**

1. **XAML修正**: MaxWidth/MaxHeight → Width/Height
2. **ApplyZoom修正**: CurrentPageImage使用
3. **ThumbnailSize行削除**: サムネイル拡大を無効化
4. **ビルド・テスト**: 右側プレビュー拡大確認

## 🎯 **期待結果**

- 🔍+/🔍-ボタンで右側プレビューエリアが拡大・縮小
- ComboBox選択で即座にプレビューサイズ変更
- 左側サムネイルサイズは固定維持
- 25%～200%の全ズームレベルでプレビュー制御