using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;
using DocOrganizer.UI.ViewModels;
using Xunit;

namespace DocOrganizer.UI.Tests.ViewModels
{
    public class MainViewModelTests
    {
        private readonly Mock<IPdfEditorService> _mockEditorService;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _mockEditorService = new Mock<IPdfEditorService>();
            _mockDialogService = new Mock<IDialogService>();
            _viewModel = new MainViewModel(_mockEditorService.Object, _mockDialogService.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            _viewModel.OpenCommand.Should().NotBeNull();
            _viewModel.SaveCommand.Should().NotBeNull();
            _viewModel.SaveAsCommand.Should().NotBeNull();
            _viewModel.CloseCommand.Should().NotBeNull();
            _viewModel.ExitCommand.Should().NotBeNull();
            _viewModel.UndoCommand.Should().NotBeNull();
            _viewModel.RedoCommand.Should().NotBeNull();
            _viewModel.DeleteCommand.Should().NotBeNull();
            _viewModel.RotateLeftCommand.Should().NotBeNull();
            _viewModel.RotateRightCommand.Should().NotBeNull();
        }

        [Fact]
        public async Task OpenCommand_ShouldOpenFileAndUpdateUI()
        {
            // Arrange
            const string filePath = @"C:\test\document.pdf";
            var document = CreateTestDocument(3);
            
            _mockDialogService.Setup(d => d.ShowOpenFileDialog(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(filePath);
            
            _mockEditorService.SetupGet(e => e.CurrentDocument)
                .Returns(document);

            // Act
            await _viewModel.OpenCommand.ExecuteAsync(null);

            // Assert
            _mockEditorService.Verify(e => e.OpenFileAsync(filePath), Times.Once);
            _viewModel.Pages.Should().HaveCount(3);
            _viewModel.HasDocument.Should().BeTrue();
            _viewModel.FileInfo.Should().Contain("document.pdf");
        }

        [Fact]
        public async Task SaveCommand_WhenCanSave_ShouldSaveDocument()
        {
            // Arrange
            var document = CreateTestDocument(1);
            document.FilePath = @"C:\test\document.pdf";
            
            _mockEditorService.SetupGet(e => e.CurrentDocument)
                .Returns(document);

            // Act
            var canSave = _viewModel.SaveCommand.CanExecute(null);
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            canSave.Should().BeTrue();
            _mockEditorService.Verify(e => e.SaveAsync(), Times.Once);
        }

        [Fact]
        public void DeleteCommand_WhenNoSelection_ShouldNotExecute()
        {
            // Arrange
            SetupDocumentWithPages(3);

            // Act
            var canDelete = _viewModel.DeleteCommand.CanExecute(null);

            // Assert
            canDelete.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCommand_WithSelection_ShouldRemovePages()
        {
            // Arrange
            SetupDocumentWithPages(3);
            _viewModel.Pages[0].IsSelected = true;
            _viewModel.Pages[2].IsSelected = true;
            
            _mockDialogService.Setup(d => d.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(null);

            // Assert
            _mockEditorService.Verify(e => e.RemovePagesAsync(It.Is<int[]>(arr => arr.Length == 2)), Times.Once);
        }

        [Fact]
        public async Task RotateCommands_WithSelection_ShouldRotatePages()
        {
            // Arrange
            SetupDocumentWithPages(2);
            _viewModel.Pages[0].IsSelected = true;

            // Act
            await _viewModel.RotateLeftCommand.ExecuteAsync(null);
            await _viewModel.RotateRightCommand.ExecuteAsync(null);

            // Assert
            _mockEditorService.Verify(e => e.RotatePagesAsync(It.IsAny<int[]>(), 270), Times.Once); // Left = 270
            _mockEditorService.Verify(e => e.RotatePagesAsync(It.IsAny<int[]>(), 90), Times.Once);  // Right = 90
        }

        [Fact]
        public void ZoomCommands_ShouldChangeZoomLevel()
        {
            // Arrange
            var initialZoom = _viewModel.ZoomLevel;

            // Act
            _viewModel.ZoomInCommand.Execute(null);
            var zoomInLevel = _viewModel.ZoomLevel;
            
            _viewModel.ZoomOutCommand.Execute(null);
            _viewModel.ZoomOutCommand.Execute(null);
            var zoomOutLevel = _viewModel.ZoomLevel;

            // Assert
            zoomInLevel.Should().BeGreaterThan(initialZoom);
            zoomOutLevel.Should().BeLessThan(initialZoom);
        }

        [Fact]
        public void PropertyChanged_ShouldBeRaisedCorrectly()
        {
            // Arrange
            var propertyNames = new List<string>();
            _viewModel.PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

            // Act
            _viewModel.StatusMessage = "Test";
            _viewModel.ZoomLevel = "150%";

            // Assert
            propertyNames.Should().Contain("StatusMessage");
            propertyNames.Should().Contain("ZoomLevel");
        }

        [Fact]
        public void EmptyStateVisibility_ShouldDependOnDocument()
        {
            // Arrange & Act - No document
            var emptyVisibility = _viewModel.EmptyStateVisibility;

            // Assert
            emptyVisibility.Should().Be(System.Windows.Visibility.Visible);

            // Arrange & Act - With document
            SetupDocumentWithPages(1);
            emptyVisibility = _viewModel.EmptyStateVisibility;

            // Assert
            emptyVisibility.Should().Be(System.Windows.Visibility.Collapsed);
        }

        private PdfDocument CreateTestDocument(int pageCount)
        {
            var document = new PdfDocument();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = new PdfPage(i);
                page.SetDimensions(595, 842);
                document.AddPage(page);
            }
            return document;
        }

        private void SetupDocumentWithPages(int pageCount)
        {
            var document = CreateTestDocument(pageCount);
            _mockEditorService.SetupGet(e => e.CurrentDocument).Returns(document);
            _viewModel.RefreshPages();
        }
    }
}