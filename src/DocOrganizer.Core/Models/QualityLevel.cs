namespace DocOrganizer.Core.Models;

/// <summary>
/// PDF出力画質レベル
/// </summary>
public enum QualityLevel
{
    /// <summary>
    /// 低画質 - プレビュー・メール送付用
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中画質 - 一般用途・印刷用
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 最高画質 - 高品質印刷・アーカイブ用
    /// </summary>
    High = 3
}