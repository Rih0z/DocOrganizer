using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DocOrganizer.Application.Attributes;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Infrastructure.Services.V3.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🏗️ 企業レベルDI拡張 - プロバイダー自動発見・登録
    /// 責務: 属性ベースプロバイダー自動発見、依存関係自動構成
    /// 設計: .NET標準パターン + 属性ベース自動登録
    /// 参考: ASP.NET Core Service Registration, Entity Framework DbContext Registration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 画像検証プロバイダーシステム自動登録
        /// </summary>
        /// <param name="services">DIコンテナ</param>
        /// <returns>設定済みDIコンテナ</returns>
        public static IServiceCollection AddImageValidationProviders(this IServiceCollection services)
        {
            try
            {
                // 🎯 属性ベースプロバイダー自動発見
                var providerTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.GetCustomAttribute<ValidationProviderAttribute>() != null)
                    .Where(t => typeof(IImageValidationProvider).IsAssignableFrom(t))
                    .Where(t => !t.IsInterface && !t.IsAbstract)
                    .ToArray();

                if (providerTypes.Length == 0)
                {
                    // フォールバック: 手動プロバイダー登録
                    RegisterProvidersManually(services);
                }
                else
                {
                    // 属性ベース自動登録
                    RegisterProvidersAutomatically(services, providerTypes);
                }

                // 🎯 管理システム登録
                services.AddScoped<IImageValidationProviderManager, ImageValidationProviderManager>();

                // 🎯 既存サービス新アーキテクチャ対応
                services.AddScoped<IImageValidationService, EnterpriseImageValidationService>();

                // ✅ 新しいサービスを追加
                services.AddScoped<IDocumentToV3ConverterService, DocumentToV3ConverterService>();

                return services;
            }
            catch (Exception ex)
            {
                // DI登録エラー時のフォールバック
                var serviceProvider = services.BuildServiceProvider();
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("ServiceCollectionExtensions");
                logger?.LogError(ex, "[V3_DI] プロバイダー登録エラー、フォールバックモードで継続");

                // 最低限のサービス登録
                services.AddScoped<IImageValidationService, ImageValidationService>();
                return services;
            }
        }

        /// <summary>
        /// 属性ベース自動プロバイダー登録
        /// </summary>
        private static void RegisterProvidersAutomatically(IServiceCollection services, Type[] providerTypes)
        {
            foreach (var providerType in providerTypes)
            {
                var attribute = providerType.GetCustomAttribute<ValidationProviderAttribute>();
                
                // スコープドライフタイムで登録
                services.AddScoped(typeof(IImageValidationProvider), providerType);
                
                Console.WriteLine($"[V3_DI] 自動プロバイダー登録: {attribute?.Name} ({providerType.Name}), 優先度: {attribute?.Priority}");
            }
            
            Console.WriteLine($"[V3_DI] 属性ベース自動登録完了: {providerTypes.Length}プロバイダー");
        }

        /// <summary>
        /// 手動プロバイダー登録（フォールバック）
        /// </summary>
        private static void RegisterProvidersManually(IServiceCollection services)
        {
            // 手動プロバイダー登録
            services.AddScoped<IImageValidationProvider, HeicValidationProvider>();
            services.AddScoped<IImageValidationProvider, StandardImageValidationProvider>();
            services.AddScoped<IImageValidationProvider, GifValidationProvider>();
            
            Console.WriteLine("[V3_DI] 手動プロバイダー登録完了（フォールバックモード）");
        }
    }

    /// <summary>
    /// 🎯 企業レベル統合画像検証サービス - プロバイダーマネージャー統合版
    /// 責務: 既存IImageValidationServiceインターフェース互換性保持
    /// 設計: Adapter Pattern による既存インターフェース統合
    /// </summary>
    public class EnterpriseImageValidationService : IImageValidationService
    {
        private readonly IImageValidationProviderManager _providerManager;
        private readonly ILogger<EnterpriseImageValidationService> _logger;

        public EnterpriseImageValidationService(
            IImageValidationProviderManager providerManager,
            ILogger<EnterpriseImageValidationService> logger)
        {
            _providerManager = providerManager;
            _logger = logger;
        }

        /// <summary>
        /// 画像検証（プロバイダーマネージャー経由）
        /// </summary>
        public async Task<ImageValidationResult> ValidateImageAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[Enterprise_Validation] プロバイダー経由検証開始: {FileName}", System.IO.Path.GetFileName(filePath));
                
                // 🎯 プロバイダーマネージャーによる最適検証
                var result = await _providerManager.ValidateWithBestProvider(filePath);
                
                _logger.LogDebug("[Enterprise_Validation] プロバイダー経由検証完了: {IsValid}, 形式: {Format}", 
                    result.IsValid, result.Format);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Enterprise_Validation] プロバイダー経由検証エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 画像修復（フォールバック実装）
        /// </summary>
        public async Task<ImageRepairResult> RepairImageAsync(string filePath)
        {
            // 将来的にプロバイダー別修復機能を実装予定
            // 現在は基本実装を提供
            return await Task.FromResult(new ImageRepairResult(
                OriginalPath: filePath,
                RepairedPath: null,
                RepairSuccessful: false,
                RepairActions: new List<string> { "プロバイダーベース修復機能は将来実装予定" },
                ErrorMessage: "Not implemented in provider architecture"
            ));
        }

        /// <summary>
        /// 一括検証（プロバイダー並列処理）
        /// </summary>
        public async Task<ImageValidationResult[]> ValidateBatchAsync(string[] filePaths)
        {
            try
            {
                _logger.LogDebug("[Enterprise_Validation] 一括プロバイダー検証開始: {FileCount}件", filePaths.Length);
                
                var tasks = filePaths.Select(fp => _providerManager.ValidateWithBestProvider(fp));
                var results = await Task.WhenAll(tasks);
                
                var validCount = results.Count(r => r.IsValid);
                _logger.LogDebug("[Enterprise_Validation] 一括プロバイダー検証完了: {ValidCount}/{TotalCount}件が有効", 
                    validCount, filePaths.Length);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Enterprise_Validation] 一括プロバイダー検証エラー");
                throw;
            }
        }

        /// <summary>
        /// サポート形式判定（プロバイダーマネージャー経由）
        /// </summary>
        public bool IsSupportedImageFormat(string filePath)
        {
            try
            {
                var extension = System.IO.Path.GetExtension(filePath);
                var provider = _providerManager.GetProvider(extension);
                return provider != null;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Enterprise_Validation] サポート形式判定エラー: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 画像品質評価（簡易実装）
        /// </summary>
        public async Task<ImageQualityAssessment> AssessImageQualityAsync(string filePath)
        {
            // プロバイダーベース品質評価は将来実装予定
            var validation = await ValidateImageAsync(filePath);
            
            var qualityLevel = validation.IsValid 
                ? (validation.Width * validation.Height > 1000000 
                    ? ImageQualityLevel.Good 
                    : ImageQualityLevel.Fair)
                : ImageQualityLevel.Poor;

            return new ImageQualityAssessment(
                FilePath: filePath,
                QualityLevel: qualityLevel,
                Resolution: validation.Width * validation.Height,
                CompressionRatio: validation.FileSize > 0 ? (double)validation.FileSize / (validation.Width * validation.Height) : 0,
                HasArtifacts: !validation.IsValid,
                QualityIssues: validation.Issues,
                Metrics: new Dictionary<string, object>
                {
                    ["Width"] = validation.Width,
                    ["Height"] = validation.Height,
                    ["FileSize"] = validation.FileSize,
                    ["Format"] = validation.Format
                }
            );
        }

        /// <summary>
        /// 有効ファイル抽出（プロバイダー高速処理）
        /// </summary>
        public async Task<string[]> FilterValidImagesAsync(string[] filePaths)
        {
            try
            {
                var validationResults = await ValidateBatchAsync(filePaths);
                var validFiles = validationResults
                    .Where(r => r.IsValid && !r.IsZeroBytes && !r.IsCorrupted)
                    .Select(r => r.FilePath)
                    .ToArray();

                _logger.LogDebug("[Enterprise_Validation] 有効ファイル抽出完了: {ValidCount}/{TotalCount}件", 
                    validFiles.Length, filePaths.Length);

                return validFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Enterprise_Validation] 有効ファイル抽出エラー");
                throw;
            }
        }
    }
}