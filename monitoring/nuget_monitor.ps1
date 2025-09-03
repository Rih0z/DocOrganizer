# nuget_monitor.ps1 - NuGetパッケージ監視システム
# DocOrganizer PDF Provider依存NuGetパッケージ継続監視
# 対象: Magick.NET-Q16-x64, SixLabors.ImageSharp, Microsoft.Extensions.DependencyInjection

param(
    [string[]]$Packages = @(
        "Magick.NET-Q16-x64", 
        "SixLabors.ImageSharp", 
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Hosting"
    ),
    [string]$OutputDir = ".",
    [switch]$Detailed = $false
)

# 監視結果格納
$global:MonitoringReport = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    System = "DocOrganizer NuGet Package Monitor v1.0"
    Packages = @()
    Summary = @{
        Total = 0
        Healthy = 0
        Warning = 0
        Critical = 0
        Error = 0
    }
    Alerts = @()
}

function Write-MonitorLog {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $icon = switch ($Level) {
        "CRITICAL" { "🚨" }
        "WARNING" { "⚠️" }
        "INFO" { "📊" }
        "SUCCESS" { "✅" }
        "ERROR" { "❌" }
        default { "📄" }
    }
    
    Write-Host "$icon [$timestamp] $Message" -ForegroundColor $(
        switch ($Level) {
            "CRITICAL" { "Red" }
            "WARNING" { "Yellow" }
            "SUCCESS" { "Green" }
            "ERROR" { "Red" }
            default { "White" }
        }
    )
}

function Get-PackageInfo {
    param([string]$PackageName)
    
    Write-MonitorLog "パッケージ情報取得中: $PackageName" "INFO"
    
    try {
        # NuGet API - パッケージバージョン情報取得
        $packageLower = $PackageName.ToLower()
        $nugetApi = "https://api.nuget.org/v3-flatcontainer/$packageLower/index.json"
        
        $response = Invoke-RestMethod -Uri $nugetApi -TimeoutSec 30 -ErrorAction Stop
        
        if (-not $response.versions -or $response.versions.Count -eq 0) {
            throw "No versions found"
        }
        
        $latestVersion = $response.versions[-1]
        $versionCount = $response.versions.Count
        
        # パッケージメタデータ取得
        $metadataApi = "https://api.nuget.org/v3-flatcontainer/$packageLower/$latestVersion/$packageLower.nuspec"
        
        try {
            $metadataResponse = Invoke-RestMethod -Uri $metadataApi -TimeoutSec 10 -ErrorAction SilentlyContinue
            $hasMetadata = $true
        } catch {
            $hasMetadata = $false
            $metadataResponse = $null
        }
        
        # 更新頻度分析
        $recentVersions = $response.versions | Select-Object -Last 5
        $updateFrequency = Get-UpdateFrequency -Versions $recentVersions -PackageName $PackageName
        
        return @{
            Name = $PackageName
            LatestVersion = $latestVersion
            VersionCount = $versionCount
            RecentVersions = $recentVersions
            UpdateFrequency = $updateFrequency
            HasMetadata = $hasMetadata
            Status = Get-PackageStatus -PackageName $PackageName -UpdateFrequency $updateFrequency -VersionCount $versionCount
            LastChecked = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            ApiResponse = @{
                Success = $true
                Url = $nugetApi
                ResponseTime = "< 30s"
            }
        }
        
    } catch {
        Write-MonitorLog "パッケージ情報取得エラー: $PackageName - $($_.Exception.Message)" "ERROR"
        
        return @{
            Name = $PackageName
            Error = $_.Exception.Message
            Status = "ERROR"
            LastChecked = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            ApiResponse = @{
                Success = $false
                Error = $_.Exception.Message
            }
        }
    }
}

function Get-UpdateFrequency {
    param([array]$Versions, [string]$PackageName)
    
    # 更新頻度の推定（簡易版 - 実際のリリース日は別API必要）
    $versionCount = $Versions.Count
    
    if ($versionCount -le 1) {
        return @{
            Status = "UNKNOWN"
            Description = "バージョン履歴不足"
            Recommendation = "継続監視"
        }
    }
    
    # Microsoft製パッケージの特別扱い
    if ($PackageName -like "Microsoft.*") {
        return @{
            Status = "STABLE"
            Description = "Microsoft公式サポート - 定期リリース"
            Recommendation = ".NET LTSサイクルに従う"
        }
    }
    
    # バージョン数に基づく簡易判定
    if ($versionCount -ge 10) {
        return @{
            Status = "ACTIVE"
            Description = "活発な開発・リリース ($versionCount versions)"
            Recommendation = "継続監視 - 安定している"
        }
    } elseif ($versionCount -ge 5) {
        return @{
            Status = "MODERATE"
            Description = "適度な開発活動 ($versionCount versions)"
            Recommendation = "定期チェック推奨"
        }
    } else {
        return @{
            Status = "SLOW"
            Description = "更新頻度低 ($versionCount versions)"
            Recommendation = "代替技術調査推奨"
        }
    }
}

function Get-PackageStatus {
    param([string]$PackageName, [hashtable]$UpdateFrequency, [int]$VersionCount)
    
    # Microsoft製パッケージは基本的にHEALTHY
    if ($PackageName -like "Microsoft.*") {
        return "HEALTHY"
    }
    
    # 更新頻度に基づく判定
    switch ($UpdateFrequency.Status) {
        "ACTIVE" { return "HEALTHY" }
        "STABLE" { return "HEALTHY" }
        "MODERATE" { return "HEALTHY" }
        "SLOW" { return "WARNING" }
        "UNKNOWN" { return "WARNING" }
        default { return "CAUTION" }
    }
}

function Check-SecurityVulnerabilities {
    param([string]$PackageName, [string]$Version)
    
    Write-MonitorLog "セキュリティ脆弱性チェック: $PackageName@$Version" "INFO"
    
    try {
        # GitHub Advisory Database API使用
        $advisoryApi = "https://api.github.com/advisories"
        $query = "ecosystem:nuget $PackageName"
        
        $response = Invoke-RestMethod -Uri $advisoryApi -Body @{
            'q' = $query
            'per_page' = 50
        } -TimeoutSec 10 -ErrorAction SilentlyContinue
        
        if ($response -and $response.items) {
            $vulnerabilities = $response.items | Where-Object { 
                $_.summary -like "*$PackageName*" -or 
                $_.description -like "*$PackageName*" 
            }
            
            return @{
                Count = $vulnerabilities.Count
                Vulnerabilities = $vulnerabilities
                LastChecked = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                Status = if ($vulnerabilities.Count -gt 0) { "CRITICAL" } else { "HEALTHY" }
            }
        } else {
            return @{
                Count = 0
                Vulnerabilities = @()
                LastChecked = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                Status = "HEALTHY"
                Note = "No vulnerabilities found or API unavailable"
            }
        }
        
    } catch {
        Write-MonitorLog "セキュリティチェックエラー: $($_.Exception.Message)" "WARNING"
        
        return @{
            Count = 0
            Error = $_.Exception.Message
            Status = "UNKNOWN"
            LastChecked = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
    }
}

function Get-PackageRecommendations {
    param([hashtable]$PackageInfo, [hashtable]$SecurityInfo)
    
    $recommendations = @()
    
    # セキュリティに基づく推奨
    if ($SecurityInfo.Status -eq "CRITICAL") {
        $recommendations += "🚨 緊急: セキュリティ脆弱性対応 - 即座にバージョン更新検討"
        $recommendations += "🔒 影響範囲調査・パッチ適用計画策定"
    }
    
    # 更新状況に基づく推奨
    switch ($PackageInfo.Status) {
        "HEALTHY" {
            $recommendations += "✅ 正常状態 - 継続監視継続"
            if ($PackageInfo.UpdateFrequency.Status -eq "ACTIVE") {
                $recommendations += "📈 活発な開発 - 新機能・改善の定期確認推奨"
            }
        }
        "WARNING" {
            $recommendations += "⚠️ 注意監視 - 更新状況・コミュニティ動向の詳細確認"
            $recommendations += "🔍 代替技術の調査・評価開始検討"
        }
        "CAUTION" {
            $recommendations += "🤔 慎重監視 - 開発状況の継続確認"
        }
        "ERROR" {
            $recommendations += "❌ エラー状態 - API接続・パッケージ状況の手動確認必要"
        }
    }
    
    # パッケージ固有の推奨
    if ($PackageInfo.Name -like "Microsoft.*") {
        $recommendations += "🏢 Microsoft公式 - .NET LTSサイクル・ロードマップ確認推奨"
    }
    
    if ($PackageInfo.Name -eq "SixLabors.ImageSharp") {
        $recommendations += "🎨 ImageSharp - ライセンス変更・商用制限の動向監視継続"
    }
    
    if ($PackageInfo.Name -like "Magick.NET*") {
        $recommendations += "🎯 Magick.NET - ImageMagick本体・セキュリティ更新の連動確認"
    }
    
    return $recommendations
}

function Generate-MonitoringReport {
    
    Write-MonitorLog "NuGetパッケージ監視開始..." "SUCCESS"
    Write-MonitorLog "対象パッケージ数: $($Packages.Count)" "INFO"
    
    foreach ($packageName in $Packages) {
        Write-MonitorLog "監視実行: $packageName" "INFO"
        
        # パッケージ情報取得
        $packageInfo = Get-PackageInfo -PackageName $packageName
        
        # セキュリティ脆弱性チェック
        if ($packageInfo.Status -ne "ERROR") {
            $securityInfo = Check-SecurityVulnerabilities -PackageName $packageName -Version $packageInfo.LatestVersion
        } else {
            $securityInfo = @{ Status = "UNKNOWN"; Count = 0 }
        }
        
        # 推奨アクション生成
        $recommendations = Get-PackageRecommendations -PackageInfo $packageInfo -SecurityInfo $securityInfo
        
        # 総合ステータス判定
        $overallStatus = Get-OverallStatus -PackageInfo $packageInfo -SecurityInfo $securityInfo
        
        # レポート項目追加
        $packageReport = @{
            Package = $packageInfo
            Security = $securityInfo
            OverallStatus = $overallStatus
            Recommendations = $recommendations
            MonitoredAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
        
        $global:MonitoringReport.Packages += $packageReport
        
        # サマリー更新
        $global:MonitoringReport.Summary.Total++
        switch ($overallStatus) {
            "HEALTHY" { $global:MonitoringReport.Summary.Healthy++ }
            "WARNING" { $global:MonitoringReport.Summary.Warning++ }
            "CRITICAL" { 
                $global:MonitoringReport.Summary.Critical++
                $global:MonitoringReport.Alerts += "🚨 CRITICAL: $packageName - セキュリティまたは重要問題"
            }
            "ERROR" { 
                $global:MonitoringReport.Summary.Error++
                $global:MonitoringReport.Alerts += "❌ ERROR: $packageName - 監視システムエラー"
            }
        }
        
        Write-MonitorLog "$packageName 監視完了: $overallStatus" $(
            switch ($overallStatus) {
                "HEALTHY" { "SUCCESS" }
                "WARNING" { "WARNING" }
                "CRITICAL" { "CRITICAL" }
                "ERROR" { "ERROR" }
            }
        )
    }
}

function Get-OverallStatus {
    param([hashtable]$PackageInfo, [hashtable]$SecurityInfo)
    
    # セキュリティが最優先
    if ($SecurityInfo.Status -eq "CRITICAL") {
        return "CRITICAL"
    }
    
    # パッケージ情報エラー
    if ($PackageInfo.Status -eq "ERROR") {
        return "ERROR"
    }
    
    # パッケージ状況に基づく判定
    switch ($PackageInfo.Status) {
        "HEALTHY" { return "HEALTHY" }
        "WARNING" { return "WARNING" }
        default { return "CAUTION" }
    }
}

function Save-Report {
    param([string]$OutputDirectory)
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $jsonFile = Join-Path $OutputDirectory "nuget_monitoring_report_$timestamp.json"
    $textFile = Join-Path $OutputDirectory "nuget_monitoring_summary_$timestamp.txt"
    
    try {
        # JSON詳細レポート保存
        $global:MonitoringReport | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonFile -Encoding UTF8
        Write-MonitorLog "詳細レポート保存: $jsonFile" "SUCCESS"
        
        # テキストサマリー保存
        $summary = @"
=======================================================
DocOrganizer NuGetパッケージ監視レポート
=======================================================
監視日時: $($global:MonitoringReport.Timestamp)
対象パッケージ: $($global:MonitoringReport.Summary.Total) packages

📊 ステータスサマリー:
🟢 健全: $($global:MonitoringReport.Summary.Healthy) packages
🟡 注意: $($global:MonitoringReport.Summary.Warning) packages
🔴 危険: $($global:MonitoringReport.Summary.Critical) packages
❌ エラー: $($global:MonitoringReport.Summary.Error) packages

🚨 アラート:
$(if ($global:MonitoringReport.Alerts.Count -gt 0) { $global:MonitoringReport.Alerts -join "`n" } else { "なし" })

📋 パッケージ詳細:
"@
        
        foreach ($pkg in $global:MonitoringReport.Packages) {
            $statusIcon = switch ($pkg.OverallStatus) {
                "HEALTHY" { "🟢" }
                "WARNING" { "🟡" }
                "CRITICAL" { "🔴" }
                "ERROR" { "❌" }
                default { "❓" }
            }
            
            $summary += "`n$statusIcon $($pkg.Package.Name): $($pkg.OverallStatus)"
            if ($pkg.Package.LatestVersion) {
                $summary += "`n   最新版: $($pkg.Package.LatestVersion) ($($pkg.Package.VersionCount) total versions)"
            }
            if ($pkg.Security.Count -gt 0) {
                $summary += "`n   🔒 セキュリティアラート: $($pkg.Security.Count) 件"
            }
            if ($pkg.Recommendations.Count -gt 0) {
                $summary += "`n   💡 推奨: $($pkg.Recommendations[0])"
            }
            $summary += "`n"
        }
        
        $summary | Out-File -FilePath $textFile -Encoding UTF8
        Write-MonitorLog "サマリーレポート保存: $textFile" "SUCCESS"
        
        return @{
            JsonReport = $jsonFile
            TextReport = $textFile
        }
        
    } catch {
        Write-MonitorLog "レポート保存エラー: $($_.Exception.Message)" "ERROR"
        return $null
    }
}

function Show-Summary {
    
    Write-Host "`n" + ("=" * 60) -ForegroundColor Blue
    Write-Host "🎯 DocOrganizer NuGetパッケージ監視サマリー" -ForegroundColor Blue
    Write-Host ("=" * 60) -ForegroundColor Blue
    Write-Host "📅 監視実施: $($global:MonitoringReport.Timestamp)"
    Write-Host "📊 対象: $($global:MonitoringReport.Summary.Total) packages"
    Write-Host ""
    
    # 総合状況
    Write-Host "🟢 健全: $($global:MonitoringReport.Summary.Healthy) packages" -ForegroundColor Green
    Write-Host "🟡 注意: $($global:MonitoringReport.Summary.Warning) packages" -ForegroundColor Yellow
    Write-Host "🔴 危険: $($global:MonitoringReport.Summary.Critical) packages" -ForegroundColor Red
    Write-Host "❌ エラー: $($global:MonitoringReport.Summary.Error) packages" -ForegroundColor Red
    Write-Host ""
    
    # アラート表示
    if ($global:MonitoringReport.Alerts.Count -gt 0) {
        Write-Host "🚨 緊急アラート:" -ForegroundColor Red
        foreach ($alert in $global:MonitoringReport.Alerts) {
            Write-Host "  $alert" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    # パッケージ別詳細
    foreach ($pkg in $global:MonitoringReport.Packages) {
        $statusColor = switch ($pkg.OverallStatus) {
            "HEALTHY" { "Green" }
            "WARNING" { "Yellow" }
            "CRITICAL" { "Red" }
            "ERROR" { "Red" }
            default { "Gray" }
        }
        
        $statusIcon = switch ($pkg.OverallStatus) {
            "HEALTHY" { "🟢" }
            "WARNING" { "🟡" }
            "CRITICAL" { "🔴" }
            "ERROR" { "❌" }
            default { "❓" }
        }
        
        Write-Host "$statusIcon $($pkg.Package.Name): $($pkg.OverallStatus)" -ForegroundColor $statusColor
        
        if (-not $pkg.Package.Error) {
            Write-Host "   📦 最新版: $($pkg.Package.LatestVersion) ($($pkg.Package.VersionCount) versions)" -ForegroundColor Gray
            Write-Host "   📈 更新状況: $($pkg.Package.UpdateFrequency.Description)" -ForegroundColor Gray
        } else {
            Write-Host "   ❌ エラー: $($pkg.Package.Error)" -ForegroundColor Red
        }
        
        if ($pkg.Security.Count -gt 0) {
            Write-Host "   🔒 セキュリティ: $($pkg.Security.Count) alerts" -ForegroundColor Red
        }
        
        if ($pkg.Recommendations.Count -gt 0) {
            Write-Host "   💡 推奨: $($pkg.Recommendations[0])" -ForegroundColor Cyan
        }
        
        Write-Host ""
    }
}

# メイン実行
function Main {
    
    Write-MonitorLog "DocOrganizer NuGetパッケージ監視システム開始" "SUCCESS"
    Write-MonitorLog "対象: Magick.NET, ImageSharp, Microsoft.Extensions.*" "INFO"
    
    try {
        # 監視実行
        Generate-MonitoringReport
        
        # 結果表示
        Show-Summary
        
        # レポート保存
        $reportFiles = Save-Report -OutputDirectory $OutputDir
        
        if ($reportFiles) {
            Write-MonitorLog "監視完了 - レポートファイル:" "SUCCESS"
            Write-MonitorLog "  JSON: $($reportFiles.JsonReport)" "INFO"
            Write-MonitorLog "  TEXT: $($reportFiles.TextReport)" "INFO"
        }
        
        # 終了コード判定
        if ($global:MonitoringReport.Summary.Critical -gt 0) {
            Write-MonitorLog "CRITICAL状態のパッケージがあります - 緊急対応必要" "CRITICAL"
            return 2
        } elseif ($global:MonitoringReport.Summary.Warning -gt 0) {
            Write-MonitorLog "WARNING状態のパッケージがあります - 注意監視継続" "WARNING"
            return 1
        } else {
            Write-MonitorLog "すべてのパッケージが正常状態です" "SUCCESS"
            return 0
        }
        
    } catch {
        Write-MonitorLog "監視システム実行エラー: $($_.Exception.Message)" "ERROR"
        return 3
    }
}

# 実行
$exitCode = Main
exit $exitCode