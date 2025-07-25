# テスト用PDFファイル作成スクリプト
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class ImageGenerator {
    public static void CreateTestImage(string path, string text, int width, int height) {
        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap)) {
            graphics.Clear(Color.White);
            using (var font = new Font("Arial", 48))
            using (var brush = new SolidBrush(Color.Black)) {
                var size = graphics.MeasureString(text, font);
                var x = (width - size.Width) / 2;
                var y = (height - size.Height) / 2;
                graphics.DrawString(text, font, brush, x, y);
            }
            bitmap.Save(path, ImageFormat.Png);
        }
    }
}
"@

# サンプルディレクトリ作成
$sampleDir = Join-Path $PSScriptRoot "..\sample"
if (-not (Test-Path $sampleDir)) {
    New-Item -ItemType Directory -Path $sampleDir | Out-Null
}

# テスト画像作成
Write-Host "Creating test images..."
[ImageGenerator]::CreateTestImage("$sampleDir\test1.png", "Page 1", 800, 600)
[ImageGenerator]::CreateTestImage("$sampleDir\test2.png", "Page 2", 800, 600)
[ImageGenerator]::CreateTestImage("$sampleDir\test3.jpg", "Page 3", 800, 600)

Write-Host "Test images created in: $sampleDir"
Write-Host "- test1.png"
Write-Host "- test2.png"
Write-Host "- test3.jpg"