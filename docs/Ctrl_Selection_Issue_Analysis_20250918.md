# Ctrl+クリック複数選択不具合 - 根本原因分析

## 問題の詳細
- **Shift+クリック**: 範囲選択は正常動作 ✅
- **Ctrl+クリック**: 個別選択が動作しない ❌
- **Ctrl+Shift+クリック**: 動作しない ❌
- **Ctrl+他のキー**: ショートカットは正常 ✅

## 根本原因

### ListBoxItemのControlTemplateによる標準動作の無効化

MainWindow.xaml（行500-551）でListBoxItemにカスタムControlTemplateを設定しているため、WPFの標準的な選択動作が完全に無効化されています。

```xml
<Setter Property="Template">
    <Setter.Value>
        <ControlTemplate TargetType="ListBoxItem">
            <!-- カスタムテンプレート -->
        </ControlTemplate>
    </Setter.Value>
</Setter>
```

### 問題のメカニズム
1. **ControlTemplate**は、コントロールの構造と動作を完全に置き換える
2. WPFの**標準ListBoxItem**は、Ctrl/Shiftキーの処理を内部で実装している
3. カスタムControlTemplateでは、これらの動作が失われる
4. 視覚的なトリガーのみ定義され、入力処理が含まれていない

## なぜShiftは部分的に動作するのか
SelectionChangedイベントハンドラーで部分的に処理されているが、完全ではない。

## 解決方法

### 推奨アプローチ: ControlTemplateの削除
ControlTemplateを削除し、視覚的なスタイルのみを設定する：

```xml
<ListBox.ItemContainerStyle>
    <Style TargetType="ListBoxItem">
        <Setter Property="Margin" Value="4,2"/>
        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Padding" Value="4"/>
        
        <!-- 視覚的なスタイルをトリガーで設定 -->
        <Style.Triggers>
            <!-- 選択時のスタイル -->
            <Trigger Property="IsSelected" Value="True">
                <Setter Property="BorderBrush" Value="#0066CC"/>
                <Setter Property="BorderThickness" Value="4"/>
                <Setter Property="Background">
                    <Setter.Value>
                        <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                            <GradientStop Color="#E8F4FF" Offset="0"/>
                            <GradientStop Color="#D0E8FF" Offset="1"/>
                        </LinearGradientBrush>
                    </Setter.Value>
                </Setter>
                <Setter Property="Effect">
                    <Setter.Value>
                        <DropShadowEffect BlurRadius="10" 
                                        ShadowDepth="3" 
                                        Color="#600066CC"
                                        Opacity="0.8"/>
                    </Setter.Value>
                </Setter>
            </Trigger>
            
            <!-- ホバー時のスタイル -->
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="BorderBrush" Value="#808080"/>
                <Setter Property="BorderThickness" Value="2"/>
            </Trigger>
            
            <!-- 選択+ホバー時のスタイル -->
            <MultiTrigger>
                <MultiTrigger.Conditions>
                    <Condition Property="IsSelected" Value="True"/>
                    <Condition Property="IsMouseOver" Value="True"/>
                </MultiTrigger.Conditions>
                <Setter Property="BorderBrush" Value="#0052A3"/>
                <Setter Property="BorderThickness" Value="5"/>
            </MultiTrigger>
        </Style.Triggers>
    </Style>
</ListBox.ItemContainerStyle>
```

### 代替アプローチ: ControlTemplateの修正
もしControlTemplateを維持する必要がある場合、基本テンプレートをベースに拡張：

```xml
<Setter Property="Template">
    <Setter.Value>
        <ControlTemplate TargetType="ListBoxItem">
            <Border Name="Border" 
                    Background="{TemplateBinding Background}"
                    BorderBrush="{TemplateBinding BorderBrush}"
                    BorderThickness="{TemplateBinding BorderThickness}"
                    Padding="{TemplateBinding Padding}">
                <ContentPresenter/>
            </Border>
            <!-- トリガーはTemplateBindingを使用 -->
        </ControlTemplate>
    </Setter.Value>
</Setter>
```

## 影響評価

### 推奨アプローチのメリット
- WPFの標準選択動作を完全に維持
- Ctrl/Shift/Ctrl+Shiftの組み合わせがすべて動作
- 視覚的なカスタマイズは維持
- 実装がシンプル

### リスク
- なし（視覚的な見た目は同じまま維持可能）

---

**分析完了**: 2025-09-18
**推奨**: ControlTemplateを削除し、スタイルトリガーのみ使用