using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.Application.Attributes;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace DocOrganizer.Infrastructure.Services.V3.Providers
{
    /// <summary>
    /// 🎯 GIF専用検証プロバイダー - ImageSharp GIF最適化実装
    /// 責務: GIF形式の特化検証処理（アニメーション対応）
    /// 技術: SixLabors.ImageSharp GIF特化機能
    /// 特徴: アニメーション検出、フレーム数解析、色パレット評価、ループ回数取得
    /// 優先度: 90 (高) - GIF形式に対する専用最適化プロバイダー
    /// </summary>
    [ValidationProvider("ImageSharp GIF Animation Provider", Priority = 90)]
    public class GifValidationProvider : IImageValidationProvider
    {
        private readonly ILogger<GifValidationProvider> _logger;

        /// <summary>
        /// サポート対象拡張子
        /// </summary>
        public string[] SupportedExtensions => new[] { ".gif" };

        /// <summary>
        /// プロバイダー優先度（高）
        /// </summary>
        public int Priority => 90;

        /// <summary>
        /// プロバイダー識別名
        /// </summary>
        public string ProviderName => "ImageSharp GIF Animation Provider";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        public GifValidationProvider(ILogger<GifValidationProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// GIF形式専用検証処理
        /// </summary>
        /// <param name="filePath">GIFファイルパス</param>
        /// <returns>検証結果</returns>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[GIF_Provider] GIF検証開始: {FileName}", Path.GetFileName(filePath));

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
                        _logger.LogWarning("[GIF_Provider] 0バイトGIFファイル: {FileName}", Path.GetFileName(filePath));
                        return CreateZeroBytesResult(filePath, fileInfo.Length);
                    }

                    try
                    {
                        // 🎯 ImageSharpによるGIF特化検証
                        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
                        
                        var width = image.Width;
                        var height = image.Height;
                        var frameCount = image.Frames.Count;
                        var isAnimated = frameCount > 1;

                        // 基本サイズ検証
                        if (width <= 0 || height <= 0)
                        {
                            issues.Add("無効なGIF画像サイズです");
                        }

                        // GIF推奨サイズチェック
                        if (width < 16 || height < 16)
                        {
                            issues.Add("GIFサイズが小さすぎます (最小16x16推奨)");
                        }

                        // 大容量GIF警告
                        if (width > 2000 || height > 2000)
                        {
                            issues.Add("高解像度GIF (パフォーマンス注意)");
                        }

                        // アニメーションGIF特化検証
                        var totalDuration = TimeSpan.Zero;
                        var maxFrameDelay = 0;
                        var minFrameDelay = int.MaxValue;

                        if (isAnimated)
                        {
                            foreach (var frame in image.Frames.OfType<ImageFrame<Rgba32>>())
                            {
                                var gifMetadata = frame.Metadata.GetGifMetadata();
                                var frameDelay = gifMetadata.FrameDelay * 10; // 10ms単位

                                totalDuration = totalDuration.Add(TimeSpan.FromMilliseconds(frameDelay));
                                maxFrameDelay = Math.Max(maxFrameDelay, frameDelay);
                                minFrameDelay = Math.Min(minFrameDelay, frameDelay);
                            }

                            // アニメーション品質チェック
                            if (frameCount > 200)
                            {
                                issues.Add($"フレーム数が非常に多いGIF ({frameCount}フレーム、メモリ使用量注意)");
                            }

                            if (totalDuration.TotalSeconds > 60)
                            {
                                issues.Add($"再生時間が長いGIF ({totalDuration.TotalSeconds:F1}秒)");
                            }

                            if (minFrameDelay < 20)
                            {
                                issues.Add("フレーム間隔が短すぎる（高速再生でCPU負荷大）");
                            }

                            if (maxFrameDelay > 5000)
                            {
                                issues.Add("フレーム間隔が長すぎる（スライドショー形式）");
                            }

                            // ファイルサイズ妥当性（アニメーションGIF）
                            var expectedMinSize = (width * height * frameCount) / 100; // 高圧縮想定
                            var expectedMaxSize = (width * height * frameCount) * 4;   // 低圧縮想定

                            if (fileInfo.Length < expectedMinSize)
                            {
                                issues.Add("アニメーションGIFサイズが異常に小さい（破損の可能性）");
                            }
                            else if (fileInfo.Length > expectedMaxSize)
                            {
                                issues.Add("アニメーションGIFサイズが非常に大きい（最適化推奨）");
                            }

                            _logger.LogDebug("[GIF_Provider] アニメーションGIF解析: {FrameCount}フレーム, 総時間: {Duration}秒", 
                                frameCount, totalDuration.TotalSeconds);
                        }
                        else
                        {
                            // 静止GIF検証
                            var expectedSize = width * height * 3; // RGB想定
                            if (fileInfo.Length > expectedSize / 2)
                            {
                                issues.Add("静止画GIF (PNG/JPEG変換推奨)");
                            }
                        }

                        // 色数チェック（GIF特有）
                        PerformColorPaletteValidation(image, issues);

                        var isValid = issues.Count == 0;

                        _logger.LogDebug("[GIF_Provider] GIF検証完了: {IsValid}, サイズ: {Width}x{Height}, アニメ: {IsAnimated} ({FrameCount}フレーム), ファイル: {FileName}", 
                            isValid, width, height, isAnimated, frameCount, Path.GetFileName(filePath));

                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: isValid,
                            IsCorrupted: false,
                            IsZeroBytes: false,
                            FileSize: fileInfo.Length,
                            Format: $"GIF{(isAnimated ? " (Animation)" : " (Static)")}",
                            Width: width,
                            Height: height,
                            Issues: issues,
                            ErrorMessage: null
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[GIF_Provider] GIF読み込みエラー（破損の可能性）: {FileName}", Path.GetFileName(filePath));
                        
                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: false,
                            IsCorrupted: true,
                            IsZeroBytes: false,
                            FileSize: fileInfo.Length,
                            Format: "GIF",
                            Width: 0,
                            Height: 0,
                            Issues: new List<string> { "GIFファイルが破損しています" },
                            ErrorMessage: ex.Message
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GIF_Provider] GIF検証処理エラー: {FilePath}", filePath);
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
            return extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
        }

        #region GIF特化検証

        /// <summary>
        /// 色パレット検証
        /// </summary>
        private void PerformColorPaletteValidation(Image<Rgba32> image, List<string> issues)
        {
            try
            {
                // 簡易色数カウント（全フレーム統合）
                var uniqueColors = new HashSet<Rgba32>();
                var sampleCount = 0;
                const int maxSamplePoints = 10000; // パフォーマンス制限

                foreach (var frame in image.Frames)
                {
                    var stepX = Math.Max(1, frame.Width / 100);
                    var stepY = Math.Max(1, frame.Height / 100);

                    for (int y = 0; y < frame.Height && sampleCount < maxSamplePoints; y += stepY)
                    {
                        for (int x = 0; x < frame.Width && sampleCount < maxSamplePoints; x += stepX)
                        {
                            uniqueColors.Add(frame[x, y]);
                            sampleCount++;
                        }
                    }

                    if (sampleCount >= maxSamplePoints) break;
                }

                var approximateColorCount = uniqueColors.Count * (image.Width * image.Height) / sampleCount;

                // GIF色数制限チェック
                if (approximateColorCount > 256)
                {
                    issues.Add("256色を超える可能性（GIF制限、品質劣化の恐れ）");
                }
                else if (approximateColorCount < 16)
                {
                    issues.Add("色数が非常に少ない（PNG圧縮効率が良い可能性）");
                }

                _logger.LogDebug("[GIF_Provider] 色パレット解析: 約{ColorCount}色", approximateColorCount);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[GIF_Provider] 色パレット解析エラー（スキップ）");
                issues.Add("色パレット解析でエラー（表示には影響なし）");
            }
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
                Format: "GIF",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "GIFファイルが存在しません" },
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
                Format: "GIF",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "GIFファイルサイズが0バイトです" },
                ErrorMessage: "Zero-byte GIF file"
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
                Format: "GIF",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "GIF検証処理でエラーが発生しました" },
                ErrorMessage: errorMessage
            );
        }

        #endregion
    }
}