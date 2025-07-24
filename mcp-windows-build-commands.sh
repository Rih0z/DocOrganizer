#!/bin/bash
# Claude.md Article 12 - MCP Windows Build Execution Commands
# Complete workflow for DocOrganizer V2.2 Windows build via MCP

echo "============================================="
echo "Claude.md Article 12: MCP Windows Build Test"
echo "DocOrganizer V2.2 Complete Workflow"
echo "============================================="
echo ""

# MCP Configuration
MCP_SERVER="windows-build-server"
MCP_URL="http://100.71.150.41:8080"
AUTH_TOKEN="JIGrimGrHsJ7rTMReMZJJbPNOmkODUEd"

echo "MCP Configuration:"
echo "- Server: $MCP_SERVER"
echo "- URL: $MCP_URL"  
echo "- Project Path: C:/builds/Standard-image-repo/v2.2-doc-organizer"
echo ""

echo "=== STEP 3: CONNECTION TEST ==="
echo "Command to execute:"
echo "@$MCP_SERVER environment_info includeSystemInfo=true"
echo ""
echo "Expected Response:"
echo "- Windows 10/11 system information"
echo "- .NET 6.0+ SDK available"
echo "- PowerShell version"
echo ""

echo "=== STEP 4: GIT SYNCHRONIZATION ==="
echo "Command to execute:"
echo "@$MCP_SERVER git status"
echo "@$MCP_SERVER git pull origin main"
echo ""
echo "PowerShell Commands:"
cat << 'EOF'
cd C:\builds\Standard-image-repo\v2.2-doc-organizer
git status
git pull origin main
Write-Host "✅ Git synchronization completed"
EOF
echo ""

echo "=== STEP 5: BUILD EXECUTION ==="
echo "Commands to execute in sequence:"
echo ""

echo "5.1 Clean Solution:"
echo "@$MCP_SERVER dotnet clean --configuration Release"
echo ""

echo "5.2 Restore Dependencies:"  
echo "@$MCP_SERVER dotnet restore"
echo ""

echo "5.3 Build Solution:"
echo "@$MCP_SERVER dotnet build --configuration Release"
echo ""

echo "5.4 Run Tests (Optional):"
echo "@$MCP_SERVER dotnet test --configuration Release --verbosity minimal"
echo ""

echo "5.5 Publish Executable:"
echo "@$MCP_SERVER dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
echo ""

echo "=== STEP 6: EXE VERIFICATION ==="
echo "Command to execute:"
echo "@$MCP_SERVER [PowerShell script for EXE verification]"
echo ""
echo "PowerShell Verification Script:"
cat << 'EOF'
cd C:\builds\Standard-image-repo\v2.2-doc-organizer\release
$exeFile = Get-ChildItem -File DocOrganizer.UI.exe -ErrorAction SilentlyContinue

if ($exeFile) {
    Write-Host "✅ EXE FOUND!"
    Write-Host "===========================================" 
    Write-Host "File Name: $($exeFile.Name)"
    Write-Host "Full Path: $($exeFile.FullName)" 
    Write-Host "File Size: $([math]::Round($exeFile.Length / 1MB, 2)) MB"
    Write-Host "Creation Time: $($exeFile.CreationTime)"
    Write-Host "==========================================="
    
    # Launch test
    Write-Host "Testing EXE launch..."
    $proc = Start-Process -FilePath $exeFile.FullName -PassThru -WindowStyle Minimized
    Start-Sleep -Seconds 5
    
    if (-not $proc.HasExited) {
        Write-Host "✅ EXE launched successfully (Process ID: $($proc.Id))"
        Write-Host "✅ UI application is running"
        
        # Clean shutdown
        Stop-Process -Id $proc.Id -Force
        Write-Host "✅ Application terminated cleanly"
        
        # FINAL SUCCESS OUTPUT (Claude.md Article 12 format)
        Write-Host ""
        Write-Host "✅ DocOrganizer V2.2 完成: $($exeFile.FullName) - $([math]::Round($exeFile.Length / 1MB, 2))MB - $($exeFile.CreationTime.ToString('yyyy-MM-dd HH:mm:ss'))"
        
    } else {
        Write-Host "❌ EXE exited immediately (Exit Code: $($proc.ExitCode))"
    }
} else {
    Write-Host "❌ DocOrganizer.UI.exe not found in release directory"
    Write-Host "Available files:"
    Get-ChildItem . | ForEach-Object { Write-Host "  - $($_.Name)" }
}
EOF
echo ""

echo "=== EXPECTED FINAL OUTPUT (Claude.md Article 12 Compliant) ==="
echo "If successful, should display:"
echo ""
echo "✅ DocOrganizer V2.2 完成: C:\\builds\\Standard-image-repo\\v2.2-doc-organizer\\release\\DocOrganizer.UI.exe - 120-150MB - $(date '+%Y-%m-%d %H:%M:%S')"
echo ""

echo "=== ERROR HANDLING ==="
echo "If same error occurs 3 times consecutively:"
echo "1. Return to Step 1 (environment check)"
echo "2. Verify Windows environment setup"
echo "3. Check .NET SDK installation"
echo "4. Request manual verification if issues persist"
echo ""

echo "=== ALTERNATIVE CURL COMMANDS (if direct MCP not available) ==="
echo ""
echo "1. Environment Info:"
cat << EOF
curl -s -X POST "$MCP_URL/mcp" \\
  -H "Content-Type: application/json" \\
  -H "Authorization: Bearer $AUTH_TOKEN" \\
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "run_powershell",
      "arguments": {
        "command": "Get-ComputerInfo | Select-Object WindowsProductName, TotalPhysicalMemory; dotnet --version"
      }
    }
  }'
EOF
echo ""

echo "2. Build Commands:"
cat << EOF
curl -s -X POST "$MCP_URL/mcp" \\
  -H "Content-Type: application/json" \\
  -H "Authorization: Bearer $AUTH_TOKEN" \\
  -d '{
    "jsonrpc": "2.0", 
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "run_powershell",
      "arguments": {
        "command": "cd C:/builds/Standard-image-repo/v2.2-doc-organizer; dotnet clean --configuration Release; dotnet restore; dotnet build --configuration Release; dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
      }
    }
  }'
EOF
echo ""

echo "============================================="
echo "Ready for MCP Windows Build Execution"
echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
echo "============================================="