# テストデータ生成スクリプト
# Week 3 Priority 1統合テスト用のサンプルPDFと画像を生成

Write-Host "テストデータ生成開始..." -ForegroundColor Green

# PDFSharpを使用してサンプルPDFを生成するC#コード
$generatePdfCode = @'
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System;
using System.IO;

public class PdfGenerator
{
    public static void GenerateSamplePdf(string outputPath, int pageCount)
    {
        var document = new PdfDocument();
        document.Info.Title = $"Sample PDF - {pageCount} Pages";

        for (int i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 20, XFontStyle.Bold);

            gfx.DrawString($"Page {i + 1} of {pageCount}", font, XBrushes.Black,
                new XRect(0, 0, page.Width, page.Height),
                XStringFormats.Center);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        document.Save(outputPath);
        Console.WriteLine($"Generated: {outputPath}");
    }
}
'@

# 画像生成用のC#コード
$generateImageCode = @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class ImageGenerator
{
    public static void GenerateSampleImage(string outputPath, int width, int height, string text)
    {
        using (var bitmap = new Bitmap(width, height))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);

                using (var font = new Font("Arial", 24, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Black))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    graphics.DrawString(text, font, brush,
                        new RectangleF(0, 0, width, height), format);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            bitmap.Save(outputPath, ImageFormat.Png);
            Console.WriteLine($"Generated: {outputPath}");
        }
    }
}
'@

Write-Host "サンプルPDFを生成中..." -ForegroundColor Cyan

# dotnet scriptを使用する代わりに、既存のプロジェクトでテストヘルパーを作成
# まず、既存のTestDataディレクトリを確認
$testDataDir = Join-Path $PSScriptRoot "."
Write-Host "TestData directory: $testDataDir"

# PDFとImageディレクトリを作成
$pdfDir = Join-Path $testDataDir "Pdfs"
$imageDir = Join-Path $testDataDir "Images"

New-Item -ItemType Directory -Force -Path $pdfDir | Out-Null
New-Item -ItemType Directory -Force -Path $imageDir | Out-Null

Write-Host "テストデータディレクトリを作成しました。" -ForegroundColor Green
Write-Host "  PDFs: $pdfDir"
Write-Host "  Images: $imageDir"

# 注意: 実際のPDF・画像生成は、統合テストのテストヘルパークラスで実装します
Write-Host ""
Write-Host "次のステップ:" -ForegroundColor Yellow
Write-Host "  1. IntegrationTestFixtureにテストデータ生成メソッドを実装"
Write-Host "  2. テスト実行時に必要なPDFと画像を動的生成"
Write-Host ""
Write-Host "完了。" -ForegroundColor Green
