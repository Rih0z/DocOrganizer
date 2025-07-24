using System;
using FluentAssertions;
using SkiaSharp;
using DocOrganizer.Core.Models;
using Xunit;

namespace DocOrganizer.Core.Tests.Models
{
    public class PdfPageTests
    {
        [Fact]
        public void Constructor_WithPageNumber_ShouldInitializeCorrectly()
        {
            // Arrange
            const int pageNumber = 5;

            // Act
            var page = new PdfPage(pageNumber);

            // Assert
            page.PageNumber.Should().Be(pageNumber);
            page.Rotation.Should().Be(0);
            page.Width.Should().Be(0);
            page.Height.Should().Be(0);
            page.IsSelected.Should().BeFalse();
            page.ThumbnailImage.Should().BeNull();
            page.PreviewImage.Should().BeNull();
            page.IsDisposed.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidPageNumber_ShouldThrowArgumentException(int pageNumber)
        {
            // Act & Assert
            Action act = () => new PdfPage(pageNumber);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Page number must be greater than 0*");
        }

        [Fact]
        public void SetDimensions_ShouldUpdateWidthAndHeight()
        {
            // Arrange
            var page = new PdfPage(1);
            const float width = 595f;  // A4 width in points
            const float height = 842f; // A4 height in points

            // Act
            page.SetDimensions(width, height);

            // Assert
            page.Width.Should().Be(width);
            page.Height.Should().Be(height);
        }

        [Theory]
        [InlineData(-1, 100)]
        [InlineData(100, -1)]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        public void SetDimensions_WithInvalidValues_ShouldThrowArgumentException(float width, float height)
        {
            // Arrange
            var page = new PdfPage(1);

            // Act & Assert
            Action act = () => page.SetDimensions(width, height);
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(90)]
        [InlineData(180)]
        [InlineData(270)]
        public void Rotation_WithValidAngle_ShouldSetCorrectly(int rotation)
        {
            // Arrange
            var page = new PdfPage(1);

            // Act
            page.Rotation = rotation;

            // Assert
            page.Rotation.Should().Be(rotation);
        }

        [Theory]
        [InlineData(45)]
        [InlineData(-90)]
        [InlineData(360)]
        public void Rotation_WithInvalidAngle_ShouldThrowArgumentException(int rotation)
        {
            // Arrange
            var page = new PdfPage(1);

            // Act & Assert
            Action act = () => page.Rotation = rotation;
            act.Should().Throw<ArgumentException>()
                .WithMessage("Rotation must be 0, 90, 180, or 270*");
        }

        [Fact]
        public void IsSelected_ShouldBeSettable()
        {
            // Arrange
            var page = new PdfPage(1);

            // Act
            page.IsSelected = true;

            // Assert
            page.IsSelected.Should().BeTrue();

            // Act
            page.IsSelected = false;

            // Assert
            page.IsSelected.Should().BeFalse();
        }

        [Fact]
        public void SetThumbnailImage_ShouldUpdateThumbnailImage()
        {
            // Arrange
            var page = new PdfPage(1);
            using var bitmap = new SKBitmap(100, 100);

            // Act
            page.SetThumbnailImage(bitmap);

            // Assert
            page.ThumbnailImage.Should().NotBeNull();
            page.ThumbnailImage.Should().NotBeSameAs(bitmap); // Should be a copy
        }

        [Fact]
        public void SetThumbnailImage_WithNull_ShouldClearThumbnail()
        {
            // Arrange
            var page = new PdfPage(1);
            using var bitmap = new SKBitmap(100, 100);
            page.SetThumbnailImage(bitmap);

            // Act
            page.SetThumbnailImage(null);

            // Assert
            page.ThumbnailImage.Should().BeNull();
        }

        [Fact]
        public void SetPreviewImage_ShouldUpdatePreviewImage()
        {
            // Arrange
            var page = new PdfPage(1);
            using var bitmap = new SKBitmap(595, 842);

            // Act
            page.SetPreviewImage(bitmap);

            // Assert
            page.PreviewImage.Should().NotBeNull();
            page.PreviewImage.Should().NotBeSameAs(bitmap); // Should be a copy
        }

        [Fact]
        public void SetPreviewImage_WithNull_ShouldClearPreview()
        {
            // Arrange
            var page = new PdfPage(1);
            using var bitmap = new SKBitmap(595, 842);
            page.SetPreviewImage(bitmap);

            // Act
            page.SetPreviewImage(null);

            // Assert
            page.PreviewImage.Should().BeNull();
        }

        [Fact]
        public void Dispose_ShouldDisposeImages()
        {
            // Arrange
            var page = new PdfPage(1);
            using var thumbnailBitmap = new SKBitmap(100, 100);
            using var previewBitmap = new SKBitmap(595, 842);
            page.SetThumbnailImage(thumbnailBitmap);
            page.SetPreviewImage(previewBitmap);

            // Act
            page.Dispose();

            // Assert
            page.IsDisposed.Should().BeTrue();
            page.ThumbnailImage.Should().BeNull();
            page.PreviewImage.Should().BeNull();
        }

        [Fact]
        public void Dispose_MultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var page = new PdfPage(1);

            // Act & Assert
            Action act = () =>
            {
                page.Dispose();
                page.Dispose();
            };
            act.Should().NotThrow();
        }

        [Fact]
        public void GetEffectiveDimensions_WithoutRotation_ShouldReturnOriginalDimensions()
        {
            // Arrange
            var page = new PdfPage(1);
            page.SetDimensions(595f, 842f);
            page.Rotation = 0;

            // Act
            var (width, height) = page.GetEffectiveDimensions();

            // Assert
            width.Should().Be(595f);
            height.Should().Be(842f);
        }

        [Theory]
        [InlineData(90)]
        [InlineData(270)]
        public void GetEffectiveDimensions_WithRotation_ShouldSwapDimensions(int rotation)
        {
            // Arrange
            var page = new PdfPage(1);
            page.SetDimensions(595f, 842f);
            page.Rotation = rotation;

            // Act
            var (width, height) = page.GetEffectiveDimensions();

            // Assert
            width.Should().Be(842f);
            height.Should().Be(595f);
        }

        [Fact]
        public void GetEffectiveDimensions_With180Rotation_ShouldReturnOriginalDimensions()
        {
            // Arrange
            var page = new PdfPage(1);
            page.SetDimensions(595f, 842f);
            page.Rotation = 180;

            // Act
            var (width, height) = page.GetEffectiveDimensions();

            // Assert
            width.Should().Be(595f);
            height.Should().Be(842f);
        }
    }
}