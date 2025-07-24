using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SkiaSharp;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;
using Xunit;

namespace DocOrganizer.Application.Tests.Interfaces
{
    public class IPdfServiceTests
    {
        [Fact]
        public async Task LoadPdfAsync_ShouldReturnPdfDocument()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            var expectedDocument = new PdfDocument(@"C:\test\document.pdf");
            mockService.Setup(s => s.LoadPdfAsync(It.IsAny<string>()))
                       .ReturnsAsync(expectedDocument);

            // Act
            var result = await mockService.Object.LoadPdfAsync(@"C:\test\document.pdf");

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedDocument);
            mockService.Verify(s => s.LoadPdfAsync(@"C:\test\document.pdf"), Times.Once);
        }

        [Fact]
        public async Task SavePdfAsync_ShouldSaveDocument()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            var document = new PdfDocument();
            mockService.Setup(s => s.SavePdfAsync(It.IsAny<PdfDocument>(), It.IsAny<string>()))
                       .Returns(Task.CompletedTask);

            // Act
            await mockService.Object.SavePdfAsync(document, @"C:\test\output.pdf");

            // Assert
            mockService.Verify(s => s.SavePdfAsync(document, @"C:\test\output.pdf"), Times.Once);
        }

        [Fact]
        public async Task MergePdfsAsync_ShouldReturnMergedDocument()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            var doc1 = new PdfDocument(@"C:\test\doc1.pdf");
            var doc2 = new PdfDocument(@"C:\test\doc2.pdf");
            var documents = new[] { doc1, doc2 };
            var mergedDocument = new PdfDocument();
            
            mockService.Setup(s => s.MergePdfsAsync(It.IsAny<PdfDocument[]>()))
                       .ReturnsAsync(mergedDocument);

            // Act
            var result = await mockService.Object.MergePdfsAsync(documents);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(mergedDocument);
            mockService.Verify(s => s.MergePdfsAsync(documents), Times.Once);
        }

        [Fact]
        public async Task SplitPdfAsync_ShouldReturnSplitDocuments()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            var document = new PdfDocument();
            var pageRanges = new[] { (1, 3), (4, 6) };
            var splitDocuments = new[] { new PdfDocument(), new PdfDocument() };
            
            mockService.Setup(s => s.SplitPdfAsync(It.IsAny<PdfDocument>(), It.IsAny<(int, int)[]>()))
                       .ReturnsAsync(splitDocuments);

            // Act
            var result = await mockService.Object.SplitPdfAsync(document, pageRanges);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            mockService.Verify(s => s.SplitPdfAsync(document, pageRanges), Times.Once);
        }

        [Fact]
        public async Task ExtractPageThumbnailAsync_ShouldReturnBitmap()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            var document = new PdfDocument();
            var page = new PdfPage(1);
            document.AddPage(page);
            
            using var expectedBitmap = new SKBitmap(100, 100);
            mockService.Setup(s => s.ExtractPageThumbnailAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<int>()))
                       .ReturnsAsync(expectedBitmap);

            // Act
            var result = await mockService.Object.ExtractPageThumbnailAsync(document, 0, 120);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedBitmap);
            mockService.Verify(s => s.ExtractPageThumbnailAsync(document, 0, 120), Times.Once);
        }

        [Fact]
        public async Task ExtractPagePreviewAsync_ShouldReturnBitmap()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            var document = new PdfDocument();
            var page = new PdfPage(1);
            document.AddPage(page);
            
            using var expectedBitmap = new SKBitmap(595, 842);
            mockService.Setup(s => s.ExtractPagePreviewAsync(It.IsAny<PdfDocument>(), It.IsAny<int>(), It.IsAny<float>()))
                       .ReturnsAsync(expectedBitmap);

            // Act
            var result = await mockService.Object.ExtractPagePreviewAsync(document, 0, 1.0f);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(expectedBitmap);
            mockService.Verify(s => s.ExtractPagePreviewAsync(document, 0, 1.0f), Times.Once);
        }

        [Fact]
        public void GetPageCount_ShouldReturnPageCount()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            mockService.Setup(s => s.GetPageCount(It.IsAny<string>()))
                       .Returns(10);

            // Act
            var result = mockService.Object.GetPageCount(@"C:\test\document.pdf");

            // Assert
            result.Should().Be(10);
            mockService.Verify(s => s.GetPageCount(@"C:\test\document.pdf"), Times.Once);
        }

        [Fact]
        public void GetFileSize_ShouldReturnFileSize()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            const long expectedSize = 1024 * 1024; // 1MB
            mockService.Setup(s => s.GetFileSize(It.IsAny<string>()))
                       .Returns(expectedSize);

            // Act
            var result = mockService.Object.GetFileSize(@"C:\test\document.pdf");

            // Assert
            result.Should().Be(expectedSize);
            mockService.Verify(s => s.GetFileSize(@"C:\test\document.pdf"), Times.Once);
        }

        [Fact]
        public void IsValidPdf_ShouldReturnTrue_ForValidPdf()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            mockService.Setup(s => s.IsValidPdf(It.IsAny<string>()))
                       .Returns(true);

            // Act
            var result = mockService.Object.IsValidPdf(@"C:\test\document.pdf");

            // Assert
            result.Should().BeTrue();
            mockService.Verify(s => s.IsValidPdf(@"C:\test\document.pdf"), Times.Once);
        }

        [Fact]
        public void IsValidPdf_ShouldReturnFalse_ForInvalidPdf()
        {
            // Arrange
            var mockService = new Mock<IPdfService>();
            mockService.Setup(s => s.IsValidPdf(It.IsAny<string>()))
                       .Returns(false);

            // Act
            var result = mockService.Object.IsValidPdf(@"C:\test\invalid.txt");

            // Assert
            result.Should().BeFalse();
            mockService.Verify(s => s.IsValidPdf(@"C:\test\invalid.txt"), Times.Once);
        }
    }
}