using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using DocOrganizer.Core.Models;
using Xunit;

namespace DocOrganizer.Core.Tests.Models
{
    public class PdfDocumentTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithEmptyPages()
        {
            // Act
            var document = new PdfDocument();

            // Assert
            document.Pages.Should().NotBeNull();
            document.Pages.Should().BeEmpty();
            document.FilePath.Should().BeNull();
            document.IsModified.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithFilePath_ShouldSetFilePath()
        {
            // Arrange
            const string filePath = @"C:\test\document.pdf";

            // Act
            var document = new PdfDocument(filePath);

            // Assert
            document.FilePath.Should().Be(filePath);
            document.Pages.Should().BeEmpty();
            document.IsModified.Should().BeFalse();
        }

        [Fact]
        public void AddPage_ShouldAddPageToDocument()
        {
            // Arrange
            var document = new PdfDocument();
            var page = new PdfPage(1);

            // Act
            document.AddPage(page);

            // Assert
            document.Pages.Should().ContainSingle();
            document.Pages[0].Should().Be(page);
            document.IsModified.Should().BeTrue();
        }

        [Fact]
        public void AddPage_Null_ShouldThrowArgumentNullException()
        {
            // Arrange
            var document = new PdfDocument();

            // Act & Assert
            Action act = () => document.AddPage(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RemovePage_ShouldRemovePageFromDocument()
        {
            // Arrange
            var document = new PdfDocument();
            var page1 = new PdfPage(1);
            var page2 = new PdfPage(2);
            document.AddPage(page1);
            document.AddPage(page2);
            document.ClearModifiedFlag();

            // Act
            document.RemovePage(page1);

            // Assert
            document.Pages.Should().ContainSingle();
            document.Pages[0].Should().Be(page2);
            document.IsModified.Should().BeTrue();
        }

        [Fact]
        public void RemovePage_NonExistentPage_ShouldReturnFalse()
        {
            // Arrange
            var document = new PdfDocument();
            var page = new PdfPage(1);

            // Act
            var result = document.RemovePage(page);

            // Assert
            result.Should().BeFalse();
            document.IsModified.Should().BeFalse();
        }

        [Fact]
        public void RemovePageAt_ValidIndex_ShouldRemovePage()
        {
            // Arrange
            var document = new PdfDocument();
            var page1 = new PdfPage(1);
            var page2 = new PdfPage(2);
            var page3 = new PdfPage(3);
            document.AddPage(page1);
            document.AddPage(page2);
            document.AddPage(page3);
            document.ClearModifiedFlag();

            // Act
            document.RemovePageAt(1);

            // Assert
            document.Pages.Should().HaveCount(2);
            document.Pages[0].Should().Be(page1);
            document.Pages[1].Should().Be(page3);
            document.IsModified.Should().BeTrue();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void RemovePageAt_InvalidIndex_ShouldThrowArgumentOutOfRangeException(int index)
        {
            // Arrange
            var document = new PdfDocument();
            document.AddPage(new PdfPage(1));
            document.AddPage(new PdfPage(2));
            document.AddPage(new PdfPage(3));

            // Act & Assert
            Action act = () => document.RemovePageAt(index);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void MovePage_ShouldReorderPages()
        {
            // Arrange
            var document = new PdfDocument();
            var page1 = new PdfPage(1);
            var page2 = new PdfPage(2);
            var page3 = new PdfPage(3);
            document.AddPage(page1);
            document.AddPage(page2);
            document.AddPage(page3);
            document.ClearModifiedFlag();

            // Act
            document.MovePage(0, 2); // Move first page to last position

            // Assert
            document.Pages[0].Should().Be(page2);
            document.Pages[1].Should().Be(page3);
            document.Pages[2].Should().Be(page1);
            document.IsModified.Should().BeTrue();
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)]
        [InlineData(0, 3)]
        public void MovePage_InvalidIndices_ShouldThrowArgumentOutOfRangeException(int fromIndex, int toIndex)
        {
            // Arrange
            var document = new PdfDocument();
            document.AddPage(new PdfPage(1));
            document.AddPage(new PdfPage(2));
            document.AddPage(new PdfPage(3));

            // Act & Assert
            Action act = () => document.MovePage(fromIndex, toIndex);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RotatePages_ShouldRotateSelectedPages()
        {
            // Arrange
            var document = new PdfDocument();
            var page1 = new PdfPage(1) { Rotation = 0 };
            var page2 = new PdfPage(2) { Rotation = 0 };
            var page3 = new PdfPage(3) { Rotation = 0 };
            document.AddPage(page1);
            document.AddPage(page2);
            document.AddPage(page3);
            document.ClearModifiedFlag();

            // Act
            document.RotatePages(new[] { 0, 2 }, 90);

            // Assert
            page1.Rotation.Should().Be(90);
            page2.Rotation.Should().Be(0);
            page3.Rotation.Should().Be(90);
            document.IsModified.Should().BeTrue();
        }

        [Fact]
        public void RotatePages_WithFullRotation_ShouldWrapAround()
        {
            // Arrange
            var document = new PdfDocument();
            var page = new PdfPage(1) { Rotation = 270 };
            document.AddPage(page);
            document.ClearModifiedFlag();

            // Act
            document.RotatePages(new[] { 0 }, 90);

            // Assert
            page.Rotation.Should().Be(0);
        }

        [Fact]
        public void GetPageCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var document = new PdfDocument();
            document.AddPage(new PdfPage(1));
            document.AddPage(new PdfPage(2));

            // Act
            var count = document.GetPageCount();

            // Assert
            count.Should().Be(2);
        }

        [Fact]
        public void ClearModifiedFlag_ShouldResetModifiedStatus()
        {
            // Arrange
            var document = new PdfDocument();
            document.AddPage(new PdfPage(1));
            document.IsModified.Should().BeTrue();

            // Act
            document.ClearModifiedFlag();

            // Assert
            document.IsModified.Should().BeFalse();
        }

        [Fact]
        public void Dispose_ShouldDisposeAllPages()
        {
            // Arrange
            var document = new PdfDocument();
            var page1 = new PdfPage(1);
            var page2 = new PdfPage(2);
            document.AddPage(page1);
            document.AddPage(page2);

            // Act
            document.Dispose();

            // Assert
            page1.IsDisposed.Should().BeTrue();
            page2.IsDisposed.Should().BeTrue();
        }
    }
}