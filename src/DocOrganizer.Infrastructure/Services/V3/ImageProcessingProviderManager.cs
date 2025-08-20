using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces.V3;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🏗️ V3.0.009 プロバイダー動的管理 - 企業レベル拡張性
    /// OSS標準Strategy Pattern + Factory Pattern実装
    /// </summary>
    public class ImageProcessingProviderManager : IImageProcessingProviderManager
    {
        private readonly Dictionary<string, IImageProcessingProvider> _providersByExtension;
        private readonly List<IImageProcessingProvider> _allProviders;
        private readonly ILogger<ImageProcessingProviderManager> _logger;

        public ImageProcessingProviderManager(
            IEnumerable<IImageProcessingProvider> providers,
            ILogger<ImageProcessingProviderManager> logger)
        {
            _logger = logger;
            _allProviders = new List<IImageProcessingProvider>();
            _providersByExtension = new Dictionary<string, IImageProcessingProvider>(StringComparer.OrdinalIgnoreCase);
            
            // 初期プロバイダー登録
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }
            
            LogRegisteredProviders();
        }
        
        public void RegisterProvider(IImageProcessingProvider provider)
        {
            _allProviders.Add(provider);
            
            foreach (var extension in provider.SupportedExtensions)
            {
                // 優先度ベースで既存プロバイダーを置換
                if (!_providersByExtension.ContainsKey(extension) || 
                    _providersByExtension[extension].Priority < provider.Priority)
                {
                    _providersByExtension[extension] = provider;
                    _logger.LogDebug("[V3_ProviderManager] プロバイダー登録: {Extension} → {Provider} (優先度: {Priority})", 
                        extension, provider.ProviderName, provider.Priority);
                }
            }
        }
        
        public IImageProcessingProvider GetProvider(string extension)
        {
            if (_providersByExtension.TryGetValue(extension, out var provider))
            {
                return provider;
            }
            
            throw new NotSupportedException($"未サポート形式: {extension}");
        }
        
        public IImageProcessingProvider[] GetAllProviders()
        {
            return _allProviders.ToArray();
        }
        
        public async Task<T> ProcessWithBestProvider<T>(string filePath, Func<IImageProcessingProvider, Task<T>> processor)
        {
            var extension = Path.GetExtension(filePath);
            var provider = GetProvider(extension);
            
            _logger.LogDebug("[V3_ProcessingManager] 最適プロバイダー選択: {Extension} → {Provider}", 
                extension, provider.ProviderName);
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await processor(provider);
            stopwatch.Stop();
            
            _logger.LogDebug("[V3_ProcessingManager] 処理完了: 処理時間: {ElapsedMs}ms, プロバイダー: {Provider}", 
                stopwatch.ElapsedMilliseconds, provider.ProviderName);
            
            return result;
        }
        
        private void LogRegisteredProviders()
        {
            _logger.LogInformation("[V3_ProviderManager] 登録プロバイダー数: {Count}", _allProviders.Count);
            
            foreach (var group in _providersByExtension.GroupBy(p => p.Value.ProviderName))
            {
                var extensions = string.Join(", ", group.Select(g => g.Key));
                _logger.LogInformation("[V3_ProviderManager] {Provider}: {Extensions}", 
                    group.Key, extensions);
            }
        }
    }
}