@echo off
echo ============================================
echo DocOrganizer - デバッグモード起動
echo ============================================
echo.

REM デバッグモード有効化
set DOCORGANIZER_DEBUG=true
set DOCORGANIZER_LOG_PATH=.logs

echo [設定]
echo DOCORGANIZER_DEBUG = %DOCORGANIZER_DEBUG%
echo DOCORGANIZER_LOG_PATH = %DOCORGANIZER_LOG_PATH%
echo.

echo [起動中...]
start "" "%~dp0DocOrganizer.exe"
echo.

echo デバッグログ出力先: %~dp0.logs\debug.log
echo.