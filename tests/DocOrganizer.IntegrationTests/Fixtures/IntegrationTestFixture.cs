using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Infrastructure.Services;
using DocOrganizer.UI.ViewModels.V3;
using WpfApplication = System.Windows.Application;

namespace DocOrganizer.IntegrationTests.Fixtures;

/// <summary>
/// WPF統合テスト用のテストフィクスチャ
/// UIスレッドコンテキストとDIコンテナを提供
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly WpfApplication? _testApplication;
    private readonly Dispatcher _dispatcher;

    public IntegrationTestFixture()
    {
        // UIスレッドコンテキストのセットアップ
        // 注意: [StaFact]属性によりSTAスレッドで実行されることが保証される
        // Dispatcher.CurrentDispatcherはSTAスレッドのDispatcherを取得
        _dispatcher = Dispatcher.CurrentDispatcher;

        // Application.Currentのモック化（必要に応じて）
        if (WpfApplication.Current == null)
        {
            _testApplication = new WpfApplication();
        }

        var services = new ServiceCollection();

        // ロギングを追加
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // 実際のサービスを登録（統合テストでは実サービス使用が原則）
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        services.AddSingleton<IPdfEditorService, PdfEditorService>();

        // ViewModelを登録（UIスレッドで生成）
        // 注意: ViewModelの実際の依存関係を確認後、必要に応じて追加サービスを登録
        // services.AddTransient<MainCompositeViewModel>();
        // services.AddTransient<PageOperationViewModel>();
        // services.AddTransient<DocumentManagementViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// UIスレッドでサービスを取得
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        if (_dispatcher.CheckAccess())
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        else
        {
            return _dispatcher.Invoke(() => _serviceProvider.GetRequiredService<T>());
        }
    }

    /// <summary>
    /// UIスレッドで非同期アクションを実行（戻り値あり）
    /// </summary>
    public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        if (_dispatcher.CheckAccess())
        {
            return await action();
        }
        else
        {
            return await _dispatcher.InvokeAsync(action).Task.Unwrap();
        }
    }

    /// <summary>
    /// UIスレッドで非同期アクションを実行（戻り値なし）
    /// </summary>
    public async Task InvokeAsync(Func<Task> action)
    {
        if (_dispatcher.CheckAccess())
        {
            await action();
        }
        else
        {
            await _dispatcher.InvokeAsync(action).Task;
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _testApplication?.Shutdown();
    }
}
