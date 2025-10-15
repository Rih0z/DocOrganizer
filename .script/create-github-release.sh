#!/bin/bash
# =====================================================
# GitHubリリース自動作成スクリプト (Bash版)
# =====================================================
#
# 機能:
# - Version.csからバージョン番号を自動取得
# - リリースビルドを実行
# - GitHubリリースを作成
# - 実行ファイルをアップロード
#
# 前提条件:
# - GitHub CLI (gh) がインストール済み
# - gh auth login でGitHub認証済み
#
# 使用方法:
#   ./.script/create-github-release.sh
#   ./.script/create-github-release.sh "カスタムリリースノート"
# =====================================================

set -e  # エラー時に停止

# 色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# リリースノート（引数から取得）
RELEASE_NOTES="$1"

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN} DocOrganizer GitHub Release Creator${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# =====================================================
# ステップ1: バージョン番号の取得
# =====================================================

echo -e "${YELLOW}[1/6] Version.csからバージョン番号を取得中...${NC}"

VERSION_FILE="src/DocOrganizer.Core/Version.cs"

if [ ! -f "$VERSION_FILE" ]; then
    echo -e "${RED}エラー: Version.csが見つかりません: $VERSION_FILE${NC}"
    exit 1
fi

VERSION=$(grep -oP 'public const string Version = "\K[0-9]+\.[0-9]+\.[0-9]+' "$VERSION_FILE")

if [ -z "$VERSION" ]; then
    echo -e "${RED}エラー: Version.csからバージョン番号を取得できませんでした${NC}"
    exit 1
fi

TAG_NAME="v$VERSION"
echo -e "${GREEN}  ✓ バージョン: $VERSION${NC}"
echo -e "${GREEN}  ✓ タグ名: $TAG_NAME${NC}"

# =====================================================
# ステップ2: GitHub CLI (gh) の確認
# =====================================================

echo ""
echo -e "${YELLOW}[2/6] GitHub CLI (gh) の確認...${NC}"

if ! command -v gh &> /dev/null; then
    echo -e "${RED}エラー: GitHub CLI (gh) がインストールされていません${NC}"
    echo -e "${YELLOW}  インストール方法: https://cli.github.com/${NC}"
    exit 1
fi

echo -e "${GREEN}  ✓ GitHub CLI: インストール済み${NC}"

# GitHub認証確認
if ! gh auth status &> /dev/null; then
    echo -e "${RED}エラー: GitHub CLIで認証されていません${NC}"
    echo -e "${YELLOW}  認証方法: gh auth login${NC}"
    exit 1
fi

echo -e "${GREEN}  ✓ GitHub認証: OK${NC}"

# =====================================================
# ステップ3: Gitの状態確認
# =====================================================

echo ""
echo -e "${YELLOW}[3/6] Gitの状態確認...${NC}"

# 未コミットの変更確認
if [ -n "$(git status --porcelain)" ]; then
    echo -e "${YELLOW}警告: 未コミットの変更があります${NC}"
    git status --porcelain
    read -p "続行しますか? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}キャンセルしました${NC}"
        exit 0
    fi
fi

# リモートと同期確認
git fetch origin main &> /dev/null
LOCAL_COMMIT=$(git rev-parse main)
REMOTE_COMMIT=$(git rev-parse origin/main)

if [ "$LOCAL_COMMIT" != "$REMOTE_COMMIT" ]; then
    echo -e "${YELLOW}警告: ローカルとリモートが同期していません${NC}"
    read -p "git pushを実行しますか? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${CYAN}  → git push実行中...${NC}"
        git push origin main
        echo -e "${GREEN}  ✓ git push完了${NC}"
    fi
fi

echo -e "${GREEN}  ✓ Git状態: OK${NC}"

# =====================================================
# ステップ4: リリースビルド
# =====================================================

echo ""
echo -e "${YELLOW}[4/6] リリースビルド実行中...${NC}"

# クリーン
echo -e "${CYAN}  → dotnet clean...${NC}"
dotnet clean --verbosity quiet

# リストア
echo -e "${CYAN}  → dotnet restore...${NC}"
dotnet restore --verbosity quiet

# リリースビルド
echo -e "${CYAN}  → dotnet publish (リリースビルド・ログ無効版)...${NC}"
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o release \
    --verbosity quiet

# EXE存在確認
EXE_PATH="release/DocOrganizer.exe"
if [ ! -f "$EXE_PATH" ]; then
    echo -e "${RED}エラー: $EXE_PATH が生成されませんでした${NC}"
    exit 1
fi

EXE_SIZE=$(du -h "$EXE_PATH" | cut -f1)
echo -e "${GREEN}  ✓ ビルド完了: $EXE_PATH ($EXE_SIZE)${NC}"

# =====================================================
# ステップ5: リリースノート作成
# =====================================================

echo ""
echo -e "${YELLOW}[5/6] リリースノート作成...${NC}"

if [ -z "$RELEASE_NOTES" ]; then
    # CLAUDE.mdからバージョン履歴を取得
    if grep -q "| V$VERSION |" CLAUDE.md; then
        CHANGE_LINE=$(grep "| V$VERSION |" CLAUDE.md)
        CHANGE_DATE=$(echo "$CHANGE_LINE" | cut -d'|' -f3 | xargs)
        CHANGE_DESC=$(echo "$CHANGE_LINE" | cut -d'|' -f4 | xargs)

        RELEASE_NOTES="## V$VERSION ($CHANGE_DATE)

### 変更内容
$CHANGE_DESC

---

**ダウンロード**: 下記の \`DocOrganizer.exe\` をダウンロードしてご使用ください。

**インストール方法**:
1. 既存の \`DocOrganizer.exe\` を終了
2. ダウンロードした \`DocOrganizer.exe\` で上書き
3. アプリケーションを再起動

**システム要件**: Windows 10/11 (64bit)

---

**自動アップデート**: アプリケーション内の「ヘルプ」→「アップデート確認」から自動更新可能です。"
    else
        # デフォルトのリリースノート
        RELEASE_NOTES="## DocOrganizer V$VERSION

### 変更内容
詳細はCLAUDE.mdを参照してください。

---

**ダウンロード**: 下記の \`DocOrganizer.exe\` をダウンロードしてご使用ください。

**システム要件**: Windows 10/11 (64bit)"
    fi
fi

echo -e "${GREEN}  ✓ リリースノート準備完了${NC}"

# =====================================================
# ステップ6: GitHubリリース作成
# =====================================================

echo ""
echo -e "${YELLOW}[6/6] GitHubリリース作成中...${NC}"

# 既存のリリース確認
if gh release view "$TAG_NAME" &> /dev/null; then
    echo -e "${YELLOW}警告: タグ $TAG_NAME のリリースが既に存在します${NC}"
    read -p "既存のリリースを削除して再作成しますか? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${CYAN}  → 既存リリース削除中...${NC}"
        gh release delete "$TAG_NAME" --yes
    else
        echo -e "${YELLOW}キャンセルしました${NC}"
        exit 0
    fi
fi

# リリース作成
echo -e "${CYAN}  → GitHubリリース作成中...${NC}"

# リリースノートを一時ファイルに保存
TEMP_NOTES=$(mktemp)
echo "$RELEASE_NOTES" > "$TEMP_NOTES"

gh release create "$TAG_NAME" \
    "$EXE_PATH" \
    --title "DocOrganizer V$VERSION" \
    --notes-file "$TEMP_NOTES" \
    --latest

# 一時ファイル削除
rm -f "$TEMP_NOTES"

echo -e "${GREEN}  ✓ GitHubリリース作成完了${NC}"

# =====================================================
# 完了
# =====================================================

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN} ✓ リリース作成完了！${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${CYAN}リリース情報:${NC}"
echo -e "  バージョン: V$VERSION"
echo -e "  タグ: $TAG_NAME"
echo -e "  URL: https://github.com/Rih0z/DocOrganizer/releases/tag/$TAG_NAME"
echo ""
echo -e "${CYAN}次のステップ:${NC}"
echo -e "  1. GitHubでリリースを確認: https://github.com/Rih0z/DocOrganizer/releases"
echo -e "  2. アプリケーション内で「ヘルプ」→「アップデート確認」をテスト"
echo ""
