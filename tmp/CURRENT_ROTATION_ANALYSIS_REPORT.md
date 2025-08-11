# 現在の回転問題 詳細分析レポート

## 🚨 ユーザー報告問題

**日時**: 2025-08-08 14:00  
**問題**: 修正後も問題が継続  

### 報告された症状
1. **まだ一致していない**: 左側プレビューと右側プレビュー・PDF出力が一致しない
2. **自動回転機能も間違っている**: ドキュメントの上側が正しく判定されず、常に左に90度回転して表示される

## 🔍 Serena MCP 分析結果

### AutoOrientの現在の適用箇所

#### ImageProcessingService.cs での AutoOrient 呼び出し
1. **Line 753**: `image.Mutate(x => x.AutoOrient());` - 一般画像ファイル用
2. **Line 804**: `image.Mutate(x => x.AutoOrient());` - バイト配列読み込み用
3. **Line 1151**: `tempImage.Mutate(x => x.AutoOrient());` - EXIF回転検出用

### 根本的な問題の特定

#### 1. AutoOrient の動作原理
- `AutoOrient()` は EXIF Orientation タグを読み取り、自動的に画像を正しい向きに回転
- しかし、元の EXIF データが間違っている、または期待と異なる場合、常に同じ方向に回転する

#### 2. 「常に左に90度回転」の意味
- これは AutoOrient が EXIF Orientation = 6 (Rotate 90 CW) を検出している可能性
- 実際の画像の正しい向きに関係なく、同じ EXIF 値により一律に回転している

#### 3. 左側プレビューと右側プレビューの不一致
現在の修正では以下の問題が残存：
- **左側プレビュー**: PageViewModel で ImageProcessingService.GetImageThumbnailAsync() 経由
- **右側プレビュー**: MainViewModel で別の処理経路
- **PDF出力**: さらに別の処理経路

## 🎯 推定される真の原因

### 原因1: EXIF Orientation の誤解釈
```csharp
// 現在の処理
image.Mutate(x => x.AutoOrient()); // 常に同じ回転を適用

// 問題: EXIFが示す向きと実際の正しい向きが異なる場合
```

### 原因2: 処理経路の分岐による不整合
- **サムネイル生成**: AutoOrient適用
- **プレビュー表示**: 別のAutoOrient適用 
- **PDF出力**: また別のAutoOrient適用
- **結果**: 同じ画像に対して異なる回転処理

### 原因3: HEIC変換時のEXIF情報の継承問題
```csharp
// HEIC → JPEG変換時
// 変換後のJPEGファイルが元のEXIF Orientationを保持
// その後のAutoOrientで二重回転が発生
```

## 💡 真の解決策

### 解決方針A: AutoOrient完全無効化 + 手動回転制御
1. **全AutoOrient呼び出しを削除**
2. **EXIF Orientationを読み取り専用で使用**
3. **手動で正しい回転角度を計算・適用**

### 解決方針B: 統一回転処理パイプライン
1. **単一の回転処理メソッドを作成**
2. **全ての表示・出力で同じメソッドを使用**
3. **EXIF情報を正確に解釈して補正**

### 解決方針C: 設定可能な回転補正
1. **ユーザーが画像の正しい向きを指定可能**
2. **自動回転を無効化するオプション**
3. **画像ごとの個別回転設定保存**

## 🔧 推奨される即座修正

### ステップ1: AutoOrient完全無効化テスト
全てのAutoOrient呼び出しを一時的にコメントアウトして、回転なしの状態で表示を確認

### ステップ2: EXIF Orientation値のログ出力
各画像のEXIF Orientation値を確認し、なぜ常に左90度回転になるかを特定

### ステップ3: 手動回転テスト
AutoOrientを使わずに、手動で0度、90度、180度、270度回転を適用して正しい向きを特定

## 📊 次の調査項目

1. **EXIF Orientationの実際の値**を各画像で確認
2. **ImageSharp AutoOrient**の具体的な動作ログ
3. **HEIC変換プロセス**でのEXIF情報の処理方法
4. **WPF表示系**でのBitmapImage回転の影響

---

**分析者**: AI Assistant + Serena MCP  
**ステータス**: 🔍 **根本原因特定中**  
**次のアクション**: AutoOrient無効化による動作確認テスト