# oss_monitor.py - OSS活動監視スクリプト
# DocOrganizer PDF Provider依存OSS技術監視システム
# 対象: Magick.NET, ImageSharp, Microsoft.Extensions.DependencyInjection

import requests
import json
import sys
from datetime import datetime, timedelta
from typing import Dict, List, Any, Optional

class OssMonitor:
    """OSS技術継続監視システム - GitHub活動・セキュリティ監視"""
    
    def __init__(self):
        self.repos = [
            "dlemstra/Magick.NET",
            "SixLabors/ImageSharp", 
            "dotnet/extensions"  # Microsoft.Extensions.DependencyInjection
        ]
        
        # 監視基準設定
        self.critical_thresholds = {
            'no_commits_days': 30,
            'high_unresolved_issues': 100,
            'security_vulnerabilities': 1  # 1件以上でCritical
        }
        
        self.warning_thresholds = {
            'slow_release_months': 6,
            'inactive_maintainer_days': 30,
            'performance_regression_percent': 10
        }
    
    def check_recent_activity(self, repo: str, days: int = 7) -> Dict[str, Any]:
        """最近の活動チェック - コミット・貢献者・Issue状況"""
        try:
            # GitHub API - コミット履歴取得
            api_url = f"https://api.github.com/repos/{repo}/commits"
            since = (datetime.now() - timedelta(days=days)).isoformat()
            
            response = requests.get(api_url, params={
                'since': since,
                'per_page': 100
            }, timeout=10)
            
            if response.status_code != 200:
                return {
                    'repo': repo,
                    'error': f"API Error: {response.status_code}",
                    'status': 'ERROR'
                }
            
            commits = response.json()
            
            # Issue状況チェック
            issues_url = f"https://api.github.com/repos/{repo}/issues"
            issues_response = requests.get(issues_url, params={
                'state': 'open',
                'per_page': 100
            }, timeout=10)
            
            open_issues = issues_response.json() if issues_response.status_code == 200 else []
            
            # 活動分析
            commit_count = len(commits)
            contributors = set()
            for commit in commits:
                if commit.get('author') and commit['author'].get('login'):
                    contributors.add(commit['author']['login'])
            
            # ステータス判定
            status = self._evaluate_activity_status(commit_count, len(open_issues))
            
            return {
                'repo': repo,
                'period_days': days,
                'commit_count': commit_count,
                'active_contributors': len(contributors),
                'contributors_list': list(contributors),
                'open_issues': len(open_issues),
                'last_commit': commits[0]['commit']['author']['date'] if commits else None,
                'last_commit_author': commits[0]['author']['login'] if commits and commits[0]['author'] else None,
                'status': status,
                'timestamp': datetime.now().isoformat()
            }
            
        except Exception as e:
            return {
                'repo': repo,
                'error': str(e),
                'status': 'ERROR',
                'timestamp': datetime.now().isoformat()
            }
    
    def check_security_alerts(self, repo: str) -> List[Dict[str, Any]]:
        """セキュリティアラートチェック - 脆弱性・Advisory監視"""
        try:
            # GitHub Security Advisory API使用
            api_url = f"https://api.github.com/repos/{repo}/security-advisories"
            
            response = requests.get(api_url, headers={
                'Accept': 'application/vnd.github.v3+json'
            }, timeout=10)
            
            if response.status_code == 200:
                return response.json()
            elif response.status_code == 404:
                # リポジトリにセキュリティアドバイザリがない場合
                return []
            else:
                return [{'error': f"Security API Error: {response.status_code}"}]
                
        except Exception as e:
            return [{'error': str(e)}]
    
    def check_release_info(self, repo: str) -> Dict[str, Any]:
        """リリース情報チェック - 最新版・更新頻度"""
        try:
            api_url = f"https://api.github.com/repos/{repo}/releases"
            
            response = requests.get(api_url, params={
                'per_page': 10
            }, timeout=10)
            
            if response.status_code != 200:
                return {'error': f"Releases API Error: {response.status_code}"}
            
            releases = response.json()
            
            if not releases:
                return {
                    'repo': repo,
                    'latest_release': None,
                    'release_frequency': 'No releases found',
                    'status': 'WARNING'
                }
            
            latest = releases[0]
            release_dates = [r['published_at'] for r in releases if r.get('published_at')]
            
            # リリース頻度計算
            if len(release_dates) >= 2:
                latest_date = datetime.fromisoformat(release_dates[0].replace('Z', '+00:00'))
                previous_date = datetime.fromisoformat(release_dates[1].replace('Z', '+00:00'))
                days_between = (latest_date - previous_date).days
                frequency_status = "HEALTHY" if days_between <= 90 else "WARNING"
            else:
                frequency_status = "UNKNOWN"
            
            return {
                'repo': repo,
                'latest_release': {
                    'version': latest.get('tag_name', 'Unknown'),
                    'published_at': latest.get('published_at'),
                    'prerelease': latest.get('prerelease', False)
                },
                'total_releases': len(releases),
                'frequency_status': frequency_status,
                'status': 'HEALTHY' if frequency_status == 'HEALTHY' else 'WARNING'
            }
            
        except Exception as e:
            return {'error': str(e), 'status': 'ERROR'}
    
    def _evaluate_activity_status(self, commit_count: int, open_issues: int) -> str:
        """活動ステータス評価"""
        if open_issues > self.critical_thresholds['high_unresolved_issues']:
            return 'CRITICAL'
        elif commit_count == 0:
            return 'WARNING'
        elif commit_count >= 5:
            return 'HEALTHY'
        else:
            return 'CAUTION'
    
    def _evaluate_overall_status(self, activity: Dict, security: List, releases: Dict) -> str:
        """総合ステータス評価"""
        # Critical条件チェック
        if len([s for s in security if not s.get('error')]) > 0:
            return 'CRITICAL'
        if activity.get('status') == 'CRITICAL':
            return 'CRITICAL'
        
        # Warning条件チェック
        if activity.get('status') in ['WARNING', 'ERROR']:
            return 'WARNING'
        if releases.get('status') == 'WARNING':
            return 'WARNING'
            
        # その他
        if activity.get('status') == 'HEALTHY' and releases.get('status') == 'HEALTHY':
            return 'HEALTHY'
        else:
            return 'CAUTION'
    
    def generate_report(self) -> Dict[str, Any]:
        """包括的監視レポート生成"""
        report = {
            'timestamp': datetime.now().isoformat(),
            'monitoring_system': 'DocOrganizer OSS Technology Monitor v1.0',
            'repositories': [],
            'summary': {
                'total_repos': len(self.repos),
                'healthy': 0,
                'warning': 0,
                'critical': 0,
                'error': 0
            }
        }
        
        print("🔍 OSS技術継続監視開始...")
        
        for repo in self.repos:
            print(f"  📊 {repo} 監視中...")
            
            # 各監視項目実行
            activity = self.check_recent_activity(repo, days=7)
            security = self.check_security_alerts(repo)
            releases = self.check_release_info(repo)
            
            # 総合評価
            overall_status = self._evaluate_overall_status(activity, security, releases)
            
            repo_report = {
                'name': repo,
                'activity': activity,
                'security_alerts': {
                    'count': len([s for s in security if not s.get('error')]),
                    'alerts': security
                },
                'releases': releases,
                'overall_status': overall_status,
                'recommendations': self._generate_recommendations(overall_status, activity, security, releases)
            }
            
            report['repositories'].append(repo_report)
            
            # サマリー更新
            if overall_status == 'HEALTHY':
                report['summary']['healthy'] += 1
            elif overall_status == 'WARNING':
                report['summary']['warning'] += 1
            elif overall_status == 'CRITICAL':
                report['summary']['critical'] += 1
            else:
                report['summary']['error'] += 1
        
        return report
    
    def _generate_recommendations(self, status: str, activity: Dict, security: List, releases: Dict) -> List[str]:
        """ステータスに基づく推奨アクション生成"""
        recommendations = []
        
        if status == 'CRITICAL':
            recommendations.append("🚨 緊急対応必要: 24時間以内に技術チーム招集")
            if len(security) > 0:
                recommendations.append("🔒 セキュリティ脆弱性対応: 即座にバージョン更新・パッチ適用検討")
            if activity.get('open_issues', 0) > 100:
                recommendations.append("📋 Issue増大: 代替技術検討・移行準備開始")
                
        elif status == 'WARNING':
            recommendations.append("⚠️ 注意監視: 1週間以内に詳細分析実施")
            if activity.get('commit_count', 0) == 0:
                recommendations.append("💤 活動停滞: コミュニティ状況・メンテナー連絡状況確認")
            if releases.get('status') == 'WARNING':
                recommendations.append("📦 リリース遅延: 次期バージョンロードマップ確認")
                
        elif status == 'HEALTHY':
            recommendations.append("✅ 正常状態: 継続監視継続")
            recommendations.append("📈 改善機会: 新機能・性能向上の調査・活用検討")
        
        return recommendations
    
    def save_report(self, report: Dict[str, Any], output_dir: str = ".") -> str:
        """レポートファイル保存"""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = f"{output_dir}/oss_monitoring_report_{timestamp}.json"
        
        try:
            with open(filename, 'w', encoding='utf-8') as f:
                json.dump(report, f, indent=2, ensure_ascii=False)
            
            print(f"📄 監視レポート保存完了: {filename}")
            return filename
            
        except Exception as e:
            print(f"❌ レポート保存エラー: {e}")
            return ""
    
    def print_summary(self, report: Dict[str, Any]):
        """監視結果サマリー表示"""
        print("\n" + "="*60)
        print("🎯 DocOrganizer OSS技術監視サマリー")
        print("="*60)
        print(f"📅 監視実施日時: {report['timestamp']}")
        print(f"📊 監視対象: {report['summary']['total_repos']} repositories")
        print()
        
        # 総合状況
        summary = report['summary']
        print(f"🟢 健全: {summary['healthy']} プロジェクト")
        print(f"🟡 注意: {summary['warning']} プロジェクト")  
        print(f"🔴 危険: {summary['critical']} プロジェクト")
        print(f"❌ エラー: {summary['error']} プロジェクト")
        print()
        
        # 各プロジェクト詳細
        for repo_report in report['repositories']:
            status_icon = {
                'HEALTHY': '🟢',
                'WARNING': '🟡', 
                'CRITICAL': '🔴',
                'ERROR': '❌'
            }.get(repo_report['overall_status'], '❓')
            
            print(f"{status_icon} {repo_report['name']}: {repo_report['overall_status']}")
            
            activity = repo_report['activity']
            if not activity.get('error'):
                print(f"   📈 活動: {activity['commit_count']} commits, {activity['active_contributors']} contributors")
                print(f"   🐛 Issues: {activity['open_issues']} open")
            
            security = repo_report['security_alerts']
            if security['count'] > 0:
                print(f"   🔒 セキュリティ: {security['count']} alerts")
            
            # 推奨アクション
            if repo_report['recommendations']:
                print(f"   💡 推奨: {repo_report['recommendations'][0]}")
            print()

def main():
    """メイン実行関数"""
    print("🚀 DocOrganizer OSS技術継続監視システム起動")
    print("対象: Magick.NET, ImageSharp, Microsoft.Extensions.DI")
    print()
    
    monitor = OssMonitor()
    
    try:
        # 監視実行
        report = monitor.generate_report()
        
        # 結果表示
        monitor.print_summary(report)
        
        # レポート保存
        output_dir = "."
        if len(sys.argv) > 1:
            output_dir = sys.argv[1]
        
        saved_file = monitor.save_report(report, output_dir)
        
        # アラート判定
        critical_count = report['summary']['critical']
        warning_count = report['summary']['warning']
        
        if critical_count > 0:
            print("🚨 CRITICAL ALERT: 緊急対応が必要なプロジェクトがあります!")
            print("📞 技術チームへの即座連絡・対応開始してください")
            return 2  # Critical exit code
        elif warning_count > 0:
            print("⚠️ WARNING: 注意監視が必要なプロジェクトがあります")
            print("📋 1週間以内の詳細分析・対策検討をお勧めします")
            return 1  # Warning exit code
        else:
            print("✅ すべてのOSS技術が正常状態です - 継続監視を継続してください")
            return 0  # Success
            
    except Exception as e:
        print(f"❌ 監視システム実行エラー: {e}")
        return 3  # Error exit code

if __name__ == "__main__":
    exit_code = main()
    sys.exit(exit_code)