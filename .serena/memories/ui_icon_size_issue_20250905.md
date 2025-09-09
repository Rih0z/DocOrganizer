# UIアイコンサイズ問題分析メモリ

## 問題の概要
- 日付: 2025-09-05  
- 問題: UIボタンとアイコンのサイズ不整合
- 影響範囲: MainWindow.xaml, App.xaml, AppSettings.json

## 現在の状況
1. **右側のテキスト「画像ファイルは自動的にPDFに変換されます」が拡大されている**
   - 想定: 通常のフォントサイズであるべき
   - 現状: 大きすぎる

2. **回転・ゴミ箱アイコンが小さいまま**
   - 想定: 48pxに拡大されるべき
   - 現状: 小さいまま

## 設定ファイルの状態
- AppSettings.json:
  - CalculatedIconFontSize: 48 (変更済み)
  - CalculatedButtonFontSize: 10 (変更済み)

- App.xaml:
  - MenuIconStyle: FontSize=15px
  - ToolbarIconStyle: FontSize=48px (新規追加)

- MainWindow.xaml:
  - 回転・ゴミ箱: ToolbarIconStyle使用に変更済み
  - その他: MenuIconStyle使用

## 推測される問題
App.xaml.csのUpdateButtonStyles()メソッドで、MenuIconStyleがCalculatedIconFontSize（48px）で動的に更新されている可能性がある。これにより、全てのMenuIconStyleを使用している要素が影響を受けている。

## 必要な修正
1. UpdateButtonStyles()メソッドの確認と修正
2. アイコンスタイルの適切な分離
3. 動的更新の制御