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
using DocOrganizer.Core.Services;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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
                    
                    // 🎯 V3.0.032新機能: Undo/Redo サービス
                    services.AddSingleton<IUndoRedoService, UndoRedoService>();
                    
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
                
                // AppSettings.jsonから動的にボタンサイズ設定を読み込み
                LoadButtonSizeSettings();
                
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

        private void LoadButtonSizeSettings()
        {
            try
            {
                DocOrganizer.Core.Logging.DebugLogger.LogStartup("AppSettings.json\u304b\u3089\u30dc\u30bf\u30f3\u30b5\u30a4\u30ba\u8a2d\u5b9a\u8aad\u307f\u8fbc\u307f\u958b\u59cb");

                string jsonContent = null;

                // \u307e\u305a\u57cb\u3081\u8fbc\u307f\u30ea\u30bd\u30fc\u30b9\u304b\u3089\u8aad\u307f\u8fbc\u307f\u3092\u8a66\u307f\u308b
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "DocOrganizer.UI.AppSettings.json";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            jsonContent = reader.ReadToEnd();
                            DocOrganizer.Core.Logging.DebugLogger.LogStartup("\u57cb\u3081\u8fbc\u307f\u30ea\u30bd\u30fc\u30b9\u304b\u3089AppSettings.json\u8aad\u307f\u8fbc\u307f\u6210\u529f");
                        }
                    }
                }

                // \u57cb\u3081\u8fbc\u307f\u30ea\u30bd\u30fc\u30b9\u304c\u898b\u3064\u304b\u3089\u306a\u3044\u5834\u5408\u306f\u5916\u90e8\u30d5\u30a1\u30a4\u30eb\u3092\u63a2\u3059\uff08\u958b\u767a\u6642\u7528\uff09
                if (jsonContent == null)
                {
                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "AppSettings.json");
                    
                    if (!File.Exists(configPath))
                    {
                        // \u4ee3\u66ff\u30d1\u30b9\u3092\u8a66\u3057\u307f\u308b
                        configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "config", "AppSettings.json");
                    }
                    
                    if (File.Exists(configPath))
                    {
                        jsonContent = File.ReadAllText(configPath);
                        DocOrganizer.Core.Logging.DebugLogger.LogStartup("\u5916\u90e8\u30d5\u30a1\u30a4\u30eb\u304b\u3089AppSettings.json\u8aad\u307f\u8fbc\u307f\u6210\u529f");
                    }
                }

                if (jsonContent == null)
                {
                    DocOrganizer.Core.Logging.DebugLogger.LogStartup("AppSettings.json\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093 - \u30c7\u30d5\u30a9\u30eb\u30c8\u5024\u4f7f\u7528");
                    return;
                }

                // JSON\u30d1\u30fc\u30b9\u51e6\u7406
                using (var document = System.Text.Json.JsonDocument.Parse(jsonContent))
                {
                    var root = document.RootElement;
                    
                    // AppSettings > UISettings > ButtonSizeSettings\u3092\u53d6\u5f97
                    if (root.TryGetProperty("AppSettings", out var appSettings) &&
                        appSettings.TryGetProperty("UISettings", out var uiSettings) &&
                        uiSettings.TryGetProperty("ButtonSizeSettings", out var buttonSettings))
                    {
                        // \u8a08\u7b97\u6e08\u307f\u5024\u3092\u53d6\u5f97
                        int buttonSize = buttonSettings.GetProperty("CalculatedButtonSize").GetInt32();
                        int buttonPadding = buttonSettings.GetProperty("CalculatedButtonPadding").GetInt32();
                        int buttonMargin = buttonSettings.GetProperty("CalculatedButtonMargin").GetInt32();
                        int toolBarHeight = buttonSettings.GetProperty("CalculatedToolBarHeight").GetInt32();
                        int iconFontSize = buttonSettings.GetProperty("CalculatedIconFontSize").GetInt32();
                        int buttonFontSize = buttonSettings.GetProperty("CalculatedButtonFontSize").GetInt32();

                        DocOrganizer.Core.Logging.DebugLogger.LogStartup($"AppSettings.json\u304b\u3089\u8a2d\u5b9a\u8aad\u307f\u8fbc\u307f\u6210\u529f: ButtonSize={buttonSize}, FontSize={buttonFontSize}");

                        // WPF\u30b9\u30bf\u30a4\u30eb\u3092\u66f4\u65b0
                        UpdateButtonStyles(buttonSize, buttonPadding, buttonMargin, toolBarHeight, iconFontSize, buttonFontSize);
                    }
                    else
                    {
                        DocOrganizer.Core.Logging.DebugLogger.LogStartup("AppSettings.json\u306bButtonSizeSettings\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093");
                    }
                }
            }
            catch (Exception ex)
            {
                DocOrganizer.Core.Logging.DebugLogger.LogError("AppSettings.json\u8aad\u307f\u8fbc\u307f\u30a8\u30e9\u30fc", ex);
                System.Diagnostics.Debug.WriteLine($"LoadButtonSizeSettings\u30a8\u30e9\u30fc: {ex.Message}");
                // \u30a8\u30e9\u30fc\u6642\u306f\u30c7\u30d5\u30a9\u30eb\u30c8\u5024\u3092\u4f7f\u7528
            }
        }

        private void UpdateButtonStyles(int buttonSize, int buttonPadding, int buttonMargin, int toolBarHeight, int iconFontSize, int buttonFontSize)
        {
            try
            {
                DocOrganizer.Core.Logging.DebugLogger.LogStartup($"WPFスタイル更新開始: ButtonSize={buttonSize}");

                // スタイルが既に使用されている場合は、新しいスタイルを作成して置き換える
                
                // ToolBarButtonStyleの更新
                var toolBarButtonStyle = new Style(typeof(Button));
                toolBarButtonStyle.Setters.Add(new Setter(Button.MinWidthProperty, (double)buttonSize));
                toolBarButtonStyle.Setters.Add(new Setter(Button.MinHeightProperty, (double)buttonSize));
                toolBarButtonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(buttonPadding)));
                toolBarButtonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(buttonMargin, 0, buttonMargin, 0)));
                Resources["ToolBarButtonStyle"] = toolBarButtonStyle;

                // ToolBarButtonIconStyleの更新
                var toolBarButtonIconStyle = new Style(typeof(TextBlock));
                toolBarButtonIconStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, (double)buttonFontSize));
                toolBarButtonIconStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                toolBarButtonIconStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                Resources["ToolBarButtonIconStyle"] = toolBarButtonIconStyle;

                // ToolBarWideButtonStyleの更新
                var toolBarWideButtonStyle = new Style(typeof(Button));
                toolBarWideButtonStyle.Setters.Add(new Setter(Button.MinWidthProperty, (double)(buttonSize * 2.08))); // 75/36 比率維持
                toolBarWideButtonStyle.Setters.Add(new Setter(Button.MinHeightProperty, (double)buttonSize));
                toolBarWideButtonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(buttonPadding * 1.5, buttonPadding, buttonPadding * 1.5, buttonPadding)));
                toolBarWideButtonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(buttonMargin, 0, buttonMargin, 0)));
                Resources["ToolBarWideButtonStyle"] = toolBarWideButtonStyle;

                // MainToolBarStyleの更新
                var mainToolBarStyle = new Style(typeof(ToolBar));
                mainToolBarStyle.Setters.Add(new Setter(ToolBar.HeightProperty, (double)toolBarHeight));
                mainToolBarStyle.Setters.Add(new Setter(ToolBar.VerticalAlignmentProperty, VerticalAlignment.Center));
                mainToolBarStyle.Setters.Add(new Setter(ToolBar.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
                mainToolBarStyle.Setters.Add(new Setter(ToolBarTray.IsLockedProperty, true));
                Resources["MainToolBarStyle"] = mainToolBarStyle;

                // MenuIconStyleの更新（固定サイズ15pxを維持）
                var menuIconStyle = new Style(typeof(TextBlock));
                menuIconStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 15.0)); // 固定値15pxを使用
                menuIconStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                menuIconStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                Resources["MenuIconStyle"] = menuIconStyle;

                // ToolbarIconStyleの更新（動的サイズ）
                var toolbarIconStyle = new Style(typeof(TextBlock));
                toolbarIconStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, (double)iconFontSize)); // 動的値を使用
                toolbarIconStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                toolbarIconStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                Resources["ToolbarIconStyle"] = toolbarIconStyle;

                DocOrganizer.Core.Logging.DebugLogger.LogStartup("WPFスタイル更新完了");
            }
            catch (Exception ex)
            {
                DocOrganizer.Core.Logging.DebugLogger.LogError("WPFスタイル更新エラー", ex);
                System.Diagnostics.Debug.WriteLine($"UpdateButtonStylesエラー: {ex.Message}");
            }
        }
    }
}