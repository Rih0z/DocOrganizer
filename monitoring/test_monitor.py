# test_monitor.py - Simple OSS monitoring test (English only)
import requests
import json
from datetime import datetime

def test_github_api(repo):
    """Test basic GitHub API connectivity"""
    print(f"Testing {repo}...")
    
    try:
        api_url = f"https://api.github.com/repos/{repo}"
        response = requests.get(api_url, timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            return {
                'repo': repo,
                'status': 'SUCCESS',
                'name': data.get('full_name'),
                'stars': data.get('stargazers_count', 0),
                'updated_at': data.get('updated_at'),
                'open_issues': data.get('open_issues_count', 0)
            }
        else:
            return {
                'repo': repo,
                'status': 'ERROR',
                'error': f"HTTP {response.status_code}"
            }
    except Exception as e:
        return {
            'repo': repo,
            'status': 'ERROR',
            'error': str(e)
        }

def main():
    print("DocOrganizer OSS Monitoring System - Test")
    print("=" * 50)
    
    repos = [
        "dlemstra/Magick.NET",
        "SixLabors/ImageSharp", 
        "dotnet/extensions"
    ]
    
    results = []
    
    for repo in repos:
        result = test_github_api(repo)
        results.append(result)
        
        status_icon = "OK" if result['status'] == 'SUCCESS' else "ERR"
        print(f"[{status_icon}] {repo}")
        
        if result['status'] == 'SUCCESS':
            print(f"     Stars: {result['stars']}")
            print(f"     Issues: {result['open_issues']}")
            print(f"     Updated: {result['updated_at']}")
        else:
            print(f"     Error: {result['error']}")
        print()
    
    # Save results
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    output_file = f"test_monitoring_report_{timestamp}.json"
    
    with open(output_file, 'w') as f:
        json.dump({
            'timestamp': datetime.now().isoformat(),
            'results': results
        }, f, indent=2)
    
    print(f"Results saved: {output_file}")
    
    # Summary
    success_count = sum(1 for r in results if r['status'] == 'SUCCESS')
    print(f"Success: {success_count}/{len(results)} repositories")
    
    return 0 if success_count == len(results) else 1

if __name__ == "__main__":
    import sys
    sys.exit(main())