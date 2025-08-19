using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using DocOrganizer.UI.ViewModels.V3;
using DocOrganizer.UI.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocOrganizer.UI.Tests.V3
{
    public class PageOperationViewModelTests
    {
        private readonly Mock<IPdfEditorService> _mockPdfEditorService;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<IThumbnailGeneratorService> _mockThumbnailService;
        private readonly Mock<ITextOrientationService> _mockTextOrientationService;
        private readonly PageOperationViewModel _viewModel;

        public PageOperationViewModelTests()
        {
            _mockPdfEditorService = new Mock<IPdfEditorService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockThumbnailService = new Mock<IThumbnailGeneratorService>();
            _mockTextOrientationService = new Mock<ITextOrientationService>();
            _viewModel = new PageOperationViewModel(_mockPdfEditorService.Object, _mockDialogService.Object);
        }

        [Fact]
        public async Task MovePageUpCommand_WhenPageSelected_ShouldMovePageUp()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage1 = new PdfPage(1);
            var pdfPage2 = new PdfPage(2);
            var pdfPage3 = new PdfPage(3);
            pdfDocument.AddPage(pdfPage1);
            pdfDocument.AddPage(pdfPage2);
            pdfDocument.AddPage(pdfPage3);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            // ページViewModelを作成して追加
            var page1 = new V3PageViewModel(pdfPage1, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page2 = new V3PageViewModel(pdfPage2, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page3 = new V3PageViewModel(pdfPage3, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            page2.IsSelected = true; // 2番目を選択
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(page1);
            _viewModel.Pages.Add(page2);
            _viewModel.Pages.Add(page3);

            // Act
            await _viewModel.MovePageUpCommand.ExecuteAsync(null);

            // Assert
            _viewModel.Pages[0].Should().Be(page2); // page2が最初に移動
            _viewModel.Pages[1].Should().Be(page1); // page1が2番目に
            _viewModel.Pages[2].Should().Be(page3); // page3はそのまま
            
            // ページ番号が更新されているか確認
            _viewModel.Pages[0].PageNumber.Should().Be(1);
            _viewModel.Pages[1].PageNumber.Should().Be(2);
            _viewModel.Pages[2].PageNumber.Should().Be(3);
        }

        [Fact]
        public async Task MovePageDownCommand_WhenPageSelected_ShouldMovePageDown()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage1 = new PdfPage(1);
            var pdfPage2 = new PdfPage(2);
            var pdfPage3 = new PdfPage(3);
            pdfDocument.AddPage(pdfPage1);
            pdfDocument.AddPage(pdfPage2);
            pdfDocument.AddPage(pdfPage3);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var page1 = new V3PageViewModel(pdfPage1, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page2 = new V3PageViewModel(pdfPage2, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page3 = new V3PageViewModel(pdfPage3, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            page1.IsSelected = true; // 1番目を選択
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(page1);
            _viewModel.Pages.Add(page2);
            _viewModel.Pages.Add(page3);

            // Act
            await _viewModel.MovePageDownCommand.ExecuteAsync(null);

            // Assert
            _viewModel.Pages[0].Should().Be(page2); // page2が最初に
            _viewModel.Pages[1].Should().Be(page1); // page1が2番目に移動
            _viewModel.Pages[2].Should().Be(page3); // page3はそのまま
            
            // ページ番号が更新されているか確認
            _viewModel.Pages[0].PageNumber.Should().Be(1);
            _viewModel.Pages[1].PageNumber.Should().Be(2);
            _viewModel.Pages[2].PageNumber.Should().Be(3);
        }

        [Fact]
        public async Task MovePageUpCommand_WhenNoPageSelected_ShouldShowMessage()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage1 = new PdfPage(1);
            var pdfPage2 = new PdfPage(2);
            pdfDocument.AddPage(pdfPage1);
            pdfDocument.AddPage(pdfPage2);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var page1 = new V3PageViewModel(pdfPage1, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page2 = new V3PageViewModel(pdfPage2, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(page1);
            _viewModel.Pages.Add(page2);

            // Act
            await _viewModel.MovePageUpCommand.ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task MovePageUpCommand_WhenAlreadyAtTop_ShouldShowMessage()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage1 = new PdfPage(1);
            var pdfPage2 = new PdfPage(2);
            pdfDocument.AddPage(pdfPage1);
            pdfDocument.AddPage(pdfPage2);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var page1 = new V3PageViewModel(pdfPage1, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page2 = new V3PageViewModel(pdfPage2, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            page1.IsSelected = true; // 最初のページを選択
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(page1);
            _viewModel.Pages.Add(page2);

            // Act
            await _viewModel.MovePageUpCommand.ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task MovePageDownCommand_WhenAlreadyAtBottom_ShouldShowMessage()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage1 = new PdfPage(1);
            var pdfPage2 = new PdfPage(2);
            pdfDocument.AddPage(pdfPage1);
            pdfDocument.AddPage(pdfPage2);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var page1 = new V3PageViewModel(pdfPage1, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page2 = new V3PageViewModel(pdfPage2, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            page2.IsSelected = true; // 最後のページを選択
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(page1);
            _viewModel.Pages.Add(page2);

            // Act
            await _viewModel.MovePageDownCommand.ExecuteAsync(null);

            // Assert
            _mockDialogService.Verify(x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSelectedPagesCommand_WhenPagesSelected_ShouldDeletePages()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage1 = new PdfPage(1);
            var pdfPage2 = new PdfPage(2);
            var pdfPage3 = new PdfPage(3);
            pdfDocument.AddPage(pdfPage1);
            pdfDocument.AddPage(pdfPage2);
            pdfDocument.AddPage(pdfPage3);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var page1 = new V3PageViewModel(pdfPage1, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page2 = new V3PageViewModel(pdfPage2, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            var page3 = new V3PageViewModel(pdfPage3, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            page2.IsSelected = true; // 削除対象
            page3.IsSelected = true; // 削除対象
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(page1);
            _viewModel.Pages.Add(page2);
            _viewModel.Pages.Add(page3);

            _mockDialogService.Setup(x => x.ShowConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // Act
            await _viewModel.DeleteSelectedPagesCommand.ExecuteAsync(null);

            // Assert
            _viewModel.Pages.Count.Should().Be(1);
            _viewModel.Pages[0].Should().Be(page1);
            _viewModel.Pages[0].PageNumber.Should().Be(1); // ページ番号が更新される
        }

        [Fact]
        public async Task RotateLeftCommand_WhenPageSelected_ShouldRotatePage()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage = new PdfPage(1);
            pdfPage.Rotation = 0;
            pdfDocument.AddPage(pdfPage);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var pageVm = new V3PageViewModel(pdfPage, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            pageVm.IsSelected = true;
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(pageVm);

            // Act
            await _viewModel.RotateLeftCommand.ExecuteAsync(null);

            // Assert
            // 実装では直接PdfPageのRotationプロパティを更新している
            pdfPage.Rotation.Should().Be(270);
            pageVm.Rotation.Should().Be(270);
        }

        [Fact]
        public async Task RotateRightCommand_WhenPageSelected_ShouldRotatePage()
        {
            // Arrange
            var pdfDocument = new PdfDocument();
            var pdfPage = new PdfPage(1);
            pdfPage.Rotation = 0;
            pdfDocument.AddPage(pdfPage);

            _viewModel.SetCurrentDocument(pdfDocument);
            
            var pageVm = new V3PageViewModel(pdfPage, _mockThumbnailService.Object, _mockTextOrientationService.Object);
            pageVm.IsSelected = true;
            
            _viewModel.Pages.Clear();
            _viewModel.Pages.Add(pageVm);

            // Act
            await _viewModel.RotateRightCommand.ExecuteAsync(null);

            // Assert
            // 実装では直接PdfPageのRotationプロパティを更新している
            pdfPage.Rotation.Should().Be(90);
            pageVm.Rotation.Should().Be(90);
        }
    }
}