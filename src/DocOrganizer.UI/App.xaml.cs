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
                    services.AddSingleton<IImageProcessingService, ImageProcessingService>();
                    services.AddSingleton<ITextOrientationService, SafeIronOcrTextOrientationService>();
                    
                    // 🎯 V3 OSS標準サービス登録
                    services.AddSingleton<IImageLoaderService, ImageLoaderService>();
                    services.AddSingleton<IThumbnailGeneratorService, ThumbnailGeneratorService>();
                    services.AddSingleton<IExifOrientationService, ExifOrientationService>();
                    services.AddSingleton<IHeicConversionService, HeicConversionService>();
                    services.AddSingleton<IImageValidationService, ImageValidationService>();
                    services.AddSingleton<IFileAdditionService, FileAdditionService>();
                    
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
            try
            {
                await _host.StartAsync();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                
                // 🎯 V3 OSS標準: 常時V3使用 (環境変数依存削除)
                var v3ViewModel = _host.Services.GetRequiredService<MainCompositeViewModel>();
                mainWindow.DataContext = v3ViewModel;
                System.Diagnostics.Debug.WriteLine("🚀 V3 OSS標準: MainCompositeViewModel常時使用");
                
                mainWindow.Show();
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
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