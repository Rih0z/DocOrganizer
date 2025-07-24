using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;
using DocOrganizer.Infrastructure.Services;
using Xunit;

namespace DocOrganizer.Application.Tests.Services
{
    public class PdfEditorServiceTests
    {
        private readonly Mock<IPdfService> _mockPdfService;
        private readonly Mock<ILogger<PdfEditorService>> _mockLogger;
        private readonly PdfEditorService _editorService;

        public PdfEditorServiceTests()
        {
            _mockPdfService = new Mock<IPdfService>();
            _mockLogger = new Mock<ILogger<PdfEditorService>>();
            _editorService = new PdfEditorService(_mockPdfService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task OpenFileAsync_ShouldLoadDocumentAndGenerateThumbnails()
        {
            // Arrange
            const string filePath = @"C:\test\document.pdf";
            var document = CreateTestDocument(3);
            
            _mockPdfService.Setup(s => s.LoadPdfAsync(filePath))
                .ReturnsAsync(document);
            
            _mockPdfService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SKBitmap(120, 150));

            // Act
            await _editorService.OpenFileAsync(filePath);

            // Assert
            _editorService.CurrentDocument.Should().NotBeNull();
            _editorService.CurrentDocument!.GetPageCount().Should().Be(3);
            _mockPdfService.Verify(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), 120), Times.Exactly(3));
        }

        [Fact]
        public async Task SaveAsync_WithoutFilePath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _editorService.SaveAsync());
        }

        [Fact]
        public async Task RemovePagesAsync_ShouldRemovePagesAndEnableUndo()
        {
            // Arrange
            await SetupDocumentWithPages(5);
            var pageIndices = new[] { 1, 3 }; // Remove pages at index 1 and 3

            // Act
            await _editorService.RemovePagesAsync(pageIndices);

            // Assert
            _editorService.CurrentDocument!.GetPageCount().Should().Be(3);
            _editorService.CanUndo.Should().BeTrue();
        }

        [Fact]
        public async Task RotatePagesAsync_ShouldRotatePagesAndUpdateThumbnails()
        {
            // Arrange
            await SetupDocumentWithPages(3);
            var pageIndices = new[] { 0, 2 };
            
            _mockPdfService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SKBitmap(120, 150));

            // Act
            await _editorService.RotatePagesAsync(pageIndices, 90);

            // Assert
            _editorService.CurrentDocument!.Pages[0].Rotation.Should().Be(90);
            _editorService.CurrentDocument!.Pages[1].Rotation.Should().Be(0);
            _editorService.CurrentDocument!.Pages[2].Rotation.Should().Be(90);
            _editorService.CanUndo.Should().BeTrue();
        }

        [Fact]
        public async Task ReorderPagesAsync_ShouldMovePageToNewPosition()
        {
            // Arrange
            await SetupDocumentWithPages(4);
            
            // Act
            await _editorService.ReorderPagesAsync(0, 2); // Move first page to third position

            // Assert
            _editorService.CurrentDocument!.Pages[0].PageNumber.Should().Be(2);
            _editorService.CurrentDocument!.Pages[1].PageNumber.Should().Be(3);
            _editorService.CurrentDocument!.Pages[2].PageNumber.Should().Be(1);
            _editorService.CanUndo.Should().BeTrue();
        }

        [Fact]
        public async Task UndoAsync_ShouldRestorePreviousState()
        {
            // Arrange
            await SetupDocumentWithPages(3);
            var originalCount = _editorService.CurrentDocument!.GetPageCount();
            
            await _editorService.RemovePagesAsync(new[] { 1 });
            _editorService.CurrentDocument!.GetPageCount().Should().Be(2);

            // Act
            await _editorService.UndoAsync();

            // Assert
            _editorService.CurrentDocument!.GetPageCount().Should().Be(originalCount);
            _editorService.CanRedo.Should().BeTrue();
        }

        [Fact]
        public async Task RedoAsync_ShouldReapplyUndoneChanges()
        {
            // Arrange
            await SetupDocumentWithPages(3);
            await _editorService.RemovePagesAsync(new[] { 1 });
            await _editorService.UndoAsync();

            // Act
            await _editorService.RedoAsync();

            // Assert
            _editorService.CurrentDocument!.GetPageCount().Should().Be(2);
            _editorService.CanUndo.Should().BeTrue();
        }

        [Fact]
        public async Task CloseDocument_ShouldClearDocumentAndHistory()
        {
            // Arrange
            const string filePath = @"C:\test\document.pdf";
            var document = CreateTestDocument(3);
            
            _mockPdfService.Setup(s => s.LoadPdfAsync(filePath))
                .ReturnsAsync(document);
                
            _mockPdfService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SKBitmap(120, 150));
            
            await _editorService.OpenFileAsync(filePath);

            // Act
            _editorService.CloseDocument();

            // Assert
            _editorService.CurrentDocument.Should().BeNull();
            _editorService.CanUndo.Should().BeFalse();
            _editorService.CanRedo.Should().BeFalse();
        }

        private PdfDocument CreateTestDocument(int pageCount)
        {
            var document = new PdfDocument(@"C:\test\document.pdf");
            for (int i = 1; i <= pageCount; i++)
            {
                var page = new PdfPage(i);
                page.SetDimensions(595, 842);
                document.AddPage(page);
            }
            document.ClearModifiedFlag();
            return document;
        }

        private async Task SetupDocumentWithPages(int pageCount)
        {
            var document = CreateTestDocument(pageCount);
            _mockPdfService.Setup(s => s.LoadPdfAsync(It.IsAny<string>()))
                .ReturnsAsync(document);
            _mockPdfService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SKBitmap(120, 150));
            
            await _editorService.OpenFileAsync(@"C:\test\document.pdf");
        }
    }
}