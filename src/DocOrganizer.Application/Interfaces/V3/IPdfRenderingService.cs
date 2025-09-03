using System;
using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3.0.025 PDF専用レンダリングサービス - PdfiumSharp統合
    /// PDF→画像変換に特化した外部サービス（HEICパターン踏襲）
    /// </summary>
    public interface IPdfRenderingService
    {
        /// <summary>
        /// PDFページを一時画像ファイルに変換（HEIC変換サービスパターン準拠）
        /// </summary>
        /// <param name="pdfPath">PDFファイルパス</param>
        /// <param name="pageIndex">ページインデックス（0ベース）</param>
        /// <param name="dpi">レンダリング解像度（デフォルト150dpi）</param>
        /// <returns>一時画像ファイルパス</returns>
        Task<string> ConvertPdfPageToTempImageAsync(string pdfPath, int pageIndex, int dpi = 150);
        
        /// <summary>
        /// PDF基本情報を取得
        /// </summary>
        /// <param name="pdfPath">PDFファイルパス</param>
        /// <returns>PDF情報</returns>
        Task<PdfInfo> GetPdfInfoAsync(string pdfPath);
        
        /// <summary>
        /// 一時ファイルのクリーンアップ
        /// </summary>
        void CleanupTempFiles();
    }
    
    /// <summary>
    /// PDF基本情報
    /// </summary>
    public record PdfInfo(
        int PageCount,
        double Width,
        double Height,
        long FileSize,
        string Version
    );
}