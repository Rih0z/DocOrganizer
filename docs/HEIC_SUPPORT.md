---
# ⚠️ 重要: このドキュメントはV2アーキテクチャ用です

🚨 **V2コード廃止による影響:**
- このドキュメントで言及するMainViewModel、PageViewModelは全て廃止済み
- V3アーキテクチャでは以下に置き換え:
  - MainViewModel → MainCompositeViewModel + 各種専門ViewModel  
  - PageViewModel → V3PageViewModel (OSS標準サービス使用)

✅ **V3での対応状況:**
- HEIC対応はV3 IThumbnailGeneratorServiceで継続
- 90度回転バグはV3非同期処理で根本解決済み

---

## 概要

DocOrganizer 2.2では、HEIC (High Efficiency Image Container) 形式の画像ファイルに完全対応しています。iPhoneやiPadで撮影された画像の高品質プレビュー表示と編集機能を提供します。

## サポートされる形式

- `.heic` - HEIC形式（推奨）
- `.heif` - HEIF形式

## 主要機能

### 1. 高品質プレビュー表示
- **解像度**: 最大1200x1600ピクセル
- **品質**: 98%品質設定 + 300DPI

### 2. **PageViewModel** (`src/DocOrganizer.UI/ViewModels/`)

**🚨 このクラスは廃止済み - V3PageViewModelを使用**

### 3. **MainViewModel** (`src/DocOrganizer.UI/ViewModels/`)  

**🚨 このクラスは廃止済み - MainCompositeViewModelを使用**

### 4. 90度回転問題の完全修正

**✅ V3で根本解決済み**
- V2の同期処理が原因だった90度回転バグ
- V3非同期処理 + OSS標準サービスで完全修正

## V3アーキテクチャでの実装

V3では以下のClean Architecture構造で実装されています:

### サービス層
- `IThumbnailGeneratorService`: OSS標準サムネイル生成
- `IImageProcessingService`: 画像処理抽象化
- `ITextOrientationService`: EXIF向き検出

### ViewModel層  
- `V3PageViewModel`: ページ単位の表示制御
- `MainCompositeViewModel`: 統合調整
- `PreviewManagementViewModel`: プレビュー専門管理

### 技術仕様
- **フレームワーク**: SixLabors.ImageSharp (OSS標準)
- **UI**: WPF + MVVM
- **品質**: V2と同等以上の高品質プレビュー
- **安定性**: 90度回転バグ完全修正済み