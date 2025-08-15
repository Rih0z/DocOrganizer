using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using DocOrganizer.UI.ViewModels.V3;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocOrganizer.UI.Tests.V3
{
    /// <summary>
    /// 🎯 V3統合テスト: ViewModel間の協調動作検証
    /// 目標: ViewModelアーキテクチャの完全統合テスト
    /// </summary>
    public class ViewModelIntegrationTests
    {
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly Mock<IImageProcessingService> _mockImageProcessingService;
        private readonly Mock<IImageLoaderService> _mockImageLoaderService;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<ILogger<DocumentManagementViewModel>> _mockDocumentLogger;
        private readonly Mock<ILogger<PageOperationViewModel>> _mockPageLogger;
        private readonly Mock<ILogger<PreviewManagementViewModel>> _mockPreviewLogger;

        public ViewModelIntegrationTests()
        {
            _mockDocumentService = new Mock<IDocumentService>();
            _mockImageProcessingService = new Mock<IImageProcessingService>();
            _mockImageLoaderService = new Mock<IImageLoaderService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockDocumentLogger = new Mock<ILogger<DocumentManagementViewModel>>();
            _mockPageLogger = new Mock<ILogger<PageOperationViewModel>>();
            _mockPreviewLogger = new Mock<ILogger<PreviewManagementViewModel>>();
        }

        [Fact]
        public async Task MainCompositeViewModel_CompleteWorkflow_ShouldCoordinateAllViewModels()
        {
            // Arrange
            var testPdfPath = CreateTestPdfFile();
            var testDocument = CreateTestDocument();

            _mockDocumentService.Setup(x => x.LoadDocumentAsync(testPdfPath))
                .ReturnsAsync(testDocument);

            var mainComposite = CreateMainCompositeViewModel();

            // Act & Assert - ドキュメント開封
            await mainComposite.DocumentManagement.OpenDocumentAsync(testPdfPath);

            // ドキュメントが正しく設定されることを確認
            Assert.NotNull(mainComposite.CurrentDocument);
            Assert.Equal(3, mainComposite.Pages.Count);
            Assert.NotNull(mainComposite.SelectedPage);

            // ページ回転操作
            var firstPage = mainComposite.Pages[0];
            await mainComposite.PageOperation.RotatePageAsync(firstPage, 90);

            // 回転後のページ情報更新確認
            Assert.Equal(90, mainComposite.Pages[0].Rotation);

            // ページ削除操作
            await mainComposite.PageOperation.DeletePageAsync(firstPage);

            // 削除後のページ数確認
            Assert.Equal(2, mainComposite.Pages.Count);

            // Cleanup
            File.Delete(testPdfPath);
        }

        [Fact]
        public async Task DragDropHandler_ImageFiles_ShouldIntegrateWithOtherViewModels()
        {
            // Arrange
            var imageFiles = new[] { "test1.jpg", "test2.png", "test3.heic" };
            var mainComposite = CreateMainCompositeViewModel();

            _mockImageProcessingService.Setup(x => x.ConvertImagesToPdfAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(CreateTestDocument());

            // Act
            await mainComposite.DragDropHandler.HandleFilesDropAsync(imageFiles);

            // Assert
            // ステータス管理との統合確認
            Assert.False(mainComposite.StatusManagement.IsProcessing);
            Assert.Contains("完了", mainComposite.StatusManagement.StatusMessage);

            // ドキュメント管理との統合確認
            _mockImageProcessingService.Verify(x => x.ConvertImagesToPdfAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Fact]
        public void StatusManagement_OperationLifecycle_ShouldCoordinateWithOtherViewModels()
        {
            // Arrange
            var mainComposite = CreateMainCompositeViewModel();
            var operationStarted = false;
            var operationCompleted = false;

            mainComposite.StatusManagement.OperationStarted += (s, e) => operationStarted = true;
            mainComposite.StatusManagement.OperationCompleted += (s, e) => operationCompleted = true;

            // Act
            mainComposite.StatusManagement.StartOperation("テスト操作", true);
            mainComposite.StatusManagement.UpdateProgress(50, "進行中...");
            mainComposite.StatusManagement.CompleteOperation("操作完了", true);

            // Assert
            Assert.True(operationStarted);
            Assert.True(operationCompleted);
            Assert.False(mainComposite.StatusManagement.IsProcessing);
            Assert.Equal(100, mainComposite.StatusManagement.ProgressPercentage);
            Assert.Equal("操作完了", mainComposite.StatusManagement.StatusMessage);
        }

        [Fact]
        public async Task PreviewManagement_PageSelection_ShouldUpdateCurrentPage()
        {
            // Arrange
            var mainComposite = CreateMainCompositeViewModel();
            var testDocument = CreateTestDocument();
            var testPage = new PageViewModel(testDocument.Pages[0]);

            // Act
            await mainComposite.PreviewManagement.UpdateCurrentPageAsync(testPage);

            // Assert
            _mockImageLoaderService.Verify(x => x.LoadHighQualityImageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task PageOperation_MultipleOperations_ShouldMaintainConsistency()
        {
            // Arrange
            var mainComposite = CreateMainCompositeViewModel();
            var testDocument = CreateTestDocument();
            
            // ドキュメント設定
            mainComposite.GetType().GetProperty("CurrentDocument")?.SetValue(mainComposite, testDocument);
            foreach (var page in testDocument.Pages)
            {
                mainComposite.Pages.Add(new PageViewModel(page));
            }

            var pageToRotate = mainComposite.Pages[0];
            var pageToDelete = mainComposite.Pages[1];

            // Act - 複数操作の実行
            await mainComposite.PageOperation.RotatePageAsync(pageToRotate, 90);
            await mainComposite.PageOperation.RotatePageAsync(pageToRotate, 180); // 累積270度
            await mainComposite.PageOperation.DeletePageAsync(pageToDelete);

            // Assert
            Assert.Equal(270, pageToRotate.Rotation);
            Assert.Equal(2, mainComposite.Pages.Count); // 1ページ削除済み
            Assert.DoesNotContain(pageToDelete, mainComposite.Pages);
        }

        [Fact]
        public void ViewModelDependencyInjection_AllViewModels_ShouldBeProperlyInitialized()
        {
            // Arrange & Act
            var mainComposite = CreateMainCompositeViewModel();

            // Assert - すべての子ViewModelが適切に初期化されていることを確認
            Assert.NotNull(mainComposite.DocumentManagement);
            Assert.NotNull(mainComposite.PageOperation);
            Assert.NotNull(mainComposite.PreviewManagement);
            Assert.NotNull(mainComposite.DragDropHandler);
            Assert.NotNull(mainComposite.StatusManagement);

            // 初期状態の確認
            Assert.Empty(mainComposite.Pages);
            Assert.Null(mainComposite.SelectedPage);
            Assert.Null(mainComposite.CurrentDocument);
            Assert.False(mainComposite.StatusManagement.IsProcessing);
        }

        [Theory]
        [InlineData("document.pdf")]
        [InlineData("image.jpg")]
        [InlineData("presentation.pptx")]
        public async Task DocumentManagement_DifferentFileTypes_ShouldHandleAppropriately(string fileName)
        {
            // Arrange
            var mainComposite = CreateMainCompositeViewModel();
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(filePath, "test content");

            try
            {
                // Act & Assert
                if (fileName.EndsWith(".pdf"))
                {
                    var testDocument = CreateTestDocument();
                    _mockDocumentService.Setup(x => x.LoadDocumentAsync(filePath)).ReturnsAsync(testDocument);
                    
                    await mainComposite.DocumentManagement.OpenDocumentAsync(filePath);
                    Assert.NotNull(mainComposite.CurrentDocument);
                }
                else
                {
                    // PDF以外のファイルは適切にエラーハンドリングされることを確認
                    _mockDocumentService.Setup(x => x.LoadDocumentAsync(filePath))
                        .ThrowsAsync(new InvalidOperationException("未対応形式"));

                    await Assert.ThrowsAsync<InvalidOperationException>(() => 
                        mainComposite.DocumentManagement.OpenDocumentAsync(filePath));
                }
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        // Private helper methods

        private MainCompositeViewModel CreateMainCompositeViewModel()
        {
            var documentManagement = new DocumentManagementViewModel(
                _mockDocumentService.Object, _mockDialogService.Object, _mockDocumentLogger.Object);
            
            var pageOperation = new PageOperationViewModel(
                _mockImageProcessingService.Object, _mockDialogService.Object, _mockPageLogger.Object);
            
            var previewManagement = new PreviewManagementViewModel(
                _mockImageLoaderService.Object, _mockDialogService.Object, _mockPreviewLogger.Object);
            
            var dragDropHandler = new DragDropHandlerViewModel(
                _mockImageProcessingService.Object, _mockImageLoaderService.Object, _mockDialogService.Object);
            
            var statusManagement = new StatusManagementViewModel(_mockDialogService.Object);

            return new MainCompositeViewModel(
                documentManagement, pageOperation, previewManagement, dragDropHandler, statusManagement);
        }

        private PdfDocument CreateTestDocument()
        {
            var pages = new List<PdfPage>
            {
                new PdfPage { Id = Guid.NewGuid(), PageNumber = 1, ImagePath = "page1.jpg", Rotation = 0 },
                new PdfPage { Id = Guid.NewGuid(), PageNumber = 2, ImagePath = "page2.jpg", Rotation = 0 },
                new PdfPage { Id = Guid.NewGuid(), PageNumber = 3, ImagePath = "page3.jpg", Rotation = 0 }
            };

            return new PdfDocument
            {
                Id = Guid.NewGuid(),
                FilePath = "test.pdf",
                Pages = pages,
                CreatedAt = DateTime.Now
            };
        }

        private string CreateTestPdfFile()
        {
            var testPath = Path.GetTempFileName() + ".pdf";
            File.WriteAllText(testPath, "%PDF-1.4 test content");
            return testPath;
        }
    }
}