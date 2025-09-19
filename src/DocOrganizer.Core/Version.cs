using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DocOrganizer.Core
{
    /// <summary>
    /// DocOrganizer統一バージョン管理システム
    /// 
    /// 単一ソース真実 (Single Source of Truth) の原則に基づき、
    /// バージョン情報を一箇所で管理し、全体の不整合を防止する。
    /// 
    /// 設計原則:
    /// - 単一ソース：バージョン情報の一元管理
    /// - 自動整合：ビルド時の自動更新対応  
    /// - 可読性：人間が読みやすい形式での提供
    /// - 追跡可能：ビルド情報・日時の記録
    /// </summary>
    public static class VersionInfo
    {
        #region 核心バージョン情報（単一ソース真実）
        
        /// <summary>
        /// 現在のアプリケーションバージョン
        /// 形式: Major.Minor.Build (例: 3.0.031)
        /// 
        /// 【重要】このバージョン番号が全システムの基準となる
        /// ビルド時はこの値を基に他の全ファイルが自動更新される
        /// </summary>
        public const string Version = "3.0.109";
        
        /// <summary>
        /// .NET AssemblyVersionで使用する4桁形式
        /// 形式: Major.Minor.Build.Revision (例: 3.0.031.0)
        /// </summary>
        public static string AssemblyVersion => $"{Version}.0";
        
        /// <summary>
        /// UI表示用のフル表示名
        /// 形式: DocOrganizer Major.Minor.Build (例: DocOrganizer 3.0.031)
        /// </summary>
        public static string DisplayVersion => $"DocOrganizer {Version}";
        
        /// <summary>
        /// V接頭辞付きバージョン（ドキュメント用）
        /// 形式: VMajor.Minor.Build (例: V3.0.031)
        /// </summary>
        public static string DocumentVersion => $"V{Version}";
        
        #endregion
        
        #region ビルド・実行情報
        
        /// <summary>
        /// アプリケーションのビルド日時
        /// 実行中のアセンブリファイルの作成日時を取得
        /// </summary>
        public static string BuildDate
        {
            get
            {
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    if (assembly.Location != null && File.Exists(assembly.Location))
                    {
                        return File.GetCreationTime(assembly.Location)
                            .ToString("yyyy-MM-dd HH:mm");
                    }
                }
                catch
                {
                    // ファイルアクセスエラーの場合はコンパイル時情報を使用
                }
                
                // フォールバック: コンパイル時の現在時刻
                return "2025-09-11 23:00";
            }
        }
        
        /// <summary>
        /// 完全バージョン情報（ビルド日時込み）
        /// 形式: DocOrganizer 3.0.031 (Build: 2025-09-04 22:00)
        /// </summary>
        public static string FullVersionString => 
            $"{DisplayVersion} (Build: {BuildDate})";
            
        /// <summary>
        /// システム情報を含む詳細バージョン文字列
        /// デバッグ・診断用の詳細情報
        /// </summary>
        public static string DetailedVersionInfo
        {
            get
            {
                var runtimeVersion = Environment.Version.ToString();
                var osVersion = Environment.OSVersion.ToString();
                var processorCount = Environment.ProcessorCount;
                
                return $@"{FullVersionString}
.NET Runtime: {runtimeVersion}
OS: {osVersion}
CPU Cores: {processorCount}
Working Directory: {Environment.CurrentDirectory}";
            }
        }
        
        #endregion
        
        #region バージョン比較・検証
        
        /// <summary>
        /// バージョン番号の妥当性検証
        /// Major.Minor.Build形式かを確認
        /// </summary>
        /// <param name="version">検証するバージョン文字列</param>
        /// <returns>妥当な形式の場合true</returns>
        public static bool IsValidVersionFormat(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;
                
            var parts = version.Split('.');
            if (parts.Length != 3)
                return false;
                
            return int.TryParse(parts[0], out _) &&
                   int.TryParse(parts[1], out _) &&
                   int.TryParse(parts[2], out _);
        }
        
        /// <summary>
        /// 指定されたバージョンとの比較
        /// </summary>
        /// <param name="otherVersion">比較対象バージョン</param>
        /// <returns>
        /// -1: 現在バージョンが古い
        ///  0: バージョンが同一
        ///  1: 現在バージョンが新しい
        /// </returns>
        public static int CompareTo(string otherVersion)
        {
            if (!IsValidVersionFormat(Version) || !IsValidVersionFormat(otherVersion))
                throw new ArgumentException("Invalid version format");
                
            var currentParts = Version.Split('.').Select(int.Parse).ToArray();
            var otherParts = otherVersion.Split('.').Select(int.Parse).ToArray();
            
            for (int i = 0; i < 3; i++)
            {
                if (currentParts[i] < otherParts[i])
                    return -1;
                else if (currentParts[i] > otherParts[i])
                    return 1;
            }
            
            return 0;
        }
        
        #endregion
        
        #region 更新管理・履歴
        
        /// <summary>
        /// バージョン更新時のチェンジログエントリ生成
        /// 新バージョンリリース時の標準フォーマット
        /// </summary>
        /// <param name="changes">変更内容の説明</param>
        /// <returns>CHANGELOGエントリ形式の文字列</returns>
        public static string GenerateChangelogEntry(string changes)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return $@"### {DocumentVersion} ({date})
- {changes}";
        }
        
        /// <summary>
        /// 次のパッチバージョン番号を生成
        /// 現在のバージョンの最後の桁を1増加
        /// </summary>
        /// <returns>次のパッチバージョン番号</returns>
        public static string GetNextPatchVersion()
        {
            var parts = Version.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[2], out var build))
                throw new InvalidOperationException($"Invalid version format: {Version}");
                
            return $"{parts[0]}.{parts[1]}.{build + 1:000}";
        }
        
        /// <summary>
        /// 次のマイナーバージョン番号を生成
        /// マイナーバージョンを1増加、ビルド番号は000にリセット
        /// </summary>
        /// <returns>次のマイナーバージョン番号</returns>
        public static string GetNextMinorVersion()
        {
            var parts = Version.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[1], out var minor))
                throw new InvalidOperationException($"Invalid version format: {Version}");
                
            return $"{parts[0]}.{minor + 1}.000";
        }
        
        #endregion
        
        #region システム統合
        
        /// <summary>
        /// 現在のバージョン情報をログ出力用に整形
        /// 起動時・診断時のログ出力で使用
        /// </summary>
        /// <returns>ログ出力用のバージョン情報</returns>
        public static string FormatForLogging()
        {
            return $"[{DisplayVersion}] Build: {BuildDate}";
        }
        
        /// <summary>
        /// Aboutダイアログ・ヘルプ用の表示テキスト
        /// ユーザー向け表示での使用
        /// </summary>
        /// <returns>ユーザー表示用のバージョン情報</returns>
        public static string FormatForUserDisplay()
        {
            return $@"{DisplayVersion}

ビルド日時: {BuildDate}
Copyright © 2025 DocOrganizer Team
プロフェッショナル文書整理ツール";
        }
        
        #endregion
    }
}