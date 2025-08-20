using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocOrganizer.Application.Attributes;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace DocOrganizer.Infrastructure.Services.V3.Providers
{
    /// <summary>
    /// 🎯 標準画像検証プロバイダー - ImageSharp最適化実装
    /// 責務: JPEG/PNG/BMP/TIFF形式の高速検証処理
    /// 技術: SixLabors.ImageSharp による標準画像処理
    /// 特徴: 高速処理、詳細メタデータ解析、圧縮品質評価
    /// 優先度: 80 (標準) - 一般的な画像形式に対する標準プロバイダー
    /// </summary>
    [ValidationProvider("ImageSharp Standard Provider", Priority = 80)]
    public class StandardImageValidationProvider : IImageValidationProvider
    {
        private readonly ILogger<StandardImageValidationProvider> _logger;

        /// <summary>
        /// サポート対象拡張子
        /// </summary>
        public string[] SupportedExtensions => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };

        /// <summary>
        /// プロバイダー優先度（標準）
        /// </summary>
        public int Priority => 80;

        /// <summary>
        /// プロバイダー識別名
        /// </summary>
        public string ProviderName => "ImageSharp Standard Provider";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        public StandardImageValidationProvider(ILogger<StandardImageValidationProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 標準画像形式専用検証処理
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>検証結果</returns>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[Standard_Provider] 標準画像検証開始: {FileName}", Path.GetFileName(filePath));

                return await Task.Run(() =>
                {
                    var issues = new List<string>();
                    var fileInfo = new FileInfo(filePath);

                    // 基本存在チェック
                    if (!fileInfo.Exists)
                    {
                        return CreateNotFoundResult(filePath);
                    }

                    // 0バイトチェック
                    var isZeroBytes = fileInfo.Length == 0;
                    if (isZeroBytes)
                    {
                        _logger.LogWarning("[Standard_Provider] 0バイト画像ファイル: {FileName}", Path.GetFileName(filePath));
                        return CreateZeroBytesResult(filePath, fileInfo.Length);
                    }

                    try
                    {
                        // 🎯 ImageSharpによる高速標準画像検証
                        using var image = SixLabors.ImageSharp.Image.Load(filePath);
                        
                        var width = image.Width;
                        var height = image.Height;
                        var format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown";

                        // 画像サイズ検証
                        if (width <= 0 || height <= 0)
                        {
                            issues.Add("無効な画像サイズです");
                        }

                        // 最小サイズチェック（品質保証）
                        if (width < 10 || height < 10)
                        {
                            issues.Add("画像サイズが小さすぎます (最小10x10推奨)");
                        }

                        // 最大サイズチェック（メモリ制限対応）
                        if (width > 15000 || height > 15000)
                        {
                            issues.Add("画像サイズが非常に大きいです (処理時間注意)");
                        }

                        // ファイルサイズ妥当性チェック
                        var pixelCount = width * height;
                        var expectedMinSize = GetExpectedMinSize(format, pixelCount);
                        var expectedMaxSize = GetExpectedMaxSize(format, pixelCount);

                        if (fileInfo.Length < expectedMinSize)
                        {
                            issues.Add($"{format}ファイルサイズが異常に小さい（破損の可能性）");
                        }
                        else if (fileInfo.Length > expectedMaxSize)
                        {
                            issues.Add($"{format}ファイルサイズが異常に大きい（非圧縮の可能性）");
                        }

                        // 形式別特化チェック
                        PerformFormatSpecificValidation(format, image, issues);

                        var isValid = issues.Count == 0;

                        _logger.LogDebug("[Standard_Provider] 標準画像検証完了: {IsValid}, サイズ: {Width}x{Height}, 形式: {Format}, ファイル: {FileName}", 
                            isValid, width, height, format, Path.GetFileName(filePath));

                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: isValid,
                            IsCorrupted: false,
                            IsZeroBytes: false,
                            FileSize: fileInfo.Length,
                            Format: format,
                            Width: width,
                            Height: height,
                            Issues: issues,
                            ErrorMessage: null
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Standard_Provider] 標準画像読み込みエラー（破損の可能性）: {FileName}", Path.GetFileName(filePath));
                        
                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: false,
                            IsCorrupted: true,
                            IsZeroBytes: false,
                            FileSize: fileInfo.Length,
                            Format: Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
                            Width: 0,
                            Height: 0,
                            Issues: new List<string> { "画像ファイルが破損しています" },
                            ErrorMessage: ex.Message
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Standard_Provider] 標準画像検証処理エラー: {FilePath}", filePath);
                return CreateErrorResult(filePath, ex.Message);
            }
        }

        /// <summary>
        /// 形式サポート判定
        /// </summary>
        /// <param name="extension">ファイル拡張子</param>
        /// <returns>サポート可否</returns>
        public bool SupportsFormat(string extension)
        {
            var normalizedExt = extension.ToLowerInvariant();
            return normalizedExt == ".jpg" || normalizedExt == ".jpeg" || 
                   normalizedExt == ".png" || normalizedExt == ".bmp" || 
                   normalizedExt == ".tiff" || normalizedExt == ".tif";
        }

        #region 形式別特化検証

        /// <summary>
        /// 形式別特化検証
        /// </summary>
        private void PerformFormatSpecificValidation(string format, Image image, List<string> issues)
        {
            switch (format?.ToUpperInvariant())
            {
                case "JPEG":
                    ValidateJpeg(image, issues);
                    break;
                case "PNG":
                    ValidatePng(image, issues);
                    break;
                case "BMP":
                    ValidateBmp(image, issues);
                    break;
                case "TIFF":
                    ValidateTiff(image, issues);
                    break;
            }
        }

        /// <summary>
        /// JPEG特化検証
        /// </summary>
        private void ValidateJpeg(Image image, List<string> issues)
        {
            // JPEG品質チェック（簡易）
            var pixelCount = image.Width * image.Height;
            var approxFileSize = pixelCount * 3; // 非圧縮RGB想定
            
            // 異常に高圧縮の場合
            if (approxFileSize > 0 && (double)image.Width * image.Height / approxFileSize > 100)
            {
                issues.Add("JPEG圧縮率が高すぎる可能性（品質劣化の恐れ）");
            }
        }

        /// <summary>
        /// PNG特化検証
        /// </summary>
        private void ValidatePng(Image image, List<string> issues)
        {
            // PNG透明度チェック等は将来実装
            // 現在は基本検証のみ
        }

        /// <summary>
        /// BMP特化検証
        /// </summary>
        private void ValidateBmp(Image image, List<string> issues)
        {
            // BMPは通常非圧縮のため大きなファイルサイズになる
            var pixelCount = image.Width * image.Height;
            var expectedSize = pixelCount * 3; // RGB想定
            
            if (expectedSize > 50 * 1024 * 1024) // 50MB以上
            {
                issues.Add("BMPファイルサイズが非常に大きいです（PNG変換推奨）");
            }
        }

        /// <summary>
        /// TIFF特化検証
        /// </summary>
        private void ValidateTiff(Image image, List<string> issues)
        {
            // TIFF詳細検証は将来実装
            // 現在は基本検証のみ
        }

        #endregion

        #region サイズ推定

        /// <summary>
        /// 期待最小ファイルサイズ取得
        /// </summary>
        private long GetExpectedMinSize(string format, int pixelCount)
        {
            return format?.ToUpperInvariant() switch
            {
                "JPEG" => pixelCount / 20,    // JPEG高圧縮想定
                "PNG" => pixelCount / 10,     // PNG圧縮想定
                "BMP" => pixelCount * 3,      // BMP非圧縮
                "TIFF" => pixelCount / 5,     // TIFF圧縮想定
                _ => pixelCount / 50          // 一般的最小値
            };
        }

        /// <summary>
        /// 期待最大ファイルサイズ取得
        /// </summary>
        private long GetExpectedMaxSize(string format, int pixelCount)
        {
            return format?.ToUpperInvariant() switch
            {
                "JPEG" => pixelCount * 2,     // JPEG低圧縮想定
                "PNG" => pixelCount * 4,      // PNG非圧縮想定
                "BMP" => pixelCount * 4,      // BMP + アルファ
                "TIFF" => pixelCount * 6,     // TIFF非圧縮想定
                _ => pixelCount * 10          // 一般的最大値
            };
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// ファイル未存在結果作成
        /// </summary>
        private static ImageValidationResult CreateNotFoundResult(string filePath)
        {
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: false,
                FileSize: 0,
                Format: "Unknown",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "画像ファイルが存在しません" },
                ErrorMessage: "File not found"
            );
        }

        /// <summary>
        /// 0バイト結果作成
        /// </summary>
        private static ImageValidationResult CreateZeroBytesResult(string filePath, long fileSize)
        {
            var format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: true,
                FileSize: fileSize,
                Format: format,
                Width: 0,
                Height: 0,
                Issues: new List<string> { $"{format}ファイルサイズが0バイトです" },
                ErrorMessage: "Zero-byte image file"
            );
        }

        /// <summary>
        /// エラー結果作成
        /// </summary>
        private static ImageValidationResult CreateErrorResult(string filePath, string errorMessage)
        {
            var format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: false,
                FileSize: 0,
                Format: format,
                Width: 0,
                Height: 0,
                Issues: new List<string> { "画像検証処理でエラーが発生しました" },
                ErrorMessage: errorMessage
            );
        }

        #endregion
    }
}