using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Infrastructure.Services;
using DocOrganizer.UI.Services;
using DocOrganizer.UI.ViewModels;
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

                    // サービスの登録
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<IPdfService, PdfService>();
                    services.AddSingleton<IPdfEditorService, PdfEditorService>();
                    services.AddSingleton<IImageProcessingService, ImageProcessingService>();
                    
                    // アップデートサービスの登録
                    services.AddHttpClient<IUpdateService, GitHubUpdateService>();

                    // ViewModelの登録
                    services.AddSingleton<MainViewModel>();

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
                var viewModel = _host.Services.GetRequiredService<MainViewModel>();
                mainWindow.DataContext = viewModel;
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