using DocOrganizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces.V3;

/// <summary>
/// PDF出力データ転送オブジェクト
/// </summary>
public class PdfExportPageData
{
    public string ImagePath { get; set; } = "";
    public int Rotation { get; set; }
    public int PageIndex { get; set; }
}

/// <summary>
/// PDF出力サービス - V3ページ状態を反映したPDF生成
/// </summary>
public interface IPdfExportService
{
    /// <summary>
    /// 現在のページ状態（回転・入れ替え含む）をPDFに出力
    /// </summary>
    /// <param name="pageData">出力対象のページデータ一覧</param>
    /// <param name="qualitySettings">画質設定</param>
    /// <param name="outputPath">出力先パス</param>
    /// <returns>出力成功可否</returns>
    Task<bool> ExportCurrentStateAsync(
        IEnumerable<PdfExportPageData> pageData, 
        PdfQualitySettings qualitySettings,
        string outputPath
    );

    /// <summary>
    /// WYSIWYG対応 - プレビュー状態を反映したPDF出力
    /// </summary>
    /// <param name="pageData">出力対象のページデータ一覧</param>
    /// <param name="qualitySettings">画質設定</param>
    /// <param name="outputPath">出力先パス</param>
    /// <param name="previewState">プレビューの表示状態</param>
    /// <returns>出力成功可否</returns>
    Task<bool> ExportCurrentStateAsync(
        IEnumerable<PdfExportPageData> pageData, 
        PdfQualitySettings qualitySettings,
        string outputPath,
        DocOrganizer.Application.Models.V3.PreviewState previewState
    );

    /// <summary>
    /// ページ状態を考慮した画像処理
    /// </summary>
    /// <param name="pageData">対象ページデータ</param>
    /// <param name="qualitySettings">画質設定</param>
    /// <returns>処理済み画像バイト配列</returns>
    Task<byte[]> ProcessPageImageAsync(PdfExportPageData pageData, PdfQualitySettings qualitySettings);

    /// <summary>
    /// PDF出力の進行状況を通知するイベント
    /// </summary>
    event EventHandler<PdfExportProgressEventArgs> ProgressChanged;
}

/// <summary>
/// PDF出力進行状況イベント引数
/// </summary>
public class PdfExportProgressEventArgs : EventArgs
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string CurrentOperation { get; set; } = "";
    public double ProgressPercentage => TotalPages > 0 ? (double)CurrentPage / TotalPages * 100 : 0;
}