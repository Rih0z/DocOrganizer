@echo off
echo ========================================
echo DocOrganizer V3.0.098 プレビュー修正テスト
echo Phase 1 fixes: CurrentPageImage型修正
echo ========================================
echo.

REM Clear all diagnostic files
if exist constructor_diagnostic.txt del constructor_diagnostic.txt
if exist simple_debug_test.txt del simple_debug_test.txt
if exist debug_diagnostic.txt del debug_diagnostic.txt
if exist .logs rmdir /s /q .logs

REM Set environment variables for debug mode
set DOCORGANIZER_DEBUG=true
set DOCORGANIZER_OCR_ENABLED=false

echo Environment variables:
echo DOCORGANIZER_DEBUG=%DOCORGANIZER_DEBUG%
echo DOCORGANIZER_OCR_ENABLED=%DOCORGANIZER_OCR_ENABLED%
echo.

echo ========================================
echo 🔧 Phase 1修正版テスト開始
echo CurrentPageImage: object? → ImageSource?
echo PropertyChanged通知強化
echo ========================================

REM Run with extended timeout for testing
echo Starting DocOrganizer with Phase 1 fixes...
start /wait /b release-debug\DocOrganizer.exe
timeout /t 10 /nobreak >nul 2>&1
taskkill /F /IM DocOrganizer.exe >nul 2>&1

echo.
echo === 🔍 修正効果の診断結果 ===
echo.

echo ----------------------------------------
echo 📋 基本動作確認
echo ----------------------------------------

if exist constructor_diagnostic.txt (
    echo ✅ constructor_diagnostic.txt found - アプリケーション正常起動:
    type constructor_diagnostic.txt
    echo.
) else (
    echo ❌ constructor_diagnostic.txt not found - 起動問題あり
    echo.
)

if exist simple_debug_test.txt (
    echo ✅ simple_debug_test.txt found - OnStartup実行確認:
    type simple_debug_test.txt
    echo.
) else (
    echo ❌ simple_debug_test.txt not found - OnStartup未実行
    echo.
)

echo ----------------------------------------
echo 🎯 プレビュー機能専用ログ確認
echo ----------------------------------------

if exist debug_diagnostic.txt (
    echo ✅ debug_diagnostic.txt found - DebugLogger動作確認:
    type debug_diagnostic.txt
    echo.
) else (
    echo ❌ debug_diagnostic.txt not found - DebugLogger未動作
    echo.
)

if exist .logs\ (
    echo ✅ .logs folder found - 詳細ログ出力確認:
    echo.
    echo === Debug Log Contents ===
    for %%f in (.logs\*.log) do (
        echo --- Contents of %%f ---
        type "%%f"
        echo.
    )
) else (
    echo ❌ No .logs folder created - 詳細ログ未出力
    echo.
)

echo ========================================
echo 📊 Phase 1修正効果 診断結果
echo ========================================

echo 🔧 実装した修正内容:
echo   ✅ CurrentPageImage型修正: object? → ImageSource?
echo   ✅ OnCurrentPageImageChanged debug callback追加
echo   ✅ 型安全なキャスト処理実装
echo   ✅ 不要なPropertyChanged手動呼び出し削除
echo.

if exist constructor_diagnostic.txt (
    if exist .logs\ (
        echo 🎉 基本動作: 正常 - アプリケーション起動・ログ出力確認
        echo.
        echo 📋 次のステップ:
        echo   1. 実際にPDFファイルを読み込んでプレビュー機能テスト
        echo   2. 左側ページ選択時のプレビュー更新確認
        echo   3. ログでCurrentPageImage変更イベント確認
    ) else (
        echo ⚠️  基本動作: 部分成功 - 起動OK、ログ出力要確認
    )
) else (
    echo ❌ 基本動作: 失敗 - アプリケーション起動問題継続
)

echo.
echo Test completed - Phase 1 fixes
pause