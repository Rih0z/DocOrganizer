using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services
{
    public class GitHubUpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubUpdateService> _logger;
        private const string GitHubOwner = "Rih0z";
        private const string GitHubRepo = "DocOrganizer";
        private const string GitHubApiUrl = "https://api.github.com";

        public string CurrentVersion { get; }

        public GitHubUpdateService(HttpClient httpClient, ILogger<GitHubUpdateService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // アプリケーションのバージョンを取得
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            CurrentVersion = $"v{version?.Major ?? 2}.{version?.Minor ?? 2}.{version?.Build ?? 0}";
            
            // GitHub APIのヘッダー設定
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocOrganizer-Updater");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                _logger.LogInformation("Checking for updates...");
                
                // GitHub Releases APIから最新リリースを取得
                var url = $"{GitHubApiUrl}/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to check for updates: {response.StatusCode}");
                    return null;
                }

                var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();
                if (release == null)
                {
                    return null;
                }

                // バージョン比較
                if (IsNewerVersion(release.TagName))
                {
                    // Windows用のアセットを探す
                    var asset = release.Assets?.FirstOrDefault(a => 
                        a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                    if (asset != null)
                    {
                        return new UpdateInfo
                        {
                            Version = release.TagName,
                            DownloadUrl = asset.BrowserDownloadUrl,
                            ReleaseNotes = release.Body ?? "新しいバージョンが利用可能です。",
                            ReleaseDate = release.PublishedAt,
                            FileSize = asset.Size,
                            IsMandatory = false
                        };
                    }
                }

                _logger.LogInformation("No updates available");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return null;
            }
        }

        public async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null)
        {
            try
            {
                _logger.LogInformation($"Downloading update {updateInfo.Version}...");
                
                // ダウンロード先のパスを作成
                var tempPath = Path.Combine(Path.GetTempPath(), "DocOrganizer_Updates");
                Directory.CreateDirectory(tempPath);
                
                var fileName = Path.GetFileName(new Uri(updateInfo.DownloadUrl).LocalPath);
                var filePath = Path.Combine(tempPath, fileName);

                // 既存のファイルがあれば削除
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // ダウンロード実行
                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1 && progress != null;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalRead = 0L;
                int read;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (canReportProgress)
                    {
                        progress!.Report((double)totalRead / totalBytes * 100);
                    }
                }

                _logger.LogInformation($"Update downloaded to: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading update");
                return null;
            }
        }

        public async Task<bool> ApplyUpdateAsync(string updateFilePath)
        {
            try
            {
                _logger.LogInformation("Applying update...");

                // 更新スクリプトを作成
                var scriptPath = Path.Combine(Path.GetTempPath(), "update_docorganizer.bat");
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                
                if (string.IsNullOrEmpty(currentExePath))
                {
                    _logger.LogError("Cannot determine current executable path");
                    return false;
                }

                var script = $@"
@echo off
echo Updating DocOrganizer...
timeout /t 3 /nobreak > nul
taskkill /F /IM DocOrganizer.exe 2> nul
timeout /t 2 /nobreak > nul
move /Y ""{updateFilePath}"" ""{currentExePath}""
start """" ""{currentExePath}""
del ""%~f0""
";

                await File.WriteAllTextAsync(scriptPath, script);

                // スクリプトを実行
                var startInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
                
                // アプリケーションを終了
                Environment.Exit(0);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying update");
                return false;
            }
        }

        private bool IsNewerVersion(string latestVersion)
        {
            try
            {
                // "v" プレフィックスを削除
                var current = CurrentVersion.TrimStart('v');
                var latest = latestVersion.TrimStart('v');

                var currentParts = current.Split('.');
                var latestParts = latest.Split('.');

                for (int i = 0; i < Math.Min(currentParts.Length, latestParts.Length); i++)
                {
                    if (int.TryParse(currentParts[i], out var currentNum) &&
                        int.TryParse(latestParts[i], out var latestNum))
                    {
                        if (latestNum > currentNum) return true;
                        if (latestNum < currentNum) return false;
                    }
                }

                return latestParts.Length > currentParts.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing versions");
                return false;
            }
        }

        // GitHub API レスポンスモデル
        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = "";
            
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";
            
            [JsonPropertyName("body")]
            public string Body { get; set; } = "";
            
            [JsonPropertyName("published_at")]
            public DateTime PublishedAt { get; set; }
            
            [JsonPropertyName("assets")]
            public GitHubAsset[]? Assets { get; set; }
        }

        private class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";
            
            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = "";
            
            [JsonPropertyName("size")]
            public long Size { get; set; }
        }
    }
}