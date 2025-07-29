# PDF回転機能の制限事項と解決策

## 現在の制限

### 問題の本質
DocOrganizer V2.2では、PDFページの回転機能に以下の制限があります：

1. **PDFサムネイルの回転表示不可**
   - PDFsharpはPDFページをビットマップ画像としてレンダリングする機能を持たない
   - そのため、回転後のPDFページのサムネイルを生成できない

2. **現在の動作**
   - 画像ファイル（JPG、PNG等）から作成されたページ：回転したサムネイルが正しく表示される
   - 既存のPDFファイルのページ：回転してもサムネイルは元の向きのまま（またはプレースホルダー表示）

## 技術的背景

### PDFsharpの制限
```csharp
// PDFsharpではこのような操作ができない
var bitmap = pdfPage.RenderToBitmap(); // ❌ この機能は存在しない
```

### 現在の回避策
1. 画像ベースのページには `SourceImagePath` を保持し、元画像から回転したサムネイルを生成
2. PDFページには回転角度を示すインジケーターをプレースホルダーに表示

## 根本的な解決策

### オプション1: PDFレンダリングライブラリの導入
以下のライブラリを検討：

1. **PDFium.NET** (推奨)
   - Google ChromeのPDFエンジン
   - 高速で正確なレンダリング
   - ライセンス：BSD

2. **Ghostscript.NET**
   - 業界標準のPDFレンダリング
   - 豊富な機能
   - ライセンス：AGPL（商用利用は要ライセンス）

3. **Syncfusion PDF Viewer**
   - .NET向け商用ライブラリ
   - WPF統合が容易
   - ライセンス：商用

### オプション2: 外部プロセスでのレンダリング
- ImageMagickやGhostscriptを外部プロセスとして呼び出し
- プロセス間通信のオーバーヘッドあり

### オプション3: Web技術の活用
- PDF.jsをWebViewで実行
- クロスプラットフォーム対応可能
- パフォーマンスは劣る

## 推奨実装手順

### 1. PDFium.NETの導入（推奨）

```xml
<!-- パッケージ追加 -->
<PackageReference Include="PDFiumSharp" Version="*" />
```

```csharp
// 実装例
public async Task<SKBitmap> RenderPdfPageAsync(string pdfPath, int pageIndex, int rotation)
{
    using (var document = PDFium.LoadDocument(pdfPath))
    {
        using (var page = document.GetPage(pageIndex))
        {
            // 回転を適用してレンダリング
            var bitmap = page.RenderWithRotation(rotation);
            return ConvertToSKBitmap(bitmap);
        }
    }
}
```

### 2. 段階的移行
1. まずPDFium.NETを導入し、基本的なレンダリング機能を実装
2. 既存のPdfServiceと並行して動作させ、徐々に移行
3. 全機能の動作確認後、PDFsharpの依存を削減

## 現在のワークアラウンド

### 実装済みの対策
1. プレースホルダーに回転角度を表示（↻ 90°など）
2. プレビュー（右側の大きな表示）は正しく回転
3. 保存時のPDFは正しく回転

### ユーザーへの説明
- 「PDFファイルのサムネイルは技術的制約により回転表示されませんが、実際のPDFは正しく回転されます」
- 「画像ファイルから作成したページは正常に回転表示されます」

## まとめ

現在の実装は、PDFsharpの制限内で最善の解決策を提供しています。根本的な解決にはPDFレンダリングライブラリの導入が必要ですが、これは大きなアーキテクチャ変更を伴うため、慎重な計画が必要です。

当面は現在のワークアラウンドで運用し、将来のバージョンアップでPDFium.NETの導入を検討することを推奨します。