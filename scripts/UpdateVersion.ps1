#!/usr/bin/env pwsh
<#
.SYNOPSIS
    DocOrganizer Unified Version Update Script
    
.DESCRIPTION
    Updates version numbers across all DocOrganizer system files to prevent version inconsistencies.
    
    Update targets:
    1. CLAUDE.md - Project configuration
    2. MainWindow.xaml - UI display
    3. DocOrganizer.UI.csproj - Assembly information
    4. Version.cs - Single source of truth
    5. version_management.md - Version history
    6. AppSettings.json - Configuration file
    
.PARAMETER NewVersion
    New version number (e.g., "3.0.032")
    
.PARAMETER DryRun
    Preview changes without actually updating files
    
.PARAMETER Force
    Skip version validation checks
    
.EXAMPLE
    .\UpdateVersion.ps1 -NewVersion "3.0.032"
    Updates version to 3.0.032
    
.EXAMPLE
    .\UpdateVersion.ps1 -NewVersion "3.0.032" -DryRun
    Preview changes only
    
.NOTES
    Author: DocOrganizer Development Team
    Version: 1.1
    Created: 2025-09-04
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="Specify new version number (e.g., 3.0.032)")]
    [string]$NewVersion,
    
    [Parameter(HelpMessage="Preview only, no actual updates")]
    [switch]$DryRun,
    
    [Parameter(HelpMessage="Skip version validation")]
    [switch]$Force
)

# Set script location to project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ProjectRoot = Split-Path -Parent $ScriptDir
Set-Location $ProjectRoot

Write-Host "DocOrganizer Unified Version Update Script" -ForegroundColor Cyan
Write-Host "Current directory: $ProjectRoot" -ForegroundColor Gray
Write-Host "New version: $NewVersion" -ForegroundColor Yellow
Write-Host "Execution mode: $(if ($DryRun) { "Preview only (DRY RUN)" } else { "Actual update" })" -ForegroundColor $(if ($DryRun) { "Yellow" } else { "Green" })
Write-Host ""

#region Version Validation

function Test-VersionFormat {
    param([string]$Version)
    
    if ([string]::IsNullOrEmpty($Version)) {
        return $false
    }
    
    $parts = $Version.Split('.')
    if ($parts.Length -ne 3) {
        return $false
    }
    
    foreach ($part in $parts) {
        if (-not [int]::TryParse($part, [ref]$null)) {
            return $false
        }
    }
    
    return $true
}

if (-not $Force) {
    Write-Host "Validating version format..." -ForegroundColor Blue
    
    if (-not (Test-VersionFormat $NewVersion)) {
        Write-Error "Invalid version format: $NewVersion"
        Write-Host "Correct format: Major.Minor.Build (e.g., 3.0.032)" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Version format is valid" -ForegroundColor Green
}

#endregion

#region Target Files Definition

$VersionFiles = @(
    @{
        Path = "CLAUDE.md"
        Description = "Project configuration and quick reference"
        Patterns = @(
            @{ 
                Regex = '(?<=current_version: ")[^"]+'
                Replace = $NewVersion
                Description = "YAML current_version"
            },
            @{ 
                Regex = '(?<=- \*\*[^\*]+\*\*: )V[\d\.]+'
                Replace = "V$NewVersion"
                Description = "Main version display"
            }
        )
    },
    @{
        Path = "src\DocOrganizer.UI\Views\MainWindow.xaml"
        Description = "Main window UI title bar"
        Patterns = @(
            @{ 
                Regex = '(?<=Title=")DocOrganizer [\d\.]+'
                Replace = "DocOrganizer $NewVersion"
                Description = "Window title"
            }
        )
    },
    @{
        Path = "src\DocOrganizer.UI\DocOrganizer.UI.csproj"
        Description = ".NET Assembly information"
        Patterns = @(
            @{ 
                Regex = '(?<=<Version>)[\d\.]+(?=</Version>)'
                Replace = $NewVersion
                Description = "Version property"
            },
            @{ 
                Regex = '(?<=<AssemblyVersion>)[\d\.]+(?=</AssemblyVersion>)'
                Replace = "$NewVersion.0"
                Description = "AssemblyVersion property"
            },
            @{ 
                Regex = '(?<=<FileVersion>)[\d\.]+(?=</FileVersion>)'
                Replace = "$NewVersion.0"
                Description = "FileVersion property"
            }
        )
    },
    @{
        Path = "src\DocOrganizer.Core\Version.cs"
        Description = "Single source of truth version class"
        Patterns = @(
            @{ 
                Regex = '(?<=public const string Version = ")[\d\.]+'
                Replace = $NewVersion
                Description = "Version constant"
            }
        )
    },
    @{
        Path = "docs\rule\version_management.md"
        Description = "Version management history document"
        Patterns = @(
            @{ 
                Regex = '(?<=### [^\n]*\n- \*\*)[V]?[\d\.]+'
                Replace = "V$NewVersion"
                Description = "Current version display"
            }
        )
    },
    @{
        Path = "config\AppSettings.json"
        Description = "Application configuration file"
        Patterns = @(
            @{ 
                Regex = '(?<="Version":\s*")[\d\.]+'
                Replace = $NewVersion
                Description = "ApplicationInfo.Version"
            }
        )
    }
)

#endregion

#region Backup Creation

function Create-Backup {
    $BackupDir = "backup\version_update_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    
    if (-not $DryRun) {
        Write-Host "Creating backup..." -ForegroundColor Blue
        
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
        
        foreach ($file in $VersionFiles) {
            $sourcePath = $file.Path
            if (Test-Path $sourcePath) {
                $backupPath = Join-Path $BackupDir $file.Path
                $backupParent = Split-Path -Parent $backupPath
                
                if ($backupParent) {
                    New-Item -ItemType Directory -Path $backupParent -Force | Out-Null
                }
                
                Copy-Item -Path $sourcePath -Destination $backupPath -Force
                Write-Host "  Backed up: $sourcePath" -ForegroundColor Green
            }
        }
        
        Write-Host "Backup complete: $BackupDir" -ForegroundColor Green
        return $BackupDir
    }
    
    return $null
}

#endregion

#region Version Update Execution

function Update-VersionInFile {
    param(
        [hashtable]$FileInfo,
        [string]$NewVersion,
        [bool]$DryRun
    )
    
    $filePath = $FileInfo.Path
    $fullPath = Join-Path $PWD $filePath
    
    Write-Host "Processing: $filePath" -ForegroundColor Cyan
    Write-Host "   Description: $($FileInfo.Description)" -ForegroundColor Gray
    
    if (-not (Test-Path $fullPath)) {
        Write-Warning "  File not found: $filePath"
        return $false
    }
    
    try {
        $content = Get-Content $fullPath -Raw -Encoding UTF8
        $originalContent = $content
        $changesMade = $false
        
        foreach ($pattern in $FileInfo.Patterns) {
            $matches = [regex]::Matches($content, $pattern.Regex)
            
            if ($matches.Count -gt 0) {
                foreach ($match in $matches) {
                    $oldValue = $match.Value
                    Write-Host "      Found: '$oldValue' -> '$($pattern.Replace)'" -ForegroundColor Yellow
                    
                    if (-not $DryRun) {
                        $content = $content -replace $pattern.Regex, $pattern.Replace
                        $changesMade = $true
                    }
                }
            } else {
                Write-Warning "      Pattern not found: $($pattern.Description)"
            }
        }
        
        if ($changesMade -and -not $DryRun) {
            Set-Content -Path $fullPath -Value $content -Encoding UTF8 -NoNewline
            Write-Host "      Update complete" -ForegroundColor Green
        } elseif ($DryRun) {
            Write-Host "      [DRY-RUN] No actual update performed" -ForegroundColor Yellow
        }
        
        return $true
    }
    catch {
        Write-Error "File processing error: $filePath - $($_.Exception.Message)"
        return $false
    }
}

#endregion

#region Main Execution

Write-Host "Starting version update" -ForegroundColor Green
Write-Host ""

# Create backup
$backupPath = Create-Backup

# Execute updates
$successCount = 0
$totalCount = $VersionFiles.Count

foreach ($file in $VersionFiles) {
    if (Update-VersionInFile -FileInfo $file -NewVersion $NewVersion -DryRun $DryRun) {
        $successCount++
    }
    Write-Host ""
}

#endregion

#region Execution Report

Write-Host "Execution Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Gray
Write-Host "Target files: $totalCount" -ForegroundColor White
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $($totalCount - $successCount)" -ForegroundColor $(if ($totalCount -eq $successCount) { "Gray" } else { "Red" })
Write-Host "New version: $NewVersion" -ForegroundColor Yellow

if ($backupPath) {
    Write-Host "Backup location: $backupPath" -ForegroundColor Blue
}

if ($DryRun) {
    Write-Host ""
    Write-Host "This was a preview. To actually update, run without -DryRun." -ForegroundColor Yellow
} elseif ($successCount -eq $totalCount) {
    Write-Host ""
    Write-Host "Version update completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. git add . && git commit -m '[V$NewVersion] Version update'" -ForegroundColor White
    Write-Host "2. dotnet build --configuration Release" -ForegroundColor White
    Write-Host "3. Update release notes" -ForegroundColor White
} else {
    Write-Host ""
    Write-Warning "Some files failed to update. Please check the error messages above."
}

Write-Host "============================================" -ForegroundColor Gray

#endregion