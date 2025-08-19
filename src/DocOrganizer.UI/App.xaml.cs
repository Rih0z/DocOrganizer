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
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.UI.Services;
using DocOrganizer.UI.ViewModels;
using DocOrganizer.UI.ViewModels.V3;
using DocOrganizer.UI.Views;

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
                    services.AddSingleton<ITextOrientationService, SafeIronOcrTextOrientationService>();
                    
                    // 🎯 V3 OSS標準サービス登録
                    services.AddSingleton<IImageLoaderService, ImageLoaderService>();
                    services.AddSingleton<IThumbnailGeneratorService, ThumbnailGeneratorService>();
                    services.AddSingleton<IExifOrientationService, ExifOrientationService>();
                    services.AddSingleton<IHeicConversionService, HeicConversionService>();
                    services.AddSingleton<IImageValidationService, ImageValidationService>();
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
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "STARTUP_LOG.txt");
            
            try
            {
                // 🎯 V3起動ログ: 起動プロセス詳細記録
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] V3 Startup開始\n");
                
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Host.StartAsync開始\n");
                await _host.StartAsync();
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Host.StartAsync成功\n");

                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow取得開始\n");
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow取得成功\n");
                
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainCompositeViewModel取得開始\n");
                var v3ViewModel = _host.Services.GetRequiredService<MainCompositeViewModel>();
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainCompositeViewModel取得成功\n");
                
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DataContext設定開始\n");
                mainWindow.DataContext = v3ViewModel;
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DataContext設定成功\n");
                
                System.Diagnostics.Debug.WriteLine("🚀 V3 OSS標準: MainCompositeViewModel常時使用");
                
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow.Show開始\n");
                mainWindow.Show();
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MainWindow.Show成功\n");
                
                base.OnStartup(e);
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] V3 Startup完了\n");
            }
            catch (Exception ex)
            {
                var errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STARTUP ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}\n";
                File.AppendAllText(logPath, errorMsg);
                
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

            base.OnExit(e);
        }
    }
}