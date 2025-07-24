using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
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
                    // ロギング設定（安全なディレクトリ作成）
                    var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    Directory.CreateDirectory(logDirectory);
                    
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.File(Path.Combine(logDirectory, "doc-organizer-.txt"), 
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    services.AddLogging(loggingBuilder =>
                        loggingBuilder.AddSerilog(dispose: true));

                    // サービスの登録
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<IPdfService, PdfService>();
                    services.AddSingleton<IPdfEditorService, PdfEditorService>();
                    services.AddSingleton<IImageProcessingService, ImageProcessingService>();

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
                
                Log.Fatal(ex, "Application startup failed");
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