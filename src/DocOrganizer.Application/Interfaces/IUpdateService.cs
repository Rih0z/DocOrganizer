using System;
using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// 現在のアプリケーションバージョン
        /// </summary>
        string CurrentVersion { get; }

        /// <summary>
        /// 最新バージョンをチェック
        /// </summary>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>
        /// アップデートをダウンロード
        /// </summary>
        Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null);

        /// <summary>
        /// アップデートを適用
        /// </summary>
        Task<bool> ApplyUpdateAsync(string updateFilePath);
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public long FileSize { get; set; }
        public bool IsMandatory { get; set; }
    }
}