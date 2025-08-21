using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Infrastructure.Services.V3;

/// <summary>
/// 型安全でアーキテクチャ準拠のPdfDocument→V3変換サービス
/// キャスト不要、Clean Architecture原則遵守
/// </summary>
public class DocumentToV3ConverterService : IDocumentToV3ConverterService
{
    private readonly ILogger<DocumentToV3ConverterService> _logger;

    public DocumentToV3ConverterService(ILogger<DocumentToV3ConverterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// PdfDocumentをV3のPDF出力データに安全に変換
    /// </summary>
    public async Task<List<PdfExportPageData>> ConvertToV3ExportDataAsync(PdfDocument document)
    {
        try
        {
            if (document?.Pages == null)
            {
                _logger.LogWarning("[DocumentToV3Converter] 空のPdfDocumentが渡されました");
                return new List<PdfExportPageData>();
            }

            var result = new List<PdfExportPageData>();
            
            _logger.LogDebug("[DocumentToV3Converter] 変換開始: {PageCount}ページ", document.Pages.Count);
            
            foreach (var page in document.Pages)
            {
                // ✅ 安全: PdfPageの情報を正しく抽出（キャスト不要）
                var pageData = new PdfExportPageData
                {
                    ImagePath = page.SourceImagePath ?? string.Empty,  // PdfPage自体のプロパティ
                    Rotation = page.Rotation,                          // PdfPage自体の回転情報
                    PageIndex = result.Count                           // ページインデックス
                };
                
                result.Add(pageData);
                
                _logger.LogDebug("[DocumentToV3Converter] ページ変換: Path='{ImagePath}', Rotation={Rotation}°", 
                    pageData.ImagePath, pageData.Rotation);
            }
            
            await AppendDebugLogAsync($"[DocumentToV3Converter] {result.Count}ページを安全に変換完了");
            _logger.LogDebug("[DocumentToV3Converter] 変換完了: {PageCount}ページ", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            await AppendDebugLogAsync($"[DocumentToV3Converter] ❌ 変換エラー: {ex.Message}");
            _logger.LogError(ex, "[DocumentToV3Converter] 変換エラー");
            throw;
        }
    }

    /// <summary>
    /// V3編集可能なコンテンツかどうか判定
    /// </summary>
    public async Task<bool> HasV3EditableContentAsync(PdfDocument document)
    {
        try
        {
            if (document?.Pages == null || !document.Pages.Any())
            {
                _logger.LogDebug("[DocumentToV3Converter] 空文書のためV3編集不可");
                return false;
            }

            // 画像ベースのPDFかどうか判定
            var hasImageContent = document.Pages.Any(page => 
                !string.IsNullOrEmpty(page.SourceImagePath) && 
                File.Exists(page.SourceImagePath));
            
            _logger.LogDebug("[DocumentToV3Converter] V3編集可能性判定: {IsEditable} ({PageCount}ページ, 画像ベース: {HasImages})", 
                hasImageContent, document.Pages.Count, hasImageContent);
            
            await AppendDebugLogAsync($"[DocumentToV3Converter] V3編集可能性: {hasImageContent} (画像ベースページ存在)");
            
            return hasImageContent;
        }
        catch (Exception ex)
        {
            await AppendDebugLogAsync($"[DocumentToV3Converter] ❌ 判定エラー: {ex.Message}");
            _logger.LogError(ex, "[DocumentToV3Converter] V3編集可能性判定エラー");
            return false;
        }
    }

    /// <summary>
    /// 現在の編集状態を反映したデータ取得
    /// </summary>
    public async Task<List<PdfExportPageData>> GetCurrentEditStateAsync(PdfDocument document)
    {
        try
        {
            _logger.LogDebug("[DocumentToV3Converter] 現在の編集状態取得開始");
            await AppendDebugLogAsync("[DocumentToV3Converter] 現在の編集状態（回転・順番）を取得開始");
            
            // 現在の編集状態（回転・順番）を反映した変換を実行
            var result = await ConvertToV3ExportDataAsync(document);
            
            // 詳細な状態ログ出力
            for (int i = 0; i < result.Count; i++)
            {
                var page = result[i];
                await AppendDebugLogAsync($"[DocumentToV3Converter] 編集状態 Page{i+1}: '{page.ImagePath}' (回転: {page.Rotation}°)");
            }
            
            _logger.LogDebug("[DocumentToV3Converter] 現在の編集状態取得完了: {PageCount}ページ", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            await AppendDebugLogAsync($"[DocumentToV3Converter] ❌ 編集状態取得エラー: {ex.Message}");
            _logger.LogError(ex, "[DocumentToV3Converter] 編集状態取得エラー");
            throw;
        }
    }

    /// <summary>
    /// 🚨 緊急デバッグ: ファイルに詳細ログを出力（第16条準拠）
    /// </summary>
    private async Task AppendDebugLogAsync(string message)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            var logPath = @"C:\Users\217216X721451\github\DocOrganizer\release\DEBUG_LOG.txt";
            await System.IO.File.AppendAllTextAsync(logPath, logMessage + Environment.NewLine);
            System.Diagnostics.Debug.WriteLine($"[DOC_TO_V3_CONVERTER] {message}");
        }
        catch
        {
            // ログ出力エラーは無視
        }
    }
}