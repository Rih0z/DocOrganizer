using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Infrastructure.Services;
using Xunit;

namespace DocOrganizer.Application.Tests.Services
{
    /// <summary>
    /// 実際のファイルを使用した統合テスト
    /// エラー処理が適切に機能することを確認
    /// </summary>
    public class ImageProcessingServiceIntegrationTests : IDisposable
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly string _testDataPath;
        private readonly IServiceProvider _serviceProvider;

        public ImageProcessingServiceIntegrationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IPdfService, PdfService>();
            services.AddSingleton<IPdfEditorService, PdfEditorService>();
            services.AddSingleton<IImageProcessingService, ImageProcessingService>();
            
            _serviceProvider = services.BuildServiceProvider();
            _imageProcessingService = _serviceProvider.GetRequiredService<IImageProcessingService>();
            
            _testDataPath = Path.Combine(Path.GetTempPath(), "ImageProcessingIntegrationTests");
            Directory.CreateDirectory(_testDataPath);
        }

        [Fact]
        public async Task RealWorldTest_CorruptedPngFile_ShouldNotCauseInfiniteLoop()
        {
            // Arrange - 破損したPNGファイルを作成
            var corruptedPng = Path.Combine(_testDataPath, "corrupted.png");
            var corruptedData = new byte[1024];
            new Random().NextBytes(corruptedData);
            // PNGヘッダーだけ正しく設定
            corruptedData[0] = 0x89;
            corruptedData[1] = 0x50;
            corruptedData[2] = 0x4E;
            corruptedData[3] = 0x47;
            await File.WriteAllBytesAsync(corruptedPng, corruptedData);

            try
            {
                // Act
                var startTime = DateTime.Now;
                Exception caughtException = null;
                
                try
                {
                    await _imageProcessingService.ConvertImageToPdfAsync(corruptedPng);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }

                var elapsedTime = DateTime.Now - startTime;

                // Assert
                caughtException.Should().NotBeNull();
                caughtException.Should().BeOfType<ArgumentException>();
                // ImageProcessingExceptionは内部で使用され、最終的にNotSupportedExceptionに変換される
                
                // 5秒以内に終了することを確認（無限ループしていない）
                elapsedTime.Should().BeLessThan(TimeSpan.FromSeconds(5));
            }
            finally
            {
                if (File.Exists(corruptedPng)) File.Delete(corruptedPng);
            }
        }

        [Fact]
        public async Task RealWorldTest_NonImageFile_ShouldFailQuickly()
        {
            // Arrange - PDFファイルをPNGとして偽装
            var fakePng = Path.Combine(_testDataPath, "fake.png");
            var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
            await File.WriteAllBytesAsync(fakePng, pdfData);

            try
            {
                // Act
                var startTime = DateTime.Now;
                Exception caughtException = null;
                
                try
                {
                    await _imageProcessingService.ConvertImageToPdfAsync(fakePng);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }

                var elapsedTime = DateTime.Now - startTime;

                // Assert
                caughtException.Should().NotBeNull();
                caughtException.Should().BeOfType<ArgumentException>();
                caughtException.Message.Should().Contain("Invalid image file");
                
                // 1秒以内に終了することを確認（早期終了）
                elapsedTime.Should().BeLessThan(TimeSpan.FromSeconds(1));
            }
            finally
            {
                if (File.Exists(fakePng)) File.Delete(fakePng);
            }
        }

        [Fact]
        public async Task RealWorldTest_ValidPngFile_ShouldProcessSuccessfully()
        {
            // Arrange - 最小限の有効なPNGファイルを作成
            var validPng = Path.Combine(_testDataPath, "valid.png");
            
            // 1x1の赤いピクセルのPNG
            var pngData = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");
            await File.WriteAllBytesAsync(validPng, pngData);

            try
            {
                // Act
                var result = await _imageProcessingService.ConvertImageToPdfAsync(validPng);

                // Assert
                result.Should().NotBeNull();
                result.Pages.Should().HaveCount(1);
            }
            finally
            {
                if (File.Exists(validPng)) File.Delete(validPng);
            }
        }

        [Fact]
        public async Task RealWorldTest_MultipleInvalidFiles_ShouldNotRetryIndefinitely()
        {
            // Arrange
            var files = new string[5];
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.Combine(_testDataPath, $"invalid_{i}.png");
                await File.WriteAllBytesAsync(files[i], new byte[] { 0x00, 0x01, 0x02 });
            }

            try
            {
                // Act
                var startTime = DateTime.Now;
                Exception caughtException = null;
                
                try
                {
                    await _imageProcessingService.ConvertImagesToPdfAsync(files);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }

                var elapsedTime = DateTime.Now - startTime;

                // Assert
                caughtException.Should().NotBeNull();
                
                // 10秒以内に終了することを確認（各ファイル2秒以下）
                elapsedTime.Should().BeLessThan(TimeSpan.FromSeconds(10));
            }
            finally
            {
                foreach (var file in files)
                {
                    if (File.Exists(file)) File.Delete(file);
                }
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
            
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}