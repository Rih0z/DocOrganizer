@echo off
echo ========================================
echo DocOrganizer Normal Build Test (Non-Single-File)
echo ========================================
echo.

REM Clear any existing diagnostic files
if exist constructor_diagnostic.txt del constructor_diagnostic.txt
if exist simple_debug_test.txt del simple_debug_test.txt
if exist debug_diagnostic.txt del debug_diagnostic.txt
if exist .logs rmdir /s /q .logs

REM Set environment variables
set DOCORGANIZER_DEBUG=true
set DOCORGANIZER_OCR_ENABLED=false

echo Environment variables:
echo DOCORGANIZER_DEBUG=%DOCORGANIZER_DEBUG%
echo DOCORGANIZER_OCR_ENABLED=%DOCORGANIZER_OCR_ENABLED%
echo.

echo Testing normal build (non-single-file)...
start /wait /b DocOrganizer.exe
timeout /t 3 /nobreak >nul 2>&1
taskkill /F /IM DocOrganizer.exe >nul 2>&1

echo.
echo === DIAGNOSTIC RESULTS ===

if exist constructor_diagnostic.txt (
    echo ✅ constructor_diagnostic.txt found - Normal build reaches App() constructor:
    type constructor_diagnostic.txt
) else (
    echo ❌ constructor_diagnostic.txt not found - Normal build also fails
)

if exist simple_debug_test.txt (
    echo ✅ simple_debug_test.txt found - Normal build reaches OnStartup():
    type simple_debug_test.txt
) else (
    echo ❌ simple_debug_test.txt not found - Normal build doesn't reach OnStartup()
)

if exist debug_diagnostic.txt (
    echo ✅ debug_diagnostic.txt found - Normal build calls DebugLogger:
    type debug_diagnostic.txt
) else (
    echo ❌ debug_diagnostic.txt not found - Normal build doesn't call DebugLogger
)

if exist .logs\ (
    echo ✅ .logs folder found:
    dir .logs\*.log 2>nul
) else (
    echo ❌ No .logs folder created
)

echo.
pause