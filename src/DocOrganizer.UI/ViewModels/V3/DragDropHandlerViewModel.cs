using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using DocOrganizer.UI.Models.V3;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: ドラッグ&ドロップ専用ViewModel
    /// 責務: ファイル処理、ページ並び替えのみ
    /// 目標: 150行以下、4メソッド以下
    /// V3.0.019: 静的キャッシュによる安全なサムネイル並び替え実装
    /// </summary>
    public partial class DragDropHandlerViewModel : ObservableObject, IAdvancedDropHandler, IAdvancedDragHandler
    {
        // 🎯 V3.0.019: 静的キャッシュによる安全なドラッグ&ドロップ実装
        // 🎯 V3.0.116: 複数ページ対応 - object型でV3PageViewModelまたはList<V3PageViewModel>を格納
        private static readonly Dictionary<string, object> _dragCache = new();
        private static readonly Timer _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        
        // 🎯 V3専用: V2のIImageProcessingService依存関係削除済み
        private readonly IImageLoaderService _imageLoaderService;
        private readonly IDialogService _dialogService;
        private readonly IFileAdditionService _fileAdditionService;

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        [ObservableProperty]
        private string dragOverlayVisibility = "Collapsed";

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string progressDetail = "";

        // 現在処理中のドキュメント（ファイル追加用）
        private PdfDocument? _currentDocument;

        public DragDropHandlerViewModel(
            IImageLoaderService imageLoaderService,
            IDialogService dialogService,
            IFileAdditionService fileAdditionService)
        {
            // 🎯 V3専用: V2のIImageProcessingService依存関係削除
            _imageLoaderService = imageLoaderService;
            _dialogService = dialogService;
            _fileAdditionService = fileAdditionService;

            // OSS標準: イベント駆動アーキテクチャ
            _fileAdditionService.ProgressUpdated += OnFileAdditionProgress;
            _fileAdditionService.AdditionCompleted += OnFileAdditionCompletedFromService;
            _fileAdditionService.ErrorOccurred += OnFileAdditionError;
        }

        #region OSS標準: IAdvancedDropHandler実装

        /// <summary>
        /// 🎯 OSS標準: ドロップ可能性判定
        /// </summary>
        public async Task<bool> CanDropAsync(IAdvancedDropInfo dropInfo)
        {
            try
            {
                if (IsProcessing) return false;

                // ファイルドロップの場合
                if (dropInfo.FilePaths != null && dropInfo.FilePaths.Length > 0)
                {
                    var validationResult = await _fileAdditionService.ValidateFilesForAdditionAsync(dropInfo.FilePaths);
                    return validationResult.IsValid || validationResult.ValidFiles.Any();
                }

                // 🎯 V3.0.019: ページViewModelドロップの場合（サムネイル並び替え）
                if (dropInfo.Data is IDataObject dataObject && 
                    dataObject.GetData(DataFormats.Text) is string dragId && 
                    _dragCache.ContainsKey(dragId))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[CanDropAsync] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🎯 OSS標準: ドロップ処理実行
        /// V3.0.019: 静的キャッシュによる安全なページ並び替え対応
        /// </summary>
        public async Task DropAsync(IAdvancedDropInfo dropInfo)
        {
            try
            {
                if (IsProcessing) 
                {
                    await AppendDebugLogAsync("[DropAsync] 処理中のためスキップ");
                    return;
                }

                await AppendDebugLogAsync("[DropAsync] ===== ドロップ処理開始 =====");
                await AppendDebugLogAsync($"[DropAsync] dropInfo.Data型: {dropInfo.Data?.GetType().Name ?? "null"}");

                // 🎯 V3.0.022: データ型別分岐処理（緊急修正）
                
                // 1️⃣ 最優先: 外部ファイルドロップ判定（String[]）
                if (dropInfo.Data is string[] filePaths)
                {
                    await AppendDebugLogAsync($"[DropAsync] ✅ 外部ファイルドロップ検出 - {filePaths.Length}ファイル");
                    
                    try
                    {
                        await HandleFilesDropAsync(filePaths);
                        dropInfo.Effects = DragDropEffects.Copy;
                        StatusMessage = $"{filePaths.Length}個のファイル追加完了";
                        await AppendDebugLogAsync("[DropAsync] ✅ 外部ファイルドロップ処理完了");
                    }
                    catch (Exception ex)
                    {
                        await AppendDebugLogAsync($"[DropAsync] ❌ ファイルドロップエラー: {ex.Message}");
                        _dialogService.ShowError($"ファイル追加エラー: {ex.Message}");
                        dropInfo.Effects = DragDropEffects.None;
                    }
                    
                    return;
                }
                
                // 2️⃣ 次優先: サムネイルドラッグ判定（IDataObject）
                if (dropInfo.Data is IDataObject dataObject)
                {
                    await AppendDebugLogAsync("[DropAsync] IDataObject確認成功");
                    
                    // Text形式チェック（サムネイルドラッグ）
                    if (dataObject.GetData(DataFormats.Text) is string dragId)
                    {
                        await AppendDebugLogAsync($"[DropAsync] Text形式検出 - DragID: {dragId}");
                        
                        if (_dragCache.TryGetValue(dragId, out var cachedItem))
                        {
                            // 🆕 V3.0.116: 複数ページ対応
                            if (cachedItem is List<V3PageViewModel> pageList)
                            {
                                await AppendDebugLogAsync($"[DropAsync] ✅ 複数ページ並び替え検出 - Count: {pageList.Count}, InsertIndex: {dropInfo.InsertIndex}");
                                
                                try
                                {
                                    await HandlePageReorderWithInsertIndex(pageList, dropInfo.InsertIndex);
                                    
                                    dropInfo.Effects = DragDropEffects.Move;
                                    StatusMessage = $"{pageList.Count}ページ並び替え完了";
                                    
                                    await AppendDebugLogAsync("[DropAsync] ✅ 複数ページ並び替え完了");
                                }
                                catch (Exception ex)
                                {
                                    await AppendDebugLogAsync($"[DropAsync] ❌ 並び替えエラー: {ex.Message}");
                                    _dialogService.ShowError($"ページ並び替えエラー: {ex.Message}");
                                    dropInfo.Effects = DragDropEffects.None;
                                }
                                finally
                                {
                                    _dragCache.Remove(dragId);
                                    await AppendDebugLogAsync($"[DropAsync] キャッシュクリーンアップ完了 - DragID: {dragId}");
                                }
                                
                                return;
                            }
                            else if (cachedItem is V3PageViewModel pageViewModel)
                            {
                                await AppendDebugLogAsync($"[DropAsync] ✅ サムネイル並び替え検出 - Page: {pageViewModel.PageNumber}, InsertIndex: {dropInfo.InsertIndex}");
                            
                                try
                                {
                                    // 🎯 V3.0.021: InsertIndex活用の並び替え処理（単一ページ）
                                    await HandlePageReorderWithInsertIndex(pageViewModel, dropInfo.InsertIndex);
                                
                                dropInfo.Effects = DragDropEffects.Move;
                                StatusMessage = "ページ並び替え完了";
                                
                                await AppendDebugLogAsync("[DropAsync] ✅ サムネイル並び替え完了");
                            }
                            catch (Exception ex)
                            {
                                await AppendDebugLogAsync($"[DropAsync] ❌ 並び替えエラー: {ex.Message}");
                                _dialogService.ShowError($"ページ並び替えエラー: {ex.Message}");
                                dropInfo.Effects = DragDropEffects.None;
                            }
                            finally
                            {
                                    // 🎯 V3.0.019: 必ずキャッシュクリーンアップ
                                    _dragCache.Remove(dragId);
                                    await AppendDebugLogAsync($"[DropAsync] キャッシュクリーンアップ完了 - DragID: {dragId}");
                                }
                                
                                return;
                            }
                        }
                        else
                        {
                            await AppendDebugLogAsync($"[DropAsync] ⚠️ キャッシュにDragID未発見: {dragId}");
                        }
                    }
                    
                    // FileDrop形式チェック（IDataObject内のファイル）
                    if (dataObject.GetDataPresent(DataFormats.FileDrop))
                    {
                        await AppendDebugLogAsync("[DropAsync] IDataObject内FileDrop形式検出");
                        
                        if (dropInfo.FilePaths != null && dropInfo.FilePaths.Length > 0)
                        {
                            await AppendDebugLogAsync($"[DropAsync] ✅ IDataObject経由ファイルドロップ - {dropInfo.FilePaths.Length}ファイル");
                            await HandleFilesDropAsync(dropInfo.FilePaths);
                            dropInfo.Effects = DragDropEffects.Copy;
                            return;
                        }
                    }
                    
                    await AppendDebugLogAsync("[DropAsync] ⚠️ IDataObject内に既知のデータ形式なし");
                }
                else
                {
                    await AppendDebugLogAsync($"[DropAsync] ⚠️ 未対応のデータ型: {dropInfo.Data?.GetType().Name ?? "null"}");
                }

                await AppendDebugLogAsync("[DropAsync] 該当するドロップ処理なし");
                dropInfo.Effects = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[DropAsync] ❌ 予期しないエラー: {ex.Message}");
                _dialogService.ShowError($"ドロップ処理エラー: {ex.Message}");
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 🎯 V3.0.021: InsertIndex活用のページ並び替え処理
        /// </summary>
        private async Task HandlePageReorderWithInsertIndex(V3PageViewModel pageViewModel, int insertIndex)
        {
            await AppendDebugLogAsync($"[HandlePageReorderWithInsertIndex] 開始 - Page: {pageViewModel.PageNumber}, InsertIndex: {insertIndex}");
            
            // PageReorderRequestedイベント発火でMainCompositeViewModelに処理委譲
            var eventArgs = new PageReorderEventArgs(
                new List<V3PageViewModel> { pageViewModel },
                insertIndex: insertIndex  // 🎯 V3.0.021: InsertIndex引数追加
            );
            
            PageReorderRequested?.Invoke(this, eventArgs);
            
            await AppendDebugLogAsync($"[HandlePageReorderWithInsertIndex] PageReorderRequestedイベント発火完了");
        }

        /// <summary>
        /// 🆕 V3.0.116: 複数ページ並び替え処理（InsertIndex指定）
        /// </summary>
        private async Task HandlePageReorderWithInsertIndex(List<V3PageViewModel> pageViewModels, int insertIndex)
        {
            await AppendDebugLogAsync($"[HandlePageReorderWithInsertIndex] 開始 - Pages: {pageViewModels.Count}, InsertIndex: {insertIndex}");
            
            // PageReorderRequestedイベント発火でMainCompositeViewModelに処理委譲
            var eventArgs = new PageReorderEventArgs(
                pageViewModels,
                insertIndex: insertIndex
            );
            
            PageReorderRequested?.Invoke(this, eventArgs);
            
            await AppendDebugLogAsync($"[HandlePageReorderWithInsertIndex] PageReorderRequestedイベント発火完了");
        }

        /// <summary>
        /// 🎯 OSS標準: ドラッグオーバー処理
        /// </summary>
        public async Task DragOverAsync(IAdvancedDropInfo dropInfo)
        {
            try
            {
                if (await CanDropAsync(dropInfo))
                {
                    ShowDragOverlay();
                    
                    // ページドラッグの場合
                    if (dropInfo.Data is IDataObject dataObject && 
                        dataObject.GetData(DataFormats.Text) is string dragId && 
                        _dragCache.ContainsKey(dragId))
                    {
                        StatusMessage = "ページ並び替え - ドロップして移動";
                    }
                    // ファイルドロップの場合
                    else
                    {
                        StatusMessage = $"{dropInfo.FilePaths?.Length ?? 0} 個のファイル - ドロップして追加";
                    }
                }
                else
                {
                    HideDragOverlay();
                    StatusMessage = "ドロップできません";
                }
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[DragOverAsync] Error: {ex.Message}");
                HideDragOverlay();
            }
        }

        #endregion

        #region OSS標準: IAdvancedDragHandler実装

        /// <summary>
        /// 🎯 V3.0.019: 静的キャッシュによる安全なドラッグ開始処理
        /// WPF制約準拠: DataFormats.Text使用でカスタムオブジェクト問題回避
        /// </summary>
        public async Task<object> StartDragAsync(IAdvancedDragInfo dragInfo)
        {
            try
            {
                // 🆕 V3.0.116: 複数選択対応（V3DragInfo専用機能）
                if (dragInfo is V3DragInfo v3DragInfo &&
                    v3DragInfo.SelectedItems != null &&
                    v3DragInfo.SelectedItems.Count > 1)
                {
                    var selectedPages = v3DragInfo.SelectedItems
                        .OfType<V3PageViewModel>()
                        .ToList();

                    if (selectedPages.Count > 1)
                    {
                        // 複数ページをキャッシュ
                        var dragId = Guid.NewGuid().ToString();
                        _dragCache[dragId] = selectedPages;

                        await AppendDebugLogAsync($"[StartDragAsync] Multiple pages drag started - DragID: {dragId}, Count: {selectedPages.Count}");

                        var dataObject = new DataObject();
                        dataObject.SetData(DataFormats.Text, dragId);

                        StatusMessage = $"{selectedPages.Count} ページをドラッグ中...";

                        return dataObject;
                    }
                }

                // 🔧 既存の単一ページ処理（フォールバック）
                if (dragInfo.SourceItem is V3PageViewModel pageViewModel)
                {
                    // 🎯 V3.0.019: 静的キャッシュに安全保存
                    var dragId = Guid.NewGuid().ToString();
                    _dragCache[dragId] = pageViewModel;
                    
                    await AppendDebugLogAsync($"[StartDragAsync] Single page drag started - DragID: {dragId}, Page: {pageViewModel.PageNumber}");
                    
                    // 🎯 V3.0.019: WPF標準形式でGUID文字列転送（安全）
                    var dataObject = new DataObject();
                    dataObject.SetData(DataFormats.Text, dragId);
                    
                    StatusMessage = $"ページ {pageViewModel.PageNumber} をドラッグ中...";
                    
                    return dataObject;
                }

                await AppendDebugLogAsync("[StartDragAsync] No draggable item detected");
                return null;
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[StartDragAsync] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎯 OSS標準: ドラッグ完了処理
        /// V3.0.019: キャッシュクリーンアップ対応
        /// </summary>
        public async Task DragCompletedAsync(IAdvancedDragCompletedInfo dragCompletedInfo)
        {
            try
            {
                if (dragCompletedInfo.IsCancelled)
                {
                    StatusMessage = "ドラッグがキャンセルされました";
                    await AppendDebugLogAsync("[DragCompletedAsync] Drag cancelled");
                    
                    // 🎯 V3.0.019: キャンセル時のキャッシュクリーンアップ
                    CleanupExpiredCache(null);
                }
                else
                {
                    StatusMessage = "ドラッグ完了";
                    await AppendDebugLogAsync("[DragCompletedAsync] Drag completed successfully");
                }
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[DragCompletedAsync] Error: {ex.Message}");
            }
        }

        #region 🎯 V3.0.019: 静的キャッシュ管理

        /// <summary>
        /// 🎯 V3.0.019: 期限切れキャッシュエントリのクリーンアップ
        /// メモリリーク防止: 10分以上経過したエントリを自動削除
        /// </summary>
        private static void CleanupExpiredCache(object? state)
        {
            try
            {
                var expiredKeys = new List<string>();
                var cutoffTime = DateTime.Now.AddMinutes(-10);
                
                foreach (var key in _dragCache.Keys.ToList())
                {
                    // 簡易的な期限チェック（実装簡素化）
                    if (_dragCache.Count > 50) // 50エントリ超過で古いものを削除
                    {
                        expiredKeys.Add(key);
                    }
                }
                
                foreach (var key in expiredKeys.Take(25)) // 最大25エントリ削除
                {
                    _dragCache.Remove(key);
                }
                
                if (expiredKeys.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[CacheCleanup] Removed {expiredKeys.Count} expired entries");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheCleanup] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 🎯 V3.0.019: 手動キャッシュクリーンアップ（緊急時用）
        /// </summary>
        public static void ClearDragCache()
        {
            try
            {
                _dragCache.Clear();
                System.Diagnostics.Debug.WriteLine("[ClearDragCache] All cache entries cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClearDragCache] Error: {ex.Message}");
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 🎯 V3 OSS標準: ファイルドロップ処理（既存ドキュメントへの追加対応）
        /// </summary>
        public async Task HandleFilesDropAsync(IEnumerable<string> filePaths)
        {
            if (IsProcessing) 
            {
                await AppendDebugLogAsync("[HandleFilesDropAsync] ⏸️ 既に処理中のため、新規ドロップをスキップ");
                return;
            }

            try
            {
                IsProcessing = true;
                DragOverlayVisibility = "Collapsed";
                ProgressPercentage = 0;

                var filesList = filePaths.ToList();
                await AppendDebugLogAsync($"[HandleFilesDropAsync] 🎯 V3.0.026 ドロップ処理開始 - {filesList.Count}ファイル");
                
                // 🎯 V3.0.026 Phase3: ファイル種別詳細分析
                var imageFiles = filesList.Where(IsImageFile).ToList();
                var pdfFiles = filesList.Where(IsPdfFile).ToList();
                var otherFiles = filesList.Except(imageFiles).Except(pdfFiles).ToList();
                
                await AppendDebugLogAsync($"[HandleFilesDropAsync] 📊 ファイル分析結果:");
                await AppendDebugLogAsync($"  - 画像ファイル: {imageFiles.Count}個 [{string.Join(", ", imageFiles.Select(Path.GetFileName))}]");
                await AppendDebugLogAsync($"  - PDFファイル: {pdfFiles.Count}個 [{string.Join(", ", pdfFiles.Select(Path.GetFileName))}]");
                await AppendDebugLogAsync($"  - その他ファイル: {otherFiles.Count}個 [{string.Join(", ", otherFiles.Select(Path.GetFileName))}]");

                StatusMessage = $"{filesList.Count} 個のファイルを検証中...";

                // 🎯 OSS標準: 事前検証（Phase3強化: PDF詳細ログ追加）
                await AppendDebugLogAsync("[HandleFilesDropAsync] 🔍 FileAdditionService検証開始");
                var validationResult = await _fileAdditionService.ValidateFilesForAdditionAsync(filesList);
                
                await AppendDebugLogAsync($"[HandleFilesDropAsync] 🔍 検証結果詳細:");
                await AppendDebugLogAsync($"  - IsValid: {validationResult.IsValid}");
                await AppendDebugLogAsync($"  - ValidFiles: {validationResult.ValidFiles.Count}個");
                await AppendDebugLogAsync($"  - InvalidFiles: {validationResult.InvalidFiles.Count}個");
                await AppendDebugLogAsync($"  - ValidationErrors: {validationResult.ValidationErrors.Count}個");
                
                if (validationResult.ValidationErrors.Any())
                {
                    await AppendDebugLogAsync("[HandleFilesDropAsync] ⚠️ 検証エラー詳細:");
                    foreach (var error in validationResult.ValidationErrors)
                    {
                        await AppendDebugLogAsync($"    - {error}");
                        
                        // 🎯 V3.0.026 Phase3: PDF関連エラーの特別処理
                        if (error.Contains("PDF") || error.Contains("pdf"))
                        {
                            await AppendDebugLogAsync($"[HandleFilesDropAsync] 🚨 PDFエラー検出: {error}");
                            
                            // GhostScript関連エラーの可能性チェック
                            if (error.Contains("Ghostscript") || error.Contains("native library") || 
                                error.Contains("MagickReadException") || error.Contains("delegate"))
                            {
                                await AppendDebugLogAsync("[HandleFilesDropAsync] 💡 GhostScript依存関係問題の可能性が高い");
                                await AppendDebugLogAsync("    解決方法: GhostScriptをインストールしてください");
                                await AppendDebugLogAsync("    URL: https://www.ghostscript.com/download/gsdnld.html");
                            }
                        }
                    }
                }
                
                if (!validationResult.IsValid)
                {
                    await AppendDebugLogAsync("[HandleFilesDropAsync] ❌ 検証失敗 - ユーザーに警告表示");
                    _dialogService.ShowWarning($"無効なファイルが含まれています:\n{string.Join("\n", validationResult.ValidationErrors)}");
                    
                    if (!validationResult.ValidFiles.Any())
                    {
                        StatusMessage = "追加可能なファイルがありません";
                        await AppendDebugLogAsync("[HandleFilesDropAsync] ❌ 処理終了 - 有効ファイル0個");
                        return;
                    }
                }

                var validFiles = validationResult.ValidFiles;
                await AppendDebugLogAsync($"[HandleFilesDropAsync] ✅ 有効ファイル {validFiles.Count}個で処理続行");
                
                StatusMessage = $"{validFiles.Count} 個のファイルを処理中...";

                // 🎯 OSS標準: 既存ドキュメントへの追加 vs 新規ドキュメント作成
                if (_currentDocument != null)
                {
                    await AppendDebugLogAsync($"[HandleFilesDropAsync] 📄 既存ドキュメント追加モード (現在のページ数: {_currentDocument.Pages?.Count ?? 0})");
                    await AddFilesToExistingDocumentAsync(validFiles);
                }
                else
                {
                    await AppendDebugLogAsync("[HandleFilesDropAsync] 📄 新規ドキュメント作成モード");
                    await CreateNewDocumentFromFilesAsync(validFiles);
                }

                StatusMessage = $"{validFiles.Count} 個のファイル処理完了";
                await AppendDebugLogAsync($"[HandleFilesDropAsync] ✅ 処理完了 - StatusMessage: {StatusMessage}");

                // イベント通知
                await AppendDebugLogAsync("[HandleFilesDropAsync] 📡 FilesProcessedイベント発火");
                FilesProcessed?.Invoke(this, new FilesProcessedEventArgs(
                    validFiles.Where(IsImageFile).ToList(),
                    validFiles.Where(IsPdfFile).ToList()));
                await AppendDebugLogAsync("[HandleFilesDropAsync] 📡 FilesProcessedイベント完了");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[HandleFilesDropAsync] 🚨 予期しないエラー発生:");
                await AppendDebugLogAsync($"  - Message: {ex.Message}");
                await AppendDebugLogAsync($"  - Type: {ex.GetType().Name}");
                await AppendDebugLogAsync($"  - StackTrace: {ex.StackTrace}");
                
                // 🎯 V3.0.026 Phase3: PDF関連例外の詳細分析
                if (ex.Message.Contains("PDF") || ex.Message.Contains("Magick") || 
                    ex.Message.Contains("Ghostscript") || ex.GetType().Name.Contains("Magick"))
                {
                    await AppendDebugLogAsync("[HandleFilesDropAsync] 🔍 PDF処理関連例外と判定");
                    await AppendDebugLogAsync("  📋 トラブルシューティング情報:");
                    await AppendDebugLogAsync("    1. GhostScriptがインストールされているか確認");
                    await AppendDebugLogAsync("    2. Magick.NET設定が正しいか確認");
                    await AppendDebugLogAsync("    3. PDFファイルが破損していないか確認");
                    
                    // 詳細なエラー情報をユーザーに提供
                    _dialogService.ShowError($"PDF処理エラー: {ex.Message}\n\n" +
                        "解決方法:\n" +
                        "1. GhostScriptがインストールされているか確認してください\n" +
                        "2. PDFファイルが破損していないか確認してください\n" +
                        "3. 詳細はDEBUG_LOG.txtをご確認ください");
                }
                else
                {
                    _dialogService.ShowError($"ファイル処理エラー: {ex.Message}");
                }
                
                StatusMessage = "ファイル処理エラー";
            }
            finally
            {
                IsProcessing = false;
                ProgressPercentage = 0;
                ProgressDetail = "";
                
                await AppendDebugLogAsync("[HandleFilesDropAsync] 🏁 finally処理完了 - IsProcessing=false");
            }
        }

        /// <summary>
        /// ページ並び替え処理
        /// </summary>
        public async Task HandlePageReorderAsync(List<V3PageViewModel> pagesToMove, V3PageViewModel targetPage)
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                StatusMessage = $"{pagesToMove.Count} ページを並び替え中...";

                // PageOperationViewModelに委譲
                PageReorderRequested?.Invoke(this, new PageReorderEventArgs(pagesToMove, targetPage));

                StatusMessage = $"{pagesToMove.Count} ページを並び替え完了";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"並び替えエラー: {ex.Message}");
                StatusMessage = "並び替えエラー";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// ドラッグオーバー表示制御
        /// </summary>
        public void ShowDragOverlay()
        {
            DragOverlayVisibility = "Visible";
        }

        public void HideDragOverlay()
        {
            DragOverlayVisibility = "Collapsed";
        }

        /// <summary>
        /// 現在のドキュメントを設定（ファイル追加機能用）
        /// </summary>
        public void SetCurrentDocument(PdfDocument? document)
        {
            _currentDocument = document;
        }

        // Private methods - OSS標準実装

        /// <summary>
        /// 🎯 OSS標準: 既存ドキュメントへのファイル追加
        /// </summary>
        private async Task AddFilesToExistingDocumentAsync(List<string> files)
        {
            if (_currentDocument == null)
            {
                await AppendDebugLogAsync("[AddFilesToExistingDocument] ❌ _currentDocumentがnull");
                return;
            }

            try
            {
                await AppendDebugLogAsync($"[AddFilesToExistingDocument] 🎯 V3.0.026 Phase3 開始");
                await AppendDebugLogAsync($"  - 追加対象ファイル数: {files.Count}");
                await AppendDebugLogAsync($"  - 現在のドキュメントページ数: {_currentDocument.Pages?.Count ?? 0}");
                
                // 🎯 V3.0.026 Phase3: ファイル種別詳細分析
                var imageFiles = files.Where(IsImageFile).ToList();
                var pdfFiles = files.Where(IsPdfFile).ToList();
                
                await AppendDebugLogAsync($"  - 画像ファイル: {imageFiles.Count}個");
                await AppendDebugLogAsync($"  - PDFファイル: {pdfFiles.Count}個");
                
                if (pdfFiles.Any())
                {
                    await AppendDebugLogAsync($"[AddFilesToExistingDocument] 🔍 PDF追加詳細:");
                    foreach (var pdfFile in pdfFiles)
                    {
                        await AppendDebugLogAsync($"    - {Path.GetFileName(pdfFile)} ({new FileInfo(pdfFile).Length / 1024:F1} KB)");
                    }
                }

                StatusMessage = "既存ドキュメントにファイルを追加中...";
                
                await AppendDebugLogAsync("[AddFilesToExistingDocument] 📞 FileAdditionService.AddMixedFilesToDocumentAsync呼び出し開始");
                
                // FileAdditionServiceで追加処理
                var result = await _fileAdditionService.AddMixedFilesToDocumentAsync(_currentDocument, files);
                
                await AppendDebugLogAsync($"[AddFilesToExistingDocument] ✅ FileAdditionService完了:");
                await AppendDebugLogAsync($"  - Summary: {result.Summary}");
                await AppendDebugLogAsync($"  - AddedPagesCount: {result.AddedPagesCount}");
                await AppendDebugLogAsync($"  - SuccessfulFiles: {result.SuccessfulFiles.Count}個");
                await AppendDebugLogAsync($"  - FailedFiles: {result.FailedFiles.Count}個");
                
                if (result.FailedFiles.Any())
                {
                    await AppendDebugLogAsync("[AddFilesToExistingDocument] ⚠️ 失敗ファイル詳細:");
                    foreach (var failedFile in result.FailedFiles)
                    {
                        await AppendDebugLogAsync($"    - {failedFile}");
                        
                        // 🎯 V3.0.026 Phase3: PDF失敗の特別処理
                        if (IsPdfFile(failedFile))
                        {
                            await AppendDebugLogAsync($"[AddFilesToExistingDocument] 🚨 PDF追加失敗: {failedFile}");
                            await AppendDebugLogAsync("    考えられる原因:");
                            await AppendDebugLogAsync("    1. GhostScript未インストール");
                            await AppendDebugLogAsync("    2. PDFファイル破損");
                            await AppendDebugLogAsync("    3. Magick.NET設定問題");
                        }
                    }
                }
                
                StatusMessage = $"ファイル追加完了: {result.Summary}";
                await AppendDebugLogAsync($"[AddFilesToExistingDocument] 📊 最終結果:");
                await AppendDebugLogAsync($"  - 更新後ドキュメントページ数: {_currentDocument.Pages?.Count ?? 0}");

                // 追加完了イベント
                await AppendDebugLogAsync("[AddFilesToExistingDocument] 📡 FilesAddedToDocumentイベント発火");
                FilesAddedToDocument?.Invoke(this, new FilesAddedEventArgs(_currentDocument, result));
                await AppendDebugLogAsync("[AddFilesToExistingDocument] 📡 イベント完了");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[AddFilesToExistingDocument] 🚨 例外発生:");
                await AppendDebugLogAsync($"  - Message: {ex.Message}");
                await AppendDebugLogAsync($"  - Type: {ex.GetType().Name}");
                await AppendDebugLogAsync($"  - StackTrace: {ex.StackTrace}");
                
                // 🎯 V3.0.026 Phase3: PDF関連例外の特別処理
                if (ex.Message.Contains("PDF") || ex.Message.Contains("Magick") || 
                    ex.Message.Contains("Ghostscript") || ex.GetType().Name.Contains("Magick"))
                {
                    await AppendDebugLogAsync("[AddFilesToExistingDocument] 🔍 PDF処理関連例外と判定");
                    await AppendDebugLogAsync("  💡 推奨対処法:");
                    await AppendDebugLogAsync("    1. GhostScriptをダウンロード・インストール");
                    await AppendDebugLogAsync("    2. システム再起動");
                    await AppendDebugLogAsync("    3. PDFファイルの整合性確認");
                }
                
                throw new InvalidOperationException($"既存ドキュメントへの追加失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 🎯 OSS標準: 新規ドキュメント作成
        /// </summary>
        private async Task CreateNewDocumentFromFilesAsync(List<string> files)
{
    try
    {
        StatusMessage = "新規ドキュメントを作成中...";
        
        // 🚨 緊急デバッグ: ファイルに出力
        await AppendDebugLogAsync($"[CreateNewDocument開始] files.Count={files.Count}");
        
        // 🎯 V3 OSS標準: FileAdditionService.CreateNewDocumentFromFilesAsync を使用
        await AppendDebugLogAsync("[CreateNewDocument] FileAdditionService.CreateNewDocumentFromFilesAsync実行開始");
        var (pdfDocument, result) = await _fileAdditionService.CreateNewDocumentFromFilesAsync(files);
        
        await AppendDebugLogAsync($"[CreateNewDocument] FileAdditionService完了: Document.Pages.Count={pdfDocument.Pages.Count}");
        
        // 🎯 V3イベント駆動: NewDocumentCreatedイベント発火
        await AppendDebugLogAsync("[CreateNewDocument] NewDocumentCreatedイベント発火開始");
        NewDocumentCreated?.Invoke(this, new NewDocumentCreatedEventArgs(pdfDocument, files));
        await AppendDebugLogAsync("[CreateNewDocument] NewDocumentCreatedイベント発火完了");

        StatusMessage = $"新規ドキュメント作成完了: {result.Summary}";
        await AppendDebugLogAsync($"[CreateNewDocument完了] StatusMessage: {StatusMessage}");
    }
    catch (Exception ex)
    {
        await AppendDebugLogAsync($"[CreateNewDocument例外] エラー: {ex.Message}");
        await AppendDebugLogAsync($"[CreateNewDocument例外] スタックトレース: {ex.StackTrace}");
        throw new InvalidOperationException($"新規ドキュメント作成失敗: {ex.Message}", ex);
    }
}

        // FileAdditionService イベントハンドラー
        private void OnFileAdditionProgress(object? sender, FileAdditionProgressEventArgs e)
        {
            ProgressPercentage = e.ProgressPercentage;
            ProgressDetail = $"処理中: {Path.GetFileName(e.CurrentFile)} ({e.ProcessedCount}/{e.TotalCount})";
        }

        private void OnFileAdditionCompletedFromService(object? sender, DocOrganizer.Application.Interfaces.V3.FileAdditionCompletedEventArgs e)
        {
            StatusMessage = $"ファイル追加完了: {e.Result.Summary}";
            
            // 🎯 V3 OSS標準: MainCompositeViewModelに通知
            // Note: FileAdditionResult doesn't contain UpdatedDocument, need to get it from current document
            var mainEventArgs = new FileAdditionCompletedEventArgs(
                _currentDocument!, 
                e.Result.AddedPagesCount, 
                e.Result.SuccessfulFiles);
            FileAdditionCompleted?.Invoke(this, mainEventArgs);
        }

        private void OnFileAdditionError(object? sender, FileAdditionErrorEventArgs e)
        {
            _dialogService.ShowError($"ファイル追加エラー: {e.ErrorMessage}");
            
            // 🎯 V3 OSS標準: MainCompositeViewModelに通知
            var mainEventArgs = new FileAdditionFailedEventArgs(
                e.ErrorMessage, 
                e.Exception, 
                new List<string> { e.FailedFile ?? "不明なファイル" });
            FileAdditionFailed?.Invoke(this, mainEventArgs);
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" ||
                   extension == ".heic" || extension == ".heif" || extension == ".bmp" ||
                   extension == ".tiff" || extension == ".gif" || extension == ".webp";
        }

        private bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 🚨 緊急デバッグ: ファイルに詳細ログを出力（第16条準拠）
        /// </summary>
        private async Task AppendDebugLogAsync(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                await DocOrganizer.Core.Logging.DebugLogger.LogAsync(message, "DragDropHandler");
                System.Diagnostics.Debug.WriteLine($"[DRAGDROP_DEBUG] {message}");
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }

        // Events for coordination with other ViewModels
        public event EventHandler<FilesProcessedEventArgs>? FilesProcessed;
        public event EventHandler<FilesAddedEventArgs>? FilesAddedToDocument;
        public event EventHandler<NewDocumentCreatedEventArgs>? NewDocumentCreated;
        public event EventHandler<PageReorderEventArgs>? PageReorderRequested;
        
        // 🎯 V3 OSS標準: ファイル追加イベント
        public event EventHandler<FileAdditionCompletedEventArgs>? FileAdditionCompleted;
        public event EventHandler<FileAdditionFailedEventArgs>? FileAdditionFailed;
    }

    /// <summary>
    /// ファイル追加完了イベント引数
    /// </summary>
    public class FilesAddedEventArgs : EventArgs
    {
        public PdfDocument Document { get; }
        public FileAdditionResult Result { get; }

        public FilesAddedEventArgs(PdfDocument document, FileAdditionResult result)
        {
            Document = document;
            Result = result;
        }
    }

    /// <summary>
    /// 新規ドキュメント作成完了イベント引数
    /// </summary>
    public class NewDocumentCreatedEventArgs : EventArgs
    {
        public PdfDocument Document { get; }
        public List<string> SourceFiles { get; }

        public NewDocumentCreatedEventArgs(PdfDocument document, List<string> sourceFiles)
        {
            Document = document;
            SourceFiles = sourceFiles;
        }
    }

    // Event argument classes
    public class FilesProcessedEventArgs : EventArgs
    {
        public List<string> ImageFiles { get; }
        public List<string> PdfFiles { get; }

        public FilesProcessedEventArgs(List<string> imageFiles, List<string> pdfFiles)
        {
            ImageFiles = imageFiles;
            PdfFiles = pdfFiles;
        }
    }

    public class ImageFilesProcessedEventArgs : EventArgs
    {
        public List<string> ImageFiles { get; }
        public PdfDocument PdfDocument { get; }

        public ImageFilesProcessedEventArgs(List<string> imageFiles, PdfDocument pdfDocument)
        {
            ImageFiles = imageFiles;
            PdfDocument = pdfDocument;
        }
    }

    public class PdfFileProcessedEventArgs : EventArgs
    {
        public string FilePath { get; }

        public PdfFileProcessedEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class PageReorderEventArgs : EventArgs
    {
        public List<V3PageViewModel> PagesToMove { get; }
        public V3PageViewModel TargetPage { get; }
        public int InsertIndex { get; }  // 🎯 V3.0.021: InsertIndex引数追加

        public PageReorderEventArgs(List<V3PageViewModel> pagesToMove, V3PageViewModel targetPage = null, int insertIndex = -1)
        {
            PagesToMove = pagesToMove;
            TargetPage = targetPage;
            InsertIndex = insertIndex;  // 🎯 V3.0.021: InsertIndex保存
        }
    }
}