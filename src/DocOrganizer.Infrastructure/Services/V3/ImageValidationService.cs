using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3実装: OSS標準画像検証サービス
    /// 技術: ImageSharp + WPF統合による包括的検証
    /// 目標: 0バイトファイル等の問題の確実な検出・修復
    /// </summary>
    public class ImageValidationService : IImageValidationService
    {
        private readonly ILogger<ImageValidationService> _logger;
        private readonly HashSet<string> _supportedExtensions;

        public ImageValidationService(ILogger<ImageValidationService> logger)
        {
            _logger = logger;
            _supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".gif", ".webp", ".heic", ".heif"
            };
        }

        /// <summary>
        /// 画像ファイルの基本検証
        /// </summary>
        public async Task<ImageValidationResult> ValidateImageAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_Validation] 画像検証開始: {FileName}", Path.GetFileName(filePath));

                return await Task.Run(() =>
                {
                    var issues = new List<string>();
                    var fileInfo = new FileInfo(filePath);

                    // 基本存在チェック
                    if (!fileInfo.Exists)
                    {
                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: false,
                            IsCorrupted: false,
                            IsZeroBytes: false,
                            FileSize: 0,
                            Format: "",
                            Width: 0,
                            Height: 0,
                            Issues: new List<string> { "ファイルが存在しません" },
                            ErrorMessage: "File not found"
                        );
                    }

                    // 0バイトチェック
                    var isZeroBytes = fileInfo.Length == 0;
                    if (isZeroBytes)
                    {
                        issues.Add("ファイルサイズが0バイトです");
                    }

                    // 拡張子チェック
                    if (!IsSupportedImageFormat(filePath))
                    {
                        issues.Add("サポートされていない画像形式です");
                    }

                    var isValid = true;
                    var isCorrupted = false;
                    var format = "";
                    var width = 0;
                    var height = 0;
                    string? errorMessage = null;

                    if (!isZeroBytes)
                    {
                        try
                        {
                            // 🎯 ImageSharpによる詳細検証
                            using var image = SixLabors.ImageSharp.Image.Load(filePath);
                            
                            width = image.Width;
                            height = image.Height;
                            format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown";

                            // サイズ検証
                            if (width <= 0 || height <= 0)
                            {
                                issues.Add("無効な画像サイズです");
                                isValid = false;
                            }

                            // 最小サイズチェック
                            if (width < 10 || height < 10)
                            {
                                issues.Add("画像サイズが小さすぎます (最小10x10)");
                            }

                            // 最大サイズチェック（メモリ制限）
                            if (width > 10000 || height > 10000)
                            {
                                issues.Add("画像サイズが大きすぎます (最大10000x10000)");
                            }

                            _logger.LogDebug("[V3_Validation] 画像検証成功: {Width}x{Height}, {Format}, ファイル: {FileName}", 
                                width, height, format, Path.GetFileName(filePath));
                        }
                        catch (Exception ex)
                        {
                            isValid = false;
                            isCorrupted = true;
                            issues.Add("画像データが破損しています");
                            errorMessage = ex.Message;

                            _logger.LogWarning(ex, "[V3_Validation] 画像読み込みエラー（破損の可能性）: {FileName}", Path.GetFileName(filePath));
                        }
                    }
                    else
                    {
                        isValid = false;
                    }

                    return new ImageValidationResult(
                        FilePath: filePath,
                        IsValid: isValid && issues.Count == 0,
                        IsCorrupted: isCorrupted,
                        IsZeroBytes: isZeroBytes,
                        FileSize: fileInfo.Length,
                        Format: format,
                        Width: width,
                        Height: height,
                        Issues: issues,
                        ErrorMessage: errorMessage
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Validation] 画像検証エラー: {FilePath}", filePath);
                return new ImageValidationResult(
                    FilePath: filePath,
                    IsValid: false,
                    IsCorrupted: false,
                    IsZeroBytes: false,
                    FileSize: 0,
                    Format: "",
                    Width: 0,
                    Height: 0,
                    Issues: new List<string> { "検証処理でエラーが発生しました" },
                    ErrorMessage: ex.Message
                );
            }
        }

        /// <summary>
        /// 画像ファイルの修復試行
        /// </summary>
        public async Task<ImageRepairResult> RepairImageAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_Validation] 画像修復開始: {FileName}", Path.GetFileName(filePath));

                var validation = await ValidateImageAsync(filePath);
                var repairActions = new List<string>();
                string? repairedPath = null;
                var repairSuccessful = false;

                if (validation.IsZeroBytes)
                {
                    repairActions.Add("0バイトファイルのため修復不可");
                    return new ImageRepairResult(filePath, null, false, repairActions, "0バイトファイルは修復できません");
                }

                if (validation.IsCorrupted)
                {
                    try
                    {
                        // 🎯 修復試行: 部分的データ回復
                        repairedPath = Path.GetTempFileName() + Path.GetExtension(filePath);
                        
                        // ImageSharpによる回復試行
                        using var originalImage = SixLabors.ImageSharp.Image.Load(filePath);
                        originalImage.Save(repairedPath);
                        
                        // 修復結果検証
                        var repairedValidation = await ValidateImageAsync(repairedPath);
                        if (repairedValidation.IsValid)
                        {
                            repairActions.Add("画像データの再エンコードにより修復成功");
                            repairSuccessful = true;
                        }
                        else
                        {
                            File.Delete(repairedPath);
                            repairedPath = null;
                            repairActions.Add("再エンコード修復が失敗しました");
                        }
                    }
                    catch (Exception ex)
                    {
                        repairActions.Add($"修復処理でエラー: {ex.Message}");
                        if (repairedPath != null && File.Exists(repairedPath))
                        {
                            File.Delete(repairedPath);
                            repairedPath = null;
                        }
                    }
                }
                else if (validation.IsValid)
                {
                    repairActions.Add("画像は正常で修復不要");
                    repairSuccessful = true;
                }

                _logger.LogDebug("[V3_Validation] 画像修復完了: {FileName}, 成功: {Success}", 
                    Path.GetFileName(filePath), repairSuccessful);

                return new ImageRepairResult(filePath, repairedPath, repairSuccessful, repairActions, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Validation] 画像修復エラー: {FilePath}", filePath);
                return new ImageRepairResult(filePath, null, false, new List<string> { "修復処理でエラーが発生しました" }, ex.Message);
            }
        }

        /// <summary>
        /// 一括画像検証
        /// </summary>
        public async Task<ImageValidationResult[]> ValidateBatchAsync(string[] filePaths)
        {
            try
            {
                _logger.LogDebug("[V3_Validation] 一括画像検証開始: {FileCount}件", filePaths.Length);

                var tasks = filePaths.Select(ValidateImageAsync);
                var results = await Task.WhenAll(tasks);

                var validCount = results.Count(r => r.IsValid);
                _logger.LogDebug("[V3_Validation] 一括画像検証完了: {ValidCount}/{TotalCount}件が有効", validCount, filePaths.Length);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Validation] 一括画像検証エラー");
                throw;
            }
        }

        /// <summary>
        /// 対応形式判定
        /// </summary>
        public bool IsSupportedImageFormat(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return _supportedExtensions.Contains(extension);
        }

        /// <summary>
        /// 画像品質評価
        /// </summary>
        public async Task<ImageQualityAssessment> AssessImageQualityAsync(string filePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var validation = ValidateImageAsync(filePath).Result;
                    if (!validation.IsValid)
                    {
                        return new ImageQualityAssessment(
                            FilePath: filePath,
                            QualityLevel: ImageQualityLevel.Poor,
                            Resolution: 0,
                            CompressionRatio: 0,
                            HasArtifacts: true,
                            QualityIssues: validation.Issues,
                            Metrics: new Dictionary<string, object>()
                        );
                    }

                    var qualityIssues = new List<string>();
                    var metrics = new Dictionary<string, object>();
                    
                    // 解像度評価
                    var resolution = validation.Width * validation.Height;
                    metrics["Resolution"] = resolution;
                    metrics["Width"] = validation.Width;
                    metrics["Height"] = validation.Height;
                    
                    // ファイルサイズ評価
                    var compressionRatio = (double)validation.FileSize / resolution;
                    metrics["CompressionRatio"] = compressionRatio;
                    metrics["FileSize"] = validation.FileSize;

                    // 品質レベル判定
                    var qualityLevel = resolution switch
                    {
                        < 100000 => ImageQualityLevel.Poor,      // 100K未満
                        < 1000000 => ImageQualityLevel.Fair,     // 1M未満
                        < 8000000 => ImageQualityLevel.Good,     // 8M未満
                        _ => ImageQualityLevel.Excellent         // 8M以上
                    };

                    // 圧縮率チェック
                    if (compressionRatio < 0.1)
                    {
                        qualityIssues.Add("過度な圧縮により品質劣化の可能性");
                    }

                    return new ImageQualityAssessment(
                        FilePath: filePath,
                        QualityLevel: qualityLevel,
                        Resolution: resolution,
                        CompressionRatio: compressionRatio,
                        HasArtifacts: qualityIssues.Count > 0,
                        QualityIssues: qualityIssues,
                        Metrics: metrics
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Validation] 画像品質評価エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 無効ファイル除去
        /// </summary>
        public async Task<string[]> FilterValidImagesAsync(string[] filePaths)
        {
            try
            {
                _logger.LogDebug("[V3_Validation] 無効ファイル除去開始: {FileCount}件", filePaths.Length);

                var validationResults = await ValidateBatchAsync(filePaths);
                var validFiles = validationResults
                    .Where(r => r.IsValid && !r.IsZeroBytes && !r.IsCorrupted)
                    .Select(r => r.FilePath)
                    .ToArray();

                _logger.LogDebug("[V3_Validation] 無効ファイル除去完了: {ValidCount}/{TotalCount}件が有効", 
                    validFiles.Length, filePaths.Length);

                return validFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Validation] 無効ファイル除去エラー");
                throw;
            }
        }
    }
}