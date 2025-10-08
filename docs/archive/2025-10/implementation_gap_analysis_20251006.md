# 実装との差分分析レポート

**作成日**: 2025-10-06
**分析者**: Serena MCP + Claude
**対象**: docs/rule/project_structure.md vs 実際の実装

---

## 📊 重大な差分一覧

### 1. バージョン番号の不一致 ⚠️

| 箇所 | 記載内容 | 実際 |
|------|---------|------|
| **CLAUDE.md** | V3.0.123 | ✅ 正しい |
| **project_structure.md** | V3.0.031 | ❌ **92バージョン古い** |
| **最新ビルド** | release-debug\DocOrganizer.exe | ✅ V3.0.123 |

**影響**: 開発者が古い情報を参照してしまう重大なリスク

---

### 2. ビルドコマンドの不一致

#### project_structure.md記載
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

#### CLAUDE.md記載（正しい）
```bash
# デフォルト: release-debugビルド（デバッグログ有効）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release-debug

# リリース版ビルド（ユーザーから明示的指示がある場合のみ）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

**影響**: デバッグログシステム（V3.0.064で実装）の運用方針が反映されていない

---

### 3. 最新EXEパスの不一致

| ドキュメント | 記載内容 |
|-------------|---------|
| **project_structure.md** | `release\DocOrganizer.exe` |
| **CLAUDE.md（正しい）** | `release-debug\DocOrganizer.exe`（デフォルト） |

**影響**: 開発時のテスト手順が不正確

---

### 4. ディレクトリ構造の未反映

#### project_structure.md記載
```
DocOrganizer/
├── release/               # ビルド出力・実行ファイル
│   ├── DocOrganizer.exe
│   ├── run-debug.bat
│   └── run-production.bat
```

#### 実際の構造（git status参照）
```
DocOrganizer/
├── release-debug/         # デフォルトビルド出力（デバッグログ有効）
│   ├── DocOrganizer.exe
│   ├── run-debug.bat
│   └── run-production.bat
├── release/              # リリースビルド出力（明示的指示時）
│   ├── DocOrganizer.exe
│   ├── run-debug.bat
│   └── run-production.bat
├── .tmp/                 # 一時分析・計画ファイル（.gitignore対象）
└── .logs/                # デバッグログ出力先（V3.0.064実装）
    └── debug.log
```

**影響**: 新規開発者が正しいフォルダ構造を理解できない

---

### 5. 最新実装の未反映

#### CLAUDE.mdには記載されているが、project_structure.mdには未反映の主要機能

| バージョン | 実装内容 | project_structure.mdの記載 |
|-----------|---------|--------------------------|
| V3.0.123 | 複数選択移動バグ完全修正 | ❌ なし |
| V3.0.117 | 複数ページ一括移動完全実装 | ❌ なし |
| V3.0.116 | 複数ページドラッグ&ドロップ実装 | ❌ なし |
| V3.0.110 | ズーム機能完全修正 | ❌ なし |
| V3.0.103 | 複数選択バグ完全修正 | ❌ なし |
| V3.0.073 | パフォーマンス最適化 | ❌ なし |
| V3.0.068 | Undo/Redo完全実装 | ❌ なし |
| V3.0.064 | 統一ログ管理システム | ❌ なし |

**影響**: プロジェクト構造文書が実装の進化に追従していない

---

### 6. 実装コードとの整合性確認

#### MovePagesCommand.cs（V3.0.123実装）
```csharp
// 🎯 V3.0.123: 複数ページ移動時の位置ズレ修正
// 移動方向を判定し、適切な順序で処理

// 複数ページ一括移動のコンストラクタ
public MovePagesCommand(PdfDocument document, List<(PdfPage page, int newPosition)> pageMoves, Action onPagesChanged)
```

**確認結果**: V3.0.117で複数ページ一括移動機能が実装され、V3.0.123で処理順序最適化が完了している

#### DragDropHandlerViewModel.cs
- V3.0.116で複数ページドラッグ&ドロップ対応
- V3.0.117で上下移動ボタンの複数選択対応

**確認結果**: CLAUDE.mdのバージョン履歴と実装が一致

---

## 🔧 修正が必要な項目

### 優先度1: 即時修正必須
1. **project_structure.md バージョン番号更新**: V3.0.031 → V3.0.123
2. **ビルドコマンド更新**: release-debug/releaseの2つのビルド方法を明記
3. **最新EXEパス更新**: デフォルトをrelease-debugに変更

### 優先度2: 重要
4. **ディレクトリ構造更新**: .tmp/.logs/release-debugフォルダを追加
5. **主要機能リスト更新**: V3.0.068以降の実装を反映
   - Undo/Redo完全実装
   - 複数ページ一括移動
   - 複数選択ドラッグ&ドロップ
   - ズーム機能
   - 統一ログ管理システム

### 優先度3: 推奨
6. **アーキテクチャ説明の更新**: 最新のV3アーキテクチャを反映
7. **技術スタック更新**: PdfiumViewer採用（V3.0.030）の明記

---

## 📝 修正計画

### Step 1: project_structure.md 完全書き換え
- 現在のV3.0.123実装に基づいた正確な情報に更新
- CLAUDE.mdとの整合性を100%確保
- 実装コード（MovePagesCommand等）との整合性確認済み情報を反映

### Step 2: アーキテクチャ文書の参照整理
- V3_COMPLETE_ARCHITECTURE.mdへのリンク追加
- V3_ARCHITECTURE_IMAGE_DISPLAY.mdへのリンク追加

### Step 3: 検証
- ビルドコマンドの実行確認
- EXEパスの存在確認
- ディレクトリ構造の実際との照合

---

## ✅ 次のアクション

1. **project_structure.md を完全書き換え** ← これを実行
2. docs フォルダ整理実行（前述の整理計画に基づく）
3. V3.0.123 変更内容レポート作成（未作成の場合）

**ユーザーの承認を待ちます。**
