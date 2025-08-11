# DocOrganizer 初回読み込み時プレビュー表示バグ 詳細分析報告書

## 📋 概要

**日時**: 2025-08-08 14:50  
**分析者**: AI Assistant + Serena MCP  
**報告者**: ユーザー  
**症状**: 最初の読み込み時の表示が間違っている - 自動回転が効く前の画像の向きがプレビューとして表示されている

## 🔍 問題の詳細分析

### 📊 現在の状況
- **✅ 改善点**: 回転操作後のプレビューは正確に読み込まれるようになった
- **❌ 新たな問題**: 初回読み込み時に自動回転適用前の向きが表示される
- **❌ 根本問題**: 最新の状態（AutoOrient適用後）がプレビューに反映されていない

### 🔄 問題発生の流れ

#### 1. 画像読み込みフェーズ
```
ユーザーがファイルをドラッグ&ドロップ
↓
ImageProcessingService.LoadImageSafelyAsync()が呼び出される
↓  
EXIF Orientation = 6 (90度CW) の画像を検出
↓
AutoOrient()が適用される（画像データが回転）
↓
正しい向きの画像データが生成される ✅
```

#### 2. プレビュー生成フェーズ（★問題発生箇所）
```
PageViewModel.LoadThumbnailFromImage()が呼び出される
↓
ProcessStandardImageAsync()が実行される  
↓
★問題: _page.Rotation = 0 でサムネイル生成要求
   GetImageThumbnailAsync(imagePath, 150, 150, _page.Rotation) // 0度
↓
GetImageThumbnailAsync()内でLoadImageSafelyAsync()が再度呼び出される
↓
★問題: AutoOrient適用済み画像に対して、手動回転0度を適用
   - LoadImageSafelyAsync()で既にAutoOrient済み ✅
   - しかし.Rotate(0)により元の状態に戻る ❌
↓  
結果: 自動回転前の向きでサムネイル表示 ❌
```

### 🧬 技術的根本原因

#### A. 二重処理による競合
```csharp
// ImageProcessingService.LoadImageSafelyAsync() - 1回目のAutoOrient
if (!isHeicFile && !isHeicConvertedFile && orientation != 1)
{
    image.Mutate(x => x.AutoOrient()); // ★正しい向きに回転
}

// ImageProcessingService.GetImageThumbnailAsync() - 手動回転適用
image.Mutate(x => x
    .Rotate(rotationDegrees)  // ★rotationDegrees = 0 で元に戻る
    .Resize(new ResizeOptions...));
```

#### B. 回転情報の不一致
- **LoadImageSafelyAsync**: EXIF Orientationに基づく自動回転（例: 90度）
- **_page.Rotation**: 初期値0度（手動回転なし）
- **結果**: AutoOrient後の正しい画像に0度回転を適用 → 元の向きに戻る

### 🎯 具体的問題箇所

#### 1. ProcessStandardImageAsync() - Line 290
```csharp
// ★問題のコード
var thumbnailData = await _imageProcessingService.GetImageThumbnailAsync(
    imagePath, 150, 150, _page.Rotation); // _page.Rotation = 0
```

#### 2. GetImageThumbnailAsync() - Line 206-210
```csharp
using var image = await LoadImageSafelyAsync(imagePath); // AutoOrient適用済み
image.Mutate(x => x
    .Rotate(rotationDegrees)  // rotationDegrees = 0 → 元に戻る
    .Resize(new ResizeOptions...));
```

## 📋 問題パターンの分類

### Pattern A: EXIF Orientation = 1 (Normal)
```
読み込み: AutoOrient無し → 向き正常 ✅
サムネイル: 0度手動回転 → 向き正常 ✅
結果: 正常表示 ✅
```

### Pattern B: EXIF Orientation = 6 (90度CW) ★問題ケース
```
読み込み: AutoOrient適用 → 90度CCW回転で正常 ✅
サムネイル: 0度手動回転 → 元の90度CW向きに戻る ❌
結果: 間違った向きで表示 ❌ (「自動回転が効く前の画像の向き」)
```

### Pattern C: EXIF Orientation = 3 (180度)
```
読み込み: AutoOrient適用 → 180度回転で正常 ✅
サムネイル: 0度手動回転 → 元の180度向きに戻る ❌
結果: 間違った向きで表示 ❌
```

## 🔧 解決方針

### Option 1: GetImageThumbnailAsync修正（推奨）
**GetImageThumbnailAsync内で手動回転を適用せず、LoadImageSafelyAsyncの結果をそのまま使用**

```csharp
public async Task<byte[]> GetImageThumbnailAsync(string imagePath, int width = 150, int height = 150, int rotationDegrees = 0)
{
    using var image = await LoadImageSafelyAsync(imagePath); // AutoOrient適用済み
    
    // ★修正: 初回読み込み時は手動回転をスキップ
    // 手動回転は明示的にユーザーが回転操作した場合のみ適用
    if (rotationDegrees != 0)
    {
        image.Mutate(x => x.Rotate(rotationDegrees));
    }
    
    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Size = new Size(width, height),
        Mode = ResizeMode.Max
    }));
    
    // サムネイル生成
}
```

### Option 2: ProcessStandardImageAsync修正
**初回読み込み時は回転角度を渡さない**

```csharp
private async Task ProcessStandardImageAsync(string imagePath, CancellationToken cancellationToken)
{
    // ★修正: 初回読み込み時は回転角度を渡さない（AutoOrientのみ適用）
    var thumbnailData = await _imageProcessingService.GetImageThumbnailAsync(
        imagePath, 150, 150, 0); // 常に0度で初回読み込み
}
```

### Option 3: AutoOrient情報の保存・活用
**LoadImageSafelyAsyncで適用したAutoOrient情報を保存し、サムネイル生成時に活用**

## 📊 影響範囲

### 修正対象ファイル
1. **ImageProcessingService.cs**: GetImageThumbnailAsync()修正
2. **PageViewModel.cs**: ProcessStandardImageAsync()修正（オプション）

### 動作検証項目
1. **Normal画像 (Orientation=1)**: 変更なし、正常表示維持 ✅
2. **90度CW画像 (Orientation=6)**: 初回から正しい向きで表示 ✅
3. **180度画像 (Orientation=3)**: 初回から正しい向きで表示 ✅
4. **手動回転操作**: 既存の動作維持 ✅
5. **HEIC画像**: 既存の動作維持 ✅

## 🚨 重要度

**Priority: HIGH** - ユーザー体験に直接影響
- 初回表示での混乱を回避
- 自動回転機能の本来の動作復旧
- UI一貫性の確保

---

**分析完了**: 🎯 **根本原因特定完了**  
**次のアクション**: Option 1推奨修正の実装  
**予想修正時間**: 15分  
**検証必要**: EXIF Orientation別テスト