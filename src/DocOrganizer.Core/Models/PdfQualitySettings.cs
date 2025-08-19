using System;

namespace DocOrganizer.Core.Models;

/// <summary>
/// PDF出力時の画質設定
/// </summary>
public class PdfQualitySettings
{
    public QualityLevel Level { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int CompressionLevel { get; set; }
    public string DisplayName { get; set; }

    public PdfQualitySettings(QualityLevel level, int maxWidth, int maxHeight, int compressionLevel, string displayName)
    {
        Level = level;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        CompressionLevel = compressionLevel;
        DisplayName = displayName;
    }

    /// <summary>
    /// 事前定義された画質設定を取得
    /// </summary>
    public static PdfQualitySettings[] GetPresetSettings()
    {
        return new[]
        {
            new PdfQualitySettings(
                QualityLevel.Low, 
                1024, 768, 
                60, 
                "低画質"
            ),
            new PdfQualitySettings(
                QualityLevel.Medium, 
                1920, 1440, 
                80, 
                "中画質"
            ),
            new PdfQualitySettings(
                QualityLevel.High, 
                0, 0, // 元画像サイズそのまま
                95, 
                "最高画質"
            )
        };
    }

    /// <summary>
    /// デフォルト設定（中画質）を取得
    /// </summary>
    public static PdfQualitySettings GetDefault()
    {
        return GetPresetSettings()[1]; // 中画質
    }
}