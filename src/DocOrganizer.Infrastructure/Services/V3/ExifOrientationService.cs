using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3実装: OSS標準EXIF Orientation処理サービス
    /// 技術: WPF標準BitmapMetadata API活用
    /// 目標: Windows Photo/Paint完全互換の確実な実現
    /// </summary>
    public class ExifOrientationService : IExifOrientationService
    {
        private readonly ILogger<ExifOrientationService> _logger;

        public ExifOrientationService(ILogger<ExifOrientationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// EXIF Orientation値を取得
        /// </summary>
        public async Task<ushort> GetExifOrientationAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_EXIF] EXIF Orientation読み取り開始: {FileName}", Path.GetFileName(filePath));

                return await Task.Run(() =>
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    var metadata = frame.Metadata as BitmapMetadata;

                    if (metadata?.ContainsQuery("System.Photo.Orientation") == true)
                    {
                        var orientationValue = metadata.GetQuery("System.Photo.Orientation");
                        if (orientationValue != null)
                        {
                            var orientation = (ushort)orientationValue;
                            _logger.LogDebug("[V3_EXIF] EXIF Orientation取得成功: {Orientation}, ファイル: {FileName}", 
                                orientation, Path.GetFileName(filePath));
                            return orientation;
                        }
                    }

                    _logger.LogDebug("[V3_EXIF] EXIF Orientationなし（標準値1を返却）: {FileName}", Path.GetFileName(filePath));
                    return (ushort)1; // 標準値
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_EXIF] EXIF Orientation読み取りエラー: {FilePath}", filePath);
                return 1; // エラー時は標準値
            }
        }

        /// <summary>
        /// EXIF OrientationからWPF Rotationに変換
        /// </summary>
        public Rotation ConvertExifToWpfRotation(ushort exifOrientation)
        {
            var rotation = exifOrientation switch
            {
                1 => Rotation.Rotate0,    // 正常 (Top-Left)
                3 => Rotation.Rotate180,  // 180度回転 (Bottom-Right)
                6 => Rotation.Rotate90,   // 右90度回転 (Right-Top) 
                8 => Rotation.Rotate270,  // 左90度回転 (Left-Bottom)
                _ => Rotation.Rotate0     // 未対応値は標準
            };

            _logger.LogDebug("[V3_EXIF] EXIF→WPF変換: {ExifValue} → {WpfRotation}", exifOrientation, rotation);
            return rotation;
        }

        /// <summary>
        /// 画像ファイルにEXIF Orientationを設定
        /// </summary>
        public async Task SetExifOrientationAsync(string filePath, ushort orientation)
        {
            try
            {
                _logger.LogDebug("[V3_EXIF] EXIF Orientation設定開始: {FileName}, 値: {Orientation}", 
                    Path.GetFileName(filePath), orientation);

                await Task.Run(() =>
                {
                    // 🎯 WPF標準APIによるEXIF書き込み
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                    var frame = BitmapFrame.Create(stream);
                    var metadata = frame.Metadata?.Clone() as BitmapMetadata;

                    if (metadata != null)
                    {
                        metadata.SetQuery("System.Photo.Orientation", orientation);
                        
                        // 新しいフレーム作成して保存
                        var encoder = new JpegBitmapEncoder(); // 形式に応じて変更
                        var newFrame = BitmapFrame.Create(frame, frame.Thumbnail, metadata, frame.ColorContexts);
                        encoder.Frames.Add(newFrame);
                        
                        stream.Position = 0;
                        encoder.Save(stream);
                    }
                });

                _logger.LogDebug("[V3_EXIF] EXIF Orientation設定完了: {FileName}", Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_EXIF] EXIF Orientation設定エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// EXIF Orientationを正規化（常に1にリセット）
        /// </summary>
        public async Task NormalizeExifOrientationAsync(string filePath, Rotation rotation)
        {
            try
            {
                _logger.LogDebug("[V3_EXIF] EXIF Orientation正規化開始: {FileName}, 適用回転: {Rotation}", 
                    Path.GetFileName(filePath), rotation);

                // EXIF Orientationを1（標準）に設定
                await SetExifOrientationAsync(filePath, 1);

                _logger.LogDebug("[V3_EXIF] EXIF Orientation正規化完了: {FileName}", Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_EXIF] EXIF Orientation正規化エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// EXIF Orientation情報を詳細取得
        /// </summary>
        public async Task<ExifOrientationInfo> GetExifOrientationInfoAsync(string filePath)
        {
            try
            {
                var orientationValue = await GetExifOrientationAsync(filePath);
                var requiredRotation = ConvertExifToWpfRotation(orientationValue);
                
                var (isFlipped, description) = GetOrientationDetails(orientationValue);
                var isWindowsCompatible = await ValidateWindowsCompatibilityAsync(filePath);

                return new ExifOrientationInfo(
                    OrientationValue: orientationValue,
                    RequiredRotation: requiredRotation,
                    IsFlipped: isFlipped,
                    Description: description,
                    IsWindowsCompatible: isWindowsCompatible
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_EXIF] EXIF詳細情報取得エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Windows Photo/Paint互換性チェック
        /// </summary>
        public async Task<bool> ValidateWindowsCompatibilityAsync(string filePath)
        {
            try
            {
                var orientation = await GetExifOrientationAsync(filePath);
                
                // Windows Photo/Paintが正しく処理できるEXIF値
                var compatibleValues = new[] { 1, 3, 6, 8 };
                var isCompatible = Array.Exists(compatibleValues, val => val == orientation);

                _logger.LogDebug("[V3_EXIF] Windows互換性チェック: {FileName}, EXIF: {Orientation}, 互換性: {IsCompatible}", 
                    Path.GetFileName(filePath), orientation, isCompatible);

                return isCompatible;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_EXIF] Windows互換性チェックエラー: {FilePath}", filePath);
                return false;
            }
        }

        // Private helper methods

        private (bool IsFlipped, string Description) GetOrientationDetails(ushort orientation)
        {
            return orientation switch
            {
                1 => (false, "正常 (Top-Left)"),
                2 => (true, "水平フリップ (Top-Right)"),
                3 => (false, "180度回転 (Bottom-Right)"),
                4 => (true, "垂直フリップ (Bottom-Left)"),
                5 => (true, "90度回転+水平フリップ (Left-Top)"),
                6 => (false, "右90度回転 (Right-Top)"),
                7 => (true, "270度回転+水平フリップ (Right-Bottom)"),
                8 => (false, "左90度回転 (Left-Bottom)"),
                _ => (false, $"未対応値 ({orientation})")
            };
        }
    }
}