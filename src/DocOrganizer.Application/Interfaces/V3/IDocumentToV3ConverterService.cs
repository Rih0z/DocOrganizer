using System.Collections.Generic;
using System.Threading.Tasks;
using DocOrganizer.Core.Models;
using DocOrganizer.Application.Interfaces.V3;

namespace DocOrganizer.Application.Interfaces.V3;

/// <summary>
/// PdfDocumentをV3のPDF出力データに安全に変換するサービス
/// </summary>
public interface IDocumentToV3ConverterService
{
    /// <summary>
    /// PdfDocumentをV3のPDF出力データに安全に変換
    /// キャスト不要でPdfPageの情報を正しく抽出
    /// </summary>
    /// <param name="document">変換対象のPdfDocument</param>
    /// <returns>V3 PDF出力用データリスト</returns>
    Task<List<PdfExportPageData>> ConvertToV3ExportDataAsync(PdfDocument document);
    
    /// <summary>
    /// V3編集可能なコンテンツかどうか判定
    /// 画像ベースのPDFかどうかを安全に判定
    /// </summary>
    /// <param name="document">判定対象のPdfDocument</param>
    /// <returns>V3で編集可能な場合true</returns>
    Task<bool> HasV3EditableContentAsync(PdfDocument document);
    
    /// <summary>
    /// 現在の編集状態を反映したデータ取得
    /// PdfDocumentの現在の状態（回転・順番）を取得
    /// </summary>
    /// <param name="document">対象のPdfDocument</param>
    /// <returns>現在の編集状態を反映したPDF出力データ</returns>
    Task<List<PdfExportPageData>> GetCurrentEditStateAsync(PdfDocument document);
}