using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocOrganizer.Application.Attributes;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3.Providers
{
    /// <summary>
    /// 🎯 HEIC専用検証プロバイダー - ImageMagick最適化実装
    /// 責務: HEIC/HEIF形式の専用検証処理
    /// 技術: ImageMagick (Magick.NET) による高品質HEIC処理
    /// 特徴: EXIF Orientation自動処理、Apple固有メタデータ対応
    /// 優先度: 100 (最高) - HEIC形式に対して最優先選択
    /// </summary>
    [ValidationProvider("ImageMagick HEIC Provider", Priority = 100)]
    public class HeicValidationProvider : IImageValidationProvider
    {
        private readonly IHeicConversionService _heicConversionService;
        private readonly ILogger<HeicValidationProvider> _logger;

        /// <summary>
        /// サポート対象拡張子
        /// </summary>
        public string[] SupportedExtensions => new[] { ".heic", ".heif" };

        /// <summary>
        /// プロバイダー優先度（最高）
        /// </summary>
        public int Priority => 100;

        /// <summary>
        /// プロバイダー識別名
        /// </summary>
        public string ProviderName => "ImageMagick HEIC Provider";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="heicConversionService">HEIC変換サービス</param>
        /// <param name="logger">ロガー</param>
        public HeicValidationProvider(
            IHeicConversionService heicConversionService,
            ILogger<HeicValidationProvider> logger)
        {
            _heicConversionService = heicConversionService;
            _logger = logger;
        }

        /// <summary>
        /// HEIC形式専用検証処理
        /// </summary>
        /// <param name="filePath">HEICファイルパス</param>
        /// <returns>検証結果</returns>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[HEIC_Provider] HEIC検証開始: {FileName}", Path.GetFileName(filePath));

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
                        _logger.LogWarning("[HEIC_Provider] 0バイトHEICファイル: {FileName}", Path.GetFileName(filePath));
                        return CreateZeroBytesResult(filePath, fileInfo.Length);
                    }

                    try
                    {
                        // 🎯 ImageMagickによるHEIC専用検証
                        var heicInfo = _heicConversionService.GetHeicInfoAsync(filePath).Result;
                        
                        // HEIC固有検証
                        if (heicInfo.Width <= 0 || heicInfo.Height <= 0)
                        {
                            issues.Add("無効なHEIC画像サイズです");
                        }

                        // Apple HEIC品質チェック
                        if (heicInfo.Width < 100 || heicInfo.Height < 100)
                        {
                            issues.Add("HEICサイズが小さすぎます (最小100x100推奨)");
                        }

                        // 大容量HEIC警告
                        if (heicInfo.Width > 8000 || heicInfo.Height > 8000)
                        {
                            issues.Add("高解像度HEIC (処理時間注意)");
                        }

                        // ファイルサイズ妥当性チェック
                        var expectedMinSize = (heicInfo.Width * heicInfo.Height) / 50; // HEIC高圧縮想定
                        if (fileInfo.Length < expectedMinSize)
                        {
                            issues.Add("HEICファイルサイズが異常に小さい（破損の可能性）");
                        }

                        var isValid = issues.Count == 0;

                        _logger.LogDebug("[HEIC_Provider] HEIC検証完了: {IsValid}, サイズ: {Width}x{Height}, ファイル: {FileName}", 
                            isValid, heicInfo.Width, heicInfo.Height, Path.GetFileName(filePath));

                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: isValid,
                            IsCorrupted: false,
                            IsZeroBytes: false,
                            FileSize: heicInfo.FileSize,
                            Format: "HEIC",
                            Width: heicInfo.Width,
                            Height: heicInfo.Height,
                            Issues: issues,
                            ErrorMessage: null
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[HEIC_Provider] HEIC読み込みエラー（破損の可能性）: {FileName}", Path.GetFileName(filePath));
                        
                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: false,
                            IsCorrupted: true,
                            IsZeroBytes: false,
                            FileSize: fileInfo.Length,
                            Format: "HEIC",
                            Width: 0,
                            Height: 0,
                            Issues: new List<string> { "HEICファイルが破損しています" },
                            ErrorMessage: ex.Message
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HEIC_Provider] HEIC検証処理エラー: {FilePath}", filePath);
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
            return extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".heif", StringComparison.OrdinalIgnoreCase);
        }

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
                Format: "HEIC",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "HEICファイルが存在しません" },
                ErrorMessage: "File not found"
            );
        }

        /// <summary>
        /// 0バイト結果作成
        /// </summary>
        private static ImageValidationResult CreateZeroBytesResult(string filePath, long fileSize)
        {
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: true,
                FileSize: fileSize,
                Format: "HEIC",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "HEICファイルサイズが0バイトです" },
                ErrorMessage: "Zero-byte HEIC file"
            );
        }

        /// <summary>
        /// エラー結果作成
        /// </summary>
        private static ImageValidationResult CreateErrorResult(string filePath, string errorMessage)
        {
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: false,
                FileSize: 0,
                Format: "HEIC",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "HEIC検証処理でエラーが発生しました" },
                ErrorMessage: errorMessage
            );
        }

        #endregion
    }
}