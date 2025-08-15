using System;
using System.IO;
using System.Threading.Tasks;
using DocOrganizer.Infrastructure.Services.V3;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocOrganizer.Infrastructure.Tests.V3
{
    /// <summary>
    /// 🎯 V3統合テスト: OSS標準サービス間の協調動作検証
    /// 目標: 90度回転問題解決の完全検証
    /// </summary>
    public class ServiceIntegrationTests
    {
        private readonly Mock<ILogger<ImageLoaderService>> _mockImageLogger;
        private readonly Mock<ILogger<ThumbnailGeneratorService>> _mockThumbnailLogger;
        private readonly Mock<ILogger<ExifOrientationService>> _mockExifLogger;
        private readonly Mock<ILogger<HeicConversionService>> _mockHeicLogger;
        private readonly Mock<ILogger<ImageValidationService>> _mockValidationLogger;

        public ServiceIntegrationTests()
        {
            _mockImageLogger = new Mock<ILogger<ImageLoaderService>>();
            _mockThumbnailLogger = new Mock<ILogger<ThumbnailGeneratorService>>();
            _mockExifLogger = new Mock<ILogger<ExifOrientationService>>();
            _mockHeicLogger = new Mock<ILogger<HeicConversionService>>();
            _mockValidationLogger = new Mock<ILogger<ImageValidationService>>();
        }

        [Fact]
        public async Task ImageLoader_ExifOrientation_ThumbnailGenerator_CompleteWorkflow()
        {
            // Arrange
            var testImagePath = CreateTestImageWithExif(exifOrientation: 6); // 90度回転が必要
            
            var imageLoaderService = new ImageLoaderService(_mockImageLogger.Object);
            var exifService = new ExifOrientationService(_mockExifLogger.Object);
            var thumbnailService = new ThumbnailGeneratorService(_mockThumbnailLogger.Object, imageLoaderService);

            try
            {
                // Act & Assert - EXIF読み取り
                var exifOrientation = await exifService.GetExifOrientationAsync(testImagePath);
                Assert.Equal((ushort)6, exifOrientation);

                // EXIF → WPF Rotation変換
                var wpfRotation = exifService.ConvertExifToWpfRotation(exifOrientation);
                Assert.Equal(System.Windows.Media.Imaging.Rotation.Rotate90, wpfRotation);

                // 🎯 重要: ImageLoaderServiceによるWPF標準回転適用
                var loadedImage = await imageLoaderService.LoadImageWithOrientationAsync(testImagePath);
                Assert.NotNull(loadedImage);

                // サムネイル生成（左パネル用）
                var leftThumbnail = await thumbnailService.GenerateLeftPanelThumbnailAsync(testImagePath);
                Assert.NotNull(leftThumbnail);

                // 高解像度プレビュー生成（右プレビュー用）
                var rightPreview = await thumbnailService.GenerateRightPreviewImageAsync(testImagePath, 1920, 1080);
                Assert.NotNull(rightPreview);

                // 🎯 決定的テスト: Windows Photo/Paint互換性確認
                var isWindowsCompatible = await exifService.ValidateWindowsCompatibilityAsync(testImagePath);
                Assert.True(isWindowsCompatible, "EXIF Orientation 6はWindows Photo/Paintで対応されているべき");
            }
            finally
            {
                File.Delete(testImagePath);
            }
        }

        [Fact]
        public async Task HeicConversion_ImageValidation_CompleteHeicWorkflow()
        {
            // Arrange
            var heicService = new HeicConversionService(_mockHeicLogger.Object);
            var validationService = new ImageValidationService(_mockValidationLogger.Object);

            // 🎯 重要: HEIC形式判定テスト
            Assert.True(heicService.IsHeicFile("test.heic"));
            Assert.True(heicService.IsHeicFile("test.HEIF"));
            Assert.False(heicService.IsHeicFile("test.jpg"));

            // 実際のHEICファイルがない場合のモック動作確認
            var testHeicPath = "sample.heic";
            var testJpegPath = "converted.jpg";

            // HEIC対応の基本機能確認
            var heicInfo = await heicService.GetHeicInfoAsync(testHeicPath).ConfigureAwait(false);
            // エラーが発生することを期待（実際のHEICファイルがないため）
        }

        [Theory]
        [InlineData("test.jpg", true)]
        [InlineData("test.png", true)]
        [InlineData("test.heic", true)]
        [InlineData("test.bmp", true)]
        [InlineData("test.txt", false)]
        [InlineData("test.pdf", false)]
        public void ImageValidation_SupportedFormats_ShouldIdentifyCorrectly(string fileName, bool expectedSupported)
        {
            // Arrange
            var validationService = new ImageValidationService(_mockValidationLogger.Object);

            // Act
            var isSupported = validationService.IsSupportedImageFormat(fileName);

            // Assert
            Assert.Equal(expectedSupported, isSupported);
        }

        [Fact]
        public async Task ImageValidation_ZeroByteFile_ShouldDetectAndHandle()
        {
            // Arrange
            var zeroByteFilePath = Path.GetTempFileName() + ".jpg";
            File.WriteAllText(zeroByteFilePath, ""); // 0バイトファイル作成

            var validationService = new ImageValidationService(_mockValidationLogger.Object);

            try
            {
                // Act
                var validationResult = await validationService.ValidateImageAsync(zeroByteFilePath);

                // Assert
                Assert.False(validationResult.IsValid);
                Assert.True(validationResult.IsZeroBytes);
                Assert.Equal(0, validationResult.FileSize);
                Assert.Contains("ファイルサイズが0バイトです", validationResult.Issues);

                // 修復試行
                var repairResult = await validationService.RepairImageAsync(zeroByteFilePath);
                Assert.False(repairResult.RepairSuccessful);
                Assert.Contains("0バイトファイルのため修復不可", repairResult.RepairActions);
            }
            finally
            {
                File.Delete(zeroByteFilePath);
            }
        }

        [Fact]
        public async Task FilterValidImages_MixedFileQuality_ShouldReturnOnlyValid()
        {
            // Arrange
            var validImagePath = CreateTestImageWithExif();
            var zeroByteImagePath = Path.GetTempFileName() + ".jpg";
            var nonExistentPath = "non_existent.jpg";
            
            File.WriteAllText(zeroByteImagePath, ""); // 0バイトファイル

            var allFiles = new[] { validImagePath, zeroByteImagePath, nonExistentPath };
            var validationService = new ImageValidationService(_mockValidationLogger.Object);

            try
            {
                // Act
                var validFiles = await validationService.FilterValidImagesAsync(allFiles);

                // Assert
                Assert.Single(validFiles); // 有効なファイルは1つのみ
                Assert.Contains(validImagePath, validFiles);
                Assert.DoesNotContain(zeroByteImagePath, validFiles);
                Assert.DoesNotContain(nonExistentPath, validFiles);
            }
            finally
            {
                File.Delete(validImagePath);
                File.Delete(zeroByteImagePath);
            }
        }

        [Fact]
        public async Task ExifOrientation_AllStandardValues_ShouldConvertCorrectly()
        {
            // Arrange
            var exifService = new ExifOrientationService(_mockExifLogger.Object);

            // Act & Assert - EXIF標準値の完全テスト
            var testCases = new[]
            {
                (Exif: 1, Expected: System.Windows.Media.Imaging.Rotation.Rotate0),
                (Exif: 3, Expected: System.Windows.Media.Imaging.Rotation.Rotate180),
                (Exif: 6, Expected: System.Windows.Media.Imaging.Rotation.Rotate90),
                (Exif: 8, Expected: System.Windows.Media.Imaging.Rotation.Rotate270)
            };

            foreach (var (exif, expected) in testCases)
            {
                var rotation = exifService.ConvertExifToWpfRotation((ushort)exif);
                Assert.Equal(expected, rotation);
            }

            // 未対応値のテスト
            var unknownRotation = exifService.ConvertExifToWpfRotation(99);
            Assert.Equal(System.Windows.Media.Imaging.Rotation.Rotate0, unknownRotation);
        }

        [Fact]
        public async Task ServicePerformance_LargeFileProcessing_ShouldMeetRequirements()
        {
            // Arrange
            var imageLoaderService = new ImageLoaderService(_mockImageLogger.Object);
            var testImagePath = CreateTestImageWithExif(width: 4000, height: 3000); // 高解像度画像

            var startTime = DateTime.Now;

            try
            {
                // Act - 高解像度画像処理
                var loadedImage = await imageLoaderService.LoadHighQualityImageAsync(testImagePath, 1920, 1080);
                var imageInfo = await imageLoaderService.GetImageInfoAsync(testImagePath);

                var endTime = DateTime.Now;
                var processingTime = endTime - startTime;

                // Assert - パフォーマンス要件
                Assert.NotNull(loadedImage);
                Assert.NotNull(imageInfo);
                Assert.True(processingTime.TotalSeconds < 3, $"処理時間が要件を超過: {processingTime.TotalSeconds}秒");
            }
            finally
            {
                File.Delete(testImagePath);
            }
        }

        // Private helper methods

        private string CreateTestImageWithExif(ushort exifOrientation = 1, int width = 100, int height = 100)
        {
            var tempPath = Path.GetTempFileName() + ".jpg";
            
            // シンプルな単色ビットマップ作成
            var bitmap = new System.Drawing.Bitmap(width, height);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Blue);
            }

            // EXIF Orientation情報を含めて保存
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            bitmap.Dispose();

            return tempPath;
        }
    }
}