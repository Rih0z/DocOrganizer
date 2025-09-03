using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Application.Attributes;
using DocOrganizer.Infrastructure.Services.V3;
using DocOrganizer.Infrastructure.Services.V3.Providers;

namespace DocOrganizer.Infrastructure.Extensions
{
    /// <summary>
    /// 🏗️ V3.0.009 プロバイダー自動発見・登録 - .NET標準パターン
    /// 属性ベース自動発見による無限拡張可能システム
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 究極拡張可能アーキテクチャ統合 - 全プロバイダー自動登録
        /// </summary>
        public static IServiceCollection AddImageProcessingProviders(this IServiceCollection services)
        {
            // 🎯 V3.0.009 統合ログ設定：最小限のログ（App.xaml.csで本格ログ設定済み）
            var extensionLogger = LoggerFactory.Create(builder => 
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            }).CreateLogger("ServiceCollectionExtensions");
            
            try
            {
                extensionLogger.LogInformation("[V3_DI] 🏗️ プロバイダー自動発見開始...");
                
                // 属性ベースプロバイダー自動発見
                var providerTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface)
                    .Where(t => t.GetCustomAttribute<ImageProcessingProviderAttribute>() != null)
                    .Where(t => typeof(IImageProcessingProvider).IsAssignableFrom(t))
                    .ToArray();

                extensionLogger.LogInformation("[V3_DI] 発見されたプロバイダー数: {Count}", providerTypes.Length);
                
                foreach (var providerType in providerTypes)
                {
                    var attribute = providerType.GetCustomAttribute<ImageProcessingProviderAttribute>();
                    services.AddScoped(typeof(IImageProcessingProvider), providerType);
                    
                    extensionLogger.LogDebug("[V3_DI] プロバイダー登録: {Provider} (優先度: {Priority})", 
                        attribute?.Name ?? providerType.Name, attribute?.Priority ?? 50);
                }

                // マネージャー登録
                services.AddScoped<IImageProcessingProviderManager, ImageProcessingProviderManager>();
                
                // 統合サービス登録
                services.AddScoped<IThumbnailGeneratorService>(provider => 
                    new ThumbnailGeneratorService(
                        provider.GetRequiredService<IImageProcessingProviderManager>(),
                        provider.GetRequiredService<ILogger<ThumbnailGeneratorService>>()));
                        
                services.AddScoped<IImageLoaderService>(provider => 
                    new ImageLoaderService(
                        provider.GetRequiredService<IImageProcessingProviderManager>(),
                        provider.GetRequiredService<ILogger<ImageLoaderService>>()));

                // 既存の検証サービスと統合
                services.AddScoped<IImageValidationService>(provider => 
                    new ImageValidationService(
                        provider.GetRequiredService<IImageProcessingProviderManager>(),
                        provider.GetRequiredService<ILogger<ImageValidationService>>()));

                // ✅ 新しいV3変換サービスを追加
                services.AddScoped<IDocumentToV3ConverterService, DocumentToV3ConverterService>();

                // 🎯 PDF専用レンダリングサービス（V3.0.029 - Magick.NET実装修正・DI登録不整合解消）
                services.AddScoped<IPdfRenderingService, PdfiumViewerRenderingService>();
                
                // 🎯 PDFパフォーマンス監視サービス（V3.0.025）
                services.AddScoped<PdfPerformanceMonitor>();

                extensionLogger.LogInformation("[V3_DI] ✅ 究極拡張可能アーキテクチャ統合完了!");
                
                return services;
            }
            catch (Exception ex)
            {
                extensionLogger.LogError(ex, "[V3_DI] ❌ プロバイダー登録エラー");
                throw;
            }
        }
    }
}