# バージョン管理規則

## 第17条: バージョン管理システム

**規則**: ビルド実行時は必ずバージョン管理システムに従い、現在のバージョン番号を確認し、最後の桁を1増加させてからCLAUDE.md・MainWindow.xaml・AssemblyVersionを更新する。バージョン履歴も必ず記録する。

## バージョン管理詳細

### 現在バージョン
- **V3.0.031** (2025-09-04)

### バージョン形式
`メジャー.マイナー.ビルド番号`

### 自動インクリメント手順
1. 現在のバージョン番号をCLAUDE.mdから取得
2. 最後の桁（ビルド番号）を1増加
3. CLAUDE.mdのcurrent_versionを更新
4. MainWindow.xamlのTitle属性を更新
5. 変更履歴をversion_historyに記録
6. ビルド実行

### 更新箇所
- `CLAUDE.md`: repository_info.version & version_management.current_version
- `src/DocOrganizer.UI/Views/MainWindow.xaml`: Title属性
- `src/DocOrganizer.UI/DocOrganizer.UI.csproj`: AssemblyVersion

### タイトルバー表示
- 形式: `DocOrganizer {version}`
- 例: `DocOrganizer 3.0.031`

### バージョン管理Git連携
- コミットメッセージ形式: `[Version {version}] {変更内容概要}`
- タグ形式: `v{version}`
- 例: `v3.0.031`

## 最近のバージョン履歴

### V3.0.031 (2025-09-03)
- PDF表示バグ完全修正・クラス名統一によるDI解決完了

### V3.0.030 (2025-09-03)
- PDF処理エンジン変更完了 - Magick.NET→PdfiumViewer切り替え

### V3.0.028 (2025-09-03)
- PDF実装完全変更完了 - PdfiumViewer採用・GhostScript依存完全排除

### V3.0.026 (2025-08-22)
- PDF Provider本格運用開始・OSS監視システム実装

### V3.0.025 (2025-08-22)
- ドラッグ&ドロップ並び替え機能完全実装完了

[完全な履歴はCLAUDE.mdを参照]