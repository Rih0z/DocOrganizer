using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;
using DocOrganizer.Infrastructure.Services;
using Xunit;
using SkiaSharp;

namespace DocOrganizer.Application.Tests.Services
{
    public class PdfRotationErrorTests
    {
        private readonly Mock<IPdfService> _mockPdfService;
        private readonly Mock<ILogger<PdfEditorService>> _mockLogger;
        private readonly PdfEditorService _editorService;

        public PdfRotationErrorTests()
        {
            _mockPdfService = new Mock<IPdfService>();
            _mockLogger = new Mock<ILogger<PdfEditorService>>();
            
            _editorService = new PdfEditorService(_mockPdfService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RotatePagesAsync_WhenNoDocumentOpen_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var pageIndices = new[] { 0, 1 };
            var degrees = 90;

            // Act
            Func<Task> act = async () => await _editorService.RotatePagesAsync(pageIndices, degrees);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No document is currently open");
        }

        [Fact]
        public async Task RotatePagesAsync_WithInvalidPageIndices_ShouldHandleGracefully()
        {
            // Arrange
            var document = CreateTestDocument(3);
            await SetupDocumentAsync(document);
            
            var pageIndices = new[] { -1, 5, 10 }; // 無効なインデックス
            var degrees = 90;

            // Act
            await _editorService.RotatePagesAsync(pageIndices, degrees);

            // Assert
            // 有効なページのみが回転され、無効なインデックスは無視される
            document.Pages[0].Rotation.Should().Be(0); // インデックス範囲外
            document.Pages[1].Rotation.Should().Be(0); // 回転対象外
            document.Pages[2].Rotation.Should().Be(0); // 回転対象外
        }

        [Fact]
        public async Task RotatePagesAsync_WithInvalidDegrees_ShouldNormalizeRotation()
        {
            // Arrange
            var document = CreateTestDocument(2);
            document.Pages[0].Rotation = 90; // 既に90度回転
            await SetupDocumentAsync(document);
            
            var pageIndices = new[] { 0 };
            var degrees = 370; // 360度を超える値

            // Act
            await _editorService.RotatePagesAsync(pageIndices, degrees);

            // Assert
            // (90 + 370) % 360 = 100
            document.Pages[0].Rotation.Should().Be(100);
        }

        [Fact]
        public async Task RotatePagesAsync_WhenThumbnailUpdateFails_ShouldStillCompleteRotation()
        {
            // Arrange
            var document = CreateTestDocument(2);
            await SetupDocumentAsync(document);
            
            // サムネイル更新時にエラーを発生させる
            _mockPdfService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new IOException("Thumbnail generation failed"));

            var pageIndices = new[] { 0, 1 };
            var degrees = 90;

            // Act
            await _editorService.RotatePagesAsync(pageIndices, degrees);

            // Assert
            // 回転は完了している
            document.Pages[0].Rotation.Should().Be(90);
            document.Pages[1].Rotation.Should().Be(90);
            document.IsModified.Should().BeTrue();
            
            // エラーログが記録されている
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update thumbnail")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2)); // 2ページ分のエラー
        }

        [Fact]
        public async Task RotatePagesAsync_WithEmptyPageIndices_ShouldNotThrow()
        {
            // Arrange
            var document = CreateTestDocument(3);
            await SetupDocumentAsync(document);
            
            var pageIndices = Array.Empty<int>(); // 空配列
            var degrees = 90;

            // Act
            Func<Task> act = async () => await _editorService.RotatePagesAsync(pageIndices, degrees);

            // Assert
            await act.Should().NotThrowAsync();
            document.Pages[0].Rotation.Should().Be(0);
            document.Pages[1].Rotation.Should().Be(0);
            document.Pages[2].Rotation.Should().Be(0);
        }

        [Fact]
        public async Task RotatePagesAsync_WithNullPageIndices_ShouldThrow()
        {
            // Arrange
            var document = CreateTestDocument(2);
            await SetupDocumentAsync(document);
            
            int[]? pageIndices = null;
            var degrees = 90;

            // Act
            Func<Task> act = async () => await _editorService.RotatePagesAsync(pageIndices!, degrees);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ApplyRotation_WithCorruptedImage_ShouldHandleError()
        {
            // Arrange
            SKBitmap? rotatedBitmap = null;
            
            // Act
            try
            {
                // null ビットマップで回転を試みる
                // 注: SimplePdfService.ApplyRotationはprivateメソッドのため、
                // ExtractPageThumbnailAsyncを通じて間接的にテスト
                var document = new PdfDocument();
                var page = new PdfPage(1);
                page.SourceImagePath = "non-existent-file.jpg"; // 存在しないファイル
                page.Rotation = 90;
                document.AddPage(page);
                
                rotatedBitmap = await SimplePdfService.ExtractPageThumbnailAsync(document, 0, 120);
            }
            catch
            {
                // エラーは期待される
            }

            // Assert
            rotatedBitmap.Should().BeNull(); // エラー時はnullを返す
        }

        private PdfDocument CreateTestDocument(int pageCount)
        {
            var document = new PdfDocument();
            for (int i = 0; i < pageCount; i++)
            {
                var page = new PdfPage(i + 1);
                page.SetDimensions(595, 842);
                document.AddPage(page);
            }
            return document;
        }

        private async Task SetupDocumentAsync(PdfDocument document)
        {
            _mockPdfService.Setup(s => s.LoadPdfAsync(It.IsAny<string>()))
                .ReturnsAsync(document);
            
            // サムネイル生成のモック
            _mockPdfService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SKBitmap(120, 150));
            
            await _editorService.OpenFileAsync("test.pdf");
        }
    }
}