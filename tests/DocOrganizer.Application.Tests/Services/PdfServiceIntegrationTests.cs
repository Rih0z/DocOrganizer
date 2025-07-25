using System;
using System.IO;
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
    public class PdfServiceIntegrationTests : IDisposable
    {
        private readonly IPdfService _pdfService;
        private readonly string _testDirectory;

        public PdfServiceIntegrationTests()
        {
            var logger = new Mock<ILogger<PdfService>>();
            _pdfService = new PdfService(logger.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), $"DocOrganizerTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task ExtractPageThumbnail_ShouldCreateBitmap()
        {
            // Arrange
            var document = new PdfDocument();
            var page = new PdfPage(1);
            page.SetDimensions(595, 842); // A4
            document.AddPage(page);

            // Act
            var thumbnail = await _pdfService.ExtractPageThumbnailAsync(document, 0, 120);

            // Assert
            thumbnail.Should().NotBeNull();
            thumbnail.Width.Should().Be(120);
            thumbnail.Height.Should().BeGreaterThan(0);
            thumbnail.Dispose();
        }

        [Fact]
        public async Task ExtractPagePreview_ShouldCreateScaledBitmap()
        {
            // Arrange
            var document = new PdfDocument();
            var page = new PdfPage(1);
            page.SetDimensions(595, 842); // A4
            document.AddPage(page);

            // Act
            var preview = await _pdfService.ExtractPagePreviewAsync(document, 0, 0.5f);

            // Assert
            preview.Should().NotBeNull();
            preview.Width.Should().Be(297); // 595 * 0.5
            preview.Height.Should().Be(421); // 842 * 0.5
            preview.Dispose();
        }

        [Fact]
        public async Task MergePdfs_ShouldCombineDocuments()
        {
            // Arrange
            var doc1 = new PdfDocument();
            doc1.AddPage(new PdfPage(1));
            doc1.AddPage(new PdfPage(2));

            var doc2 = new PdfDocument();
            doc2.AddPage(new PdfPage(1));

            // Act
            var merged = await _pdfService.MergePdfsAsync(doc1, doc2);

            // Assert
            merged.Should().NotBeNull();
            merged.GetPageCount().Should().Be(3);
            merged.Pages[0].PageNumber.Should().Be(1);
            merged.Pages[1].PageNumber.Should().Be(2);
            merged.Pages[2].PageNumber.Should().Be(3);
        }

        [Fact]
        public async Task SplitPdf_ShouldCreateMultipleDocuments()
        {
            // Arrange
            var document = new PdfDocument();
            for (int i = 1; i <= 5; i++)
            {
                document.AddPage(new PdfPage(i));
            }

            // Act
            var splits = await _pdfService.SplitPdfAsync(
                document,
                (1, 2),  // Pages 1-2
                (3, 5)   // Pages 3-5
            );

            // Assert
            splits.Should().HaveCount(2);
            splits[0].GetPageCount().Should().Be(2);
            splits[1].GetPageCount().Should().Be(3);
        }

        [Fact]
        public void GetFileSize_WithValidFile_ShouldReturnSize()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "test.pdf");
            File.WriteAllBytes(testFile, new byte[] { 1, 2, 3, 4, 5 });

            // Act
            var size = _pdfService.GetFileSize(testFile);

            // Assert
            size.Should().Be(5);
        }

        [Fact]
        public void GetFileSize_WithNonExistentFile_ShouldReturnZero()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "nonexistent.pdf");

            // Act
            var size = _pdfService.GetFileSize(testFile);

            // Assert
            size.Should().Be(0);
        }

        [Fact]
        public void IsValidPdf_WithInvalidFile_ShouldReturnFalse()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "invalid.pdf");
            File.WriteAllText(testFile, "This is not a PDF");

            // Act
            var isValid = _pdfService.IsValidPdf(testFile);

            // Assert
            isValid.Should().BeFalse();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }
}