using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Infrastructure.Services;
using DocOrganizer.Infrastructure.Services.V3;
using DocOrganizer.Infrastructure.Extensions;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.UI.Services;
using DocOrganizer.UI.ViewModels;
using DocOrganizer.UI.ViewModels.V3;
using DocOrganizer.UI.Views;
using DocOrganizer.Core.Helpers;
using DocOrganizer.Core.Config;

namespace DocOrganizer.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            // pdfium.dll初期化（単一EXE対応）
            NativeDllExtractor.InitializePdfium();
            
            // OCR機能初期化
            OcrConfig.Initialize();
            
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // ⭐修正: Serilog完全除去 - .NET標準ログのみ使用
                    try
                    {
                        // .NET標準ログ設定（Serilog不使用）
                        services.AddLogging(loggingBuilder =>
                        {
                            loggingBuilder.AddDebug();
                            loggingBuilder.SetMinimumLevel(LogLevel.Warning);
                        });
                    }
                    catch (Exception logEx)
                    {
                        // ログ設定失敗時は完全無効化
                        System.Diagnostics.Debug.WriteLine($"Logging setup failed: {logEx.Message}");
                        services.AddLogging(); // 最小限のログ設定
                    }

                    // 既存サービスの登録
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<IRotationService, RotationService>();
                    services.AddSingleton<IPdfService, PdfService>();
                    services.AddSingleton<IPdfEditorService, PdfEditorService>();
                    // 🎯 V3実装: IImageProcessingService削除済み（V2依存関係排除）
                    // OCR機能の条件付き登録
                    if (OcrConfig.IsOcrEnabled)
                    {
                        services.AddSingleton<ITextOrientationService, SafeIronOcrTextOrientationService>();
                    }
                    else
                    {
                        // OCR無効時はダミーサービスを登録
                        services.AddSingleton<ITextOrientationService, NoOpTextOrientationService>();
                    }
                    
                    // 🏗️ V3.0.009 究極拡張可能アーキテクチャ統合 - 全プロバイダー自動登録
                    services.AddImageProcessingProviders(); // 🚀 統一プロバイダーアーキテクチャによる全画像処理サービス統合
                    
                    // V3.0.009 で統合された従来サービス（プロバイダー経由で自動提供）:
                    // - IImageLoaderService → プロバイダーアーキテクチャ
                    // - IThumbnailGeneratorService → プロバイダーアーキテクチャ  
                    // - IImageValidationService → プロバイダーアーキテクチャ
                    
                    // 残存する専用サービス
                    services.AddSingleton<IExifOrientationService, ExifOrientationService>();
                    services.AddSingleton<IHeicConversionService, HeicConversionService>();
                    services.AddSingleton<IFileAdditionService, FileAdditionService>();
                    
                    // 🎯 V3.0新機能: PDF出力サービス
                    services.AddSingleton<IPdfExportService, PdfExportService>();
                    
                    // アップデートサービスの登録
                    services.AddHttpClient<IUpdateService, GitHubUpdateService>();

                    // V3アーキテクチャ: 既存MainViewModelは不要（V3 MainCompositeViewModelを使用）
                    
                    // 🎯 V3 ViewModels登録
                    services.AddSingleton<DocumentManagementViewModel>();
                    services.AddSingleton<PageOperationViewModel>();
                    services.AddSingleton<PreviewManagementViewModel>();
                    services.AddSingleton<DragDropHandlerViewModel>();
                    services.AddSingleton<StatusManagementViewModel>();
                    services.AddSingleton<MainCompositeViewModel>();

                    // Viewの登録
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // DebugLoggerを使用してログ出力を統一
            
            try
            {
                // 🎯 V3起動ログ: 起動プロセス詳細記録
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("V3 Startup開始");
                
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("Host.StartAsync開始");
                await _host.StartAsync();
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("Host.StartAsync成功");

                DocOrganizer.Core.Logging.DebugLogger.LogStartup("MainWindow取得開始");
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("MainWindow取得成功");
                
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("MainCompositeViewModel取得開始");
                var v3ViewModel = _host.Services.GetRequiredService<MainCompositeViewModel>();
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("MainCompositeViewModel取得成功");
                
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("DataContext設定開始");
                mainWindow.DataContext = v3ViewModel;
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("DataContext設定成功");
                
                System.Diagnostics.Debug.WriteLine("🚀 V3 OSS標準: MainCompositeViewModel常時使用");
                
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("MainWindow.Show開始");
                mainWindow.Show();
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("MainWindow.Show成功");
                
                base.OnStartup(e);
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("V3 Startup完了");
            }
            catch (Exception ex)
            {
                var errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STARTUP ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}\n";
                DocOrganizer.Core.Logging.DebugLogger.LogError("STARTUP ERROR", ex);
                
                MessageBox.Show($"Application startup failed: {ex.Message}\n\nDetails: {ex.StackTrace}", 
                    "DocOrganizer - Startup Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                
                System.Diagnostics.Debug.WriteLine($"Application startup failed: {ex}");
                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            // pdfium.dll クリーンアップ（単一EXE対応）
            NativeDllExtractor.Cleanup();
            
            base.OnExit(e);
        }
    }
}