# ボタン無効化問題 - V3.0.037 分析結果

## 問題状況
V3.0.037でCanExecute論理を削除したが、依然として以下のボタンが使用できない：
- 回転（RotateLeft/RotateRight）
- 並び替え（MovePageUp/MovePageDown）
- 削除（DeleteSelectedPages）

## 技術的観察
1. **XAML バインディング**：
   - `Command="{Binding PageOperation.RotateLeftCommand}"` 形式でバインディング
   - PageOperationプロパティ経由でコマンドを参照

2. **ViewModel側の実装**：
   - `[RelayCommand]`属性を使用
   - メソッド名: `RotateLeftAsync` → 生成されるコマンド名: `RotateLeftCommand`
   - CommunityToolkit.Mvvmのソースジェネレーターに依存

## 仮説
1. **ソースジェネレーター問題**：
   - CommunityToolkit.Mvvmのソースジェネレーターが正しく動作していない
   - .NET 8への移行で何か問題が発生した可能性

2. **partial class宣言不足**：
   - RelayCommandを使用するクラスは`partial`である必要がある
   - PageOperationViewModelクラスの宣言を確認する必要

3. **バインディングコンテキスト問題**：
   - MainViewModelのPageOperationプロパティが正しく初期化されていない
   - PropertyChangedイベントが適切に発火していない