using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🏗️ 企業レベルプロバイダー管理システム - 動的拡張アーキテクチャ
    /// 責務: 形式別プロバイダーの最適選択・管理・パフォーマンス監視
    /// 設計: Manager Pattern + Strategy Pattern による拡張可能システム
    /// 特徴: 優先度ベース選択、パフォーマンス測定、詳細ログ、障害回復
    /// 参考: .NET Service Provider, OSS Plugin Manager, Enterprise Service Bus
    /// </summary>
    public class ImageValidationProviderManager : IImageValidationProviderManager
    {
        private readonly Dictionary<string, IImageValidationProvider> _providersByExtension;
        private readonly List<IImageValidationProvider> _allProviders;
        private readonly ILogger<ImageValidationProviderManager> _logger;

        /// <summary>
        /// コンストラクタ - DI自動プロバイダー登録
        /// </summary>
        /// <param name="providers">DI注入されたプロバイダーコレクション</param>
        /// <param name="logger">ロガー</param>
        public ImageValidationProviderManager(
            IEnumerable<IImageValidationProvider> providers,
            ILogger<ImageValidationProviderManager> logger)
        {
            _logger = logger;
            _allProviders = new List<IImageValidationProvider>();
            _providersByExtension = new Dictionary<string, IImageValidationProvider>(StringComparer.OrdinalIgnoreCase);

            // 初期プロバイダー自動登録
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }

            LogProviderRegistrationSummary();
        }

        /// <summary>
        /// プロバイダー登録（優先度ベース）
        /// </summary>
        /// <param name="provider">プロバイダーインスタンス</param>
        public void RegisterProvider(IImageValidationProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _allProviders.Add(provider);

            foreach (var extension in provider.SupportedExtensions)
            {
                // 🎯 優先度ベース置換ロジック
                if (!_providersByExtension.ContainsKey(extension) ||
                    _providersByExtension[extension].Priority < provider.Priority)
                {
                    var previousProvider = _providersByExtension.ContainsKey(extension) 
                        ? _providersByExtension[extension].ProviderName 
                        : "なし";

                    _providersByExtension[extension] = provider;

                    _logger.LogDebug("[V3_ProviderManager] プロバイダー登録: {Extension} → {NewProvider} (優先度: {Priority}, 前: {PreviousProvider})",
                        extension, provider.ProviderName, provider.Priority, previousProvider);
                }
                else
                {
                    _logger.LogDebug("[V3_ProviderManager] プロバイダー登録スキップ: {Extension}, 既存{ExistingProvider}(優先度:{ExistingPriority}) > 新規{NewProvider}(優先度:{NewPriority})",
                        extension, _providersByExtension[extension].ProviderName, _providersByExtension[extension].Priority,
                        provider.ProviderName, provider.Priority);
                }
            }
        }

        /// <summary>
        /// 最適プロバイダー取得
        /// </summary>
        /// <param name="extension">ファイル拡張子</param>
        /// <returns>最適プロバイダー</returns>
        public IImageValidationProvider GetProvider(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentNullException(nameof(extension));
            }

            var normalizedExtension = extension.StartsWith(".") ? extension : $".{extension}";

            if (_providersByExtension.TryGetValue(normalizedExtension, out var provider))
            {
                _logger.LogDebug("[V3_ProviderManager] プロバイダー取得成功: {Extension} → {Provider} (優先度: {Priority})",
                    normalizedExtension, provider.ProviderName, provider.Priority);
                return provider;
            }

            _logger.LogWarning("[V3_ProviderManager] 未サポート形式: {Extension}, 利用可能: [{AvailableExtensions}]",
                normalizedExtension, string.Join(", ", _providersByExtension.Keys));

            throw new NotSupportedException($"未サポート画像形式: {normalizedExtension}. サポート形式: {string.Join(", ", _providersByExtension.Keys)}");
        }

        /// <summary>
        /// 全プロバイダー取得
        /// </summary>
        /// <returns>全プロバイダー配列</returns>
        public IImageValidationProvider[] GetAllProviders()
        {
            return _allProviders.ToArray();
        }

        /// <summary>
        /// 最適プロバイダーによる検証実行（パフォーマンス監視付き）
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>検証結果</returns>
        public async Task<ImageValidationResult> ValidateWithBestProvider(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("[V3_ProviderManager] ファイル未存在: {FilePath}", filePath);
                return CreateFileNotFoundResult(filePath);
            }

            try
            {
                var extension = Path.GetExtension(filePath);
                var provider = GetProvider(extension);

                _logger.LogDebug("[V3_ProviderManager] 最適プロバイダー選択完了: {Extension} → {Provider}, ファイル: {FileName}",
                    extension, provider.ProviderName, Path.GetFileName(filePath));

                // 🎯 パフォーマンス監視付き実行
                var stopwatch = Stopwatch.StartNew();
                var result = await provider.ValidateAsync(filePath);
                stopwatch.Stop();

                // パフォーマンス情報ログ
                _logger.LogDebug("[V3_ProviderManager] 検証完了: {IsValid}, 処理時間: {ElapsedMs}ms, プロバイダー: {Provider}, ファイル: {FileName}",
                    result.IsValid, stopwatch.ElapsedMilliseconds, provider.ProviderName, Path.GetFileName(filePath));

                // パフォーマンス警告
                if (stopwatch.ElapsedMilliseconds > 5000) // 5秒超過
                {
                    _logger.LogWarning("[V3_ProviderManager] 検証処理が遅延: {ElapsedMs}ms, プロバイダー: {Provider}, ファイル: {FileName}",
                        stopwatch.ElapsedMilliseconds, provider.ProviderName, Path.GetFileName(filePath));
                }

                return result;
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning("[V3_ProviderManager] 未サポート形式エラー: {FilePath}, エラー: {Message}", filePath, ex.Message);
                return CreateUnsupportedFormatResult(filePath, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_ProviderManager] 検証処理エラー: {FilePath}", filePath);
                return CreateValidationErrorResult(filePath, ex.Message);
            }
        }

        #region ログ・統計

        /// <summary>
        /// プロバイダー登録サマリーログ
        /// </summary>
        private void LogProviderRegistrationSummary()
        {
            _logger.LogInformation("[V3_ProviderManager] プロバイダー登録完了: {TotalProviders}プロバイダー, {SupportedExtensions}形式対応",
                _allProviders.Count, _providersByExtension.Count);

            // 詳細プロバイダー情報
            foreach (var group in _providersByExtension.GroupBy(p => p.Value.ProviderName))
            {
                var extensions = string.Join(", ", group.Select(g => g.Key));
                var priority = group.First().Value.Priority;
                _logger.LogInformation("[V3_ProviderManager] {Provider} (優先度:{Priority}): {Extensions}",
                    group.Key, priority, extensions);
            }

            // 重複チェック
            var duplicateExtensions = _providersByExtension
                .GroupBy(p => p.Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateExtensions)
            {
                _logger.LogDebug("[V3_ProviderManager] 形式{Extension}は複数プロバイダー候補あり（優先度最高を選択済み）", duplicate);
            }
        }

        /// <summary>
        /// 診断情報取得
        /// </summary>
        public string GetDiagnosticInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== ImageValidation Provider Manager 診断情報 ===");
            info.AppendLine($"総プロバイダー数: {_allProviders.Count}");
            info.AppendLine($"サポート拡張子数: {_providersByExtension.Count}");
            info.AppendLine();

            info.AppendLine("登録プロバイダー詳細:");
            foreach (var provider in _allProviders.OrderByDescending(p => p.Priority))
            {
                info.AppendLine($"  - {provider.ProviderName} (優先度: {provider.Priority})");
                info.AppendLine($"    対応形式: {string.Join(", ", provider.SupportedExtensions)}");
            }

            info.AppendLine();
            info.AppendLine("拡張子→プロバイダーマッピング:");
            foreach (var mapping in _providersByExtension.OrderBy(m => m.Key))
            {
                info.AppendLine($"  {mapping.Key} → {mapping.Value.ProviderName}");
            }

            return info.ToString();
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// ファイル未存在結果作成
        /// </summary>
        private static ImageValidationResult CreateFileNotFoundResult(string filePath)
        {
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: false,
                FileSize: 0,
                Format: "Unknown",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "ファイルが存在しません" },
                ErrorMessage: "File not found"
            );
        }

        /// <summary>
        /// 未サポート形式結果作成
        /// </summary>
        private static ImageValidationResult CreateUnsupportedFormatResult(string filePath, string errorMessage)
        {
            var extension = Path.GetExtension(filePath);
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: false,
                FileSize: 0,
                Format: extension?.TrimStart('.').ToUpperInvariant() ?? "Unknown",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "サポートされていない画像形式です" },
                ErrorMessage: errorMessage
            );
        }

        /// <summary>
        /// 検証エラー結果作成
        /// </summary>
        private static ImageValidationResult CreateValidationErrorResult(string filePath, string errorMessage)
        {
            var extension = Path.GetExtension(filePath);
            return new ImageValidationResult(
                FilePath: filePath,
                IsValid: false,
                IsCorrupted: false,
                IsZeroBytes: false,
                FileSize: new FileInfo(filePath).Length,
                Format: extension?.TrimStart('.').ToUpperInvariant() ?? "Unknown",
                Width: 0,
                Height: 0,
                Issues: new List<string> { "検証処理でエラーが発生しました" },
                ErrorMessage: errorMessage
            );
        }

        #endregion
    }
}