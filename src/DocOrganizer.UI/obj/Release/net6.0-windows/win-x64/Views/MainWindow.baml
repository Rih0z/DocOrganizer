   M S B A M L   `   `   `  ����   DocOrganizer&     DocOrganizer.UI.Views.MainWindow   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     -     � Title$  DocOrganizer 2.2��$	��700��$
��1000�� � WindowStartupLocation$ CenterScreen=����#F0F0F0 J� 	AllowDrop$
 True�� J� InputBindings �� 
 �� Key$ O�� �� 	Modifiers$
 Ctrlf� �� Command �� *OpenCommand+�� $ S��$
 Ctrlf� �� *SaveCommand+�� $ A��$
 Ctrlf� �� *SelectAllCommand+�� $ Z��$
 Ctrlf� �� *UndoCommand+�� $ Y��$
 Ctrlf� �� *RedoCommand+�� $ Delete�� �� *DeleteCommand+�� $ F1�� �� *ShowHelpCommand+�� $ OemPlus��$
 Ctrlf� �� *ZoomInCommand+�� $ OemMinus��$
 Ctrlf� �� *ZoomOutCommand+.���  � RowDefinitions �� $
��Auto ��� $
��Auto ��� $��* ��� $
��Auto �.+�o� $��0��
�������.@�m� $��ファイル(_F)��.@�m� $.��(PDF・画像ファイルを開く(_O)...�� m� InputGestureText$ Ctrl+O��	 m� Command	 �� *OpenCommand+
 m� Icon
 �� $
��📂��$��12(�$��Center=�$��Center=�m� $��
保存(_S)��$ Ctrl+S��	 �� *SaveCommand+
 �� $
��💾��$��12(�$��Center=�$��Center=�m� $%��名前を付けて保存(_A)...��	 �� *SaveAsCommand+�� m� $��閉じる(_C)��	 �� *CloseCommand+�� m� $��
終了(_X)��$ Alt+F4��	 �� *ExitCommand+m� $��
編集(_E)��.@�m� $��元に戻す(_U)��$ Ctrl+Z��	 �� *UndoCommand+m� $��やり直し(_R)��$ Ctrl+Y��	 �� *RedoCommand+�� m� $��すべて選択(_A)��$ Ctrl+A��	 �� *SelectAllCommand+m� $��選択解除(_D)��	 �� *DeselectAllCommand+m� $��ページ(_P)��.@�m� $��左回転(_L)��	 �� *RotateLeftCommand+
 �� $	��↺��$��12(�$��Center=�$��Center=�m� $��右回転(_R)��	 �� *RotateRightCommand+
 �� $	��↻��$��12(�$��Center=�$��Center=��� m� $��
削除(_D)��$ Delete��	 �� *DeleteCommand+
 �� $
��🗑��$��12(�$��Center=�$��Center=�m� $��
文書(_D)��.@�m� $��結合(_M)...��	 �� *MergeCommand+
 �� $
��📋��$��12(�$��Center=�$��Center=�m� $��分割(_S)...��	 �� *SplitCommand+
 �� $	��✂��$��12(�$��Center=�$��Center=��� m� $%��セキュリティ設定(_E)...��	 �� *SecurityCommand+m� $��
表示(_V)��.@�m� $��
拡大(_I)��$ Ctrl++��	 �� *ZoomInCommand+m� $��
縮小(_O)��$ Ctrl+-��	 �� *ZoomOutCommand+m� $��全体表示(_F)��	 �� *FitToWindowCommand+�� m� $"��サムネイルサイズ(_T)��.@�m� $	��小�� m� IsCheckable$
 True��	 �� *ThumbnailSmallCommand+m� $	��中��$
 True�� m� 	IsChecked$
 True��	 �� *ThumbnailMediumCommand+m� $	��大��$
 True��	 �� *ThumbnailLargeCommand+m� $��ヘルプ(_H)��.@�m� $��ヘルプの表示(_H)��$ F1��	 �� *ShowHelpCommand+�� m� $(��"アップデートの確認(_U)...��	 �� *CheckForUpdatesCommand+
 �� $
��🔄��$��12(�$��Center=�$��Center=��� m� $"��バージョン情報(_A)...��	 �� *AboutCommand+`� $��1�� `� 
Background
 �����.�c� -    c�Name$ MainToolBar��
������ .@��� -    ��Name$ 
OpenButton�� � ToolTip$0 *PDF・画像ファイルを開く (Ctrl+O)��$��4q����� *OpenCommand+.���� $
��📂��$��16(�$��Center=�$��Center=��� $ 保存 (Ctrl+S)��$��4q����� *SaveCommand+.���� $
��💾��$��16(�$��Center=�$��Center=��� -   $ クリックテスト��$��4q�
��� ���.���� $
��TEST��$��12(�$��Center=�$��Center=��� �� $ 	左回転��$��4q����� *RotateLeftCommand+{��� *HasSelectedPages+.���� $	��↺��$��16(�$��Center=�$��Center=��� $ 	右回転��$��4q����� *RotateRightCommand+{��� *HasSelectedPages+.���� $	��↻��$��16(�$��Center=�$��Center=��� $ 削除 (Delete)��$��4q����� *DeleteCommand+{��� *HasSelectedPages+.���� $
��🗑��$��16(�$��Center=�$��Center=��� �� $ 結合��$��4q����� *MergeCommand+{��� *
CanMerge+.���� $
��📋��$��16(�$��Center=�$��Center=��� $ 分割��$��4q����� *SplitCommand+{��� *HasDocument+.���� $	��✂��$��16(�$��Center=�$��Center=��� �� $ 拡大��$��4q����� *ZoomInCommand+.���� $��🔍+��$��14(�$��Center=�$��Center=��� $ 縮小��$��4q����� *ZoomOutCommand+.���� $��🔍-��$��14(�$��Center=�$��Center=��� $��80��$	��4,0q� �� SelectedItem �� *	ZoomLevel+.@��� .��50%�� .��75%��  �� 
IsSelected$
 True��.��100%�� .��125%�� .��150%�� .��200%� $��2�� � ColumnDefinitions �� $	��220 �$	��150���� $��3 ��� $��* �.+��� $��0�����#CCCCCC$��0,0,1,0q�
�������._��  �� $
��Auto ��� $��* �.+��� $��0�����#E0E0E0���#CCCCCC$��0,0,0,1q�._�� $��24��.+��� $��	ページ��$��Center=�$	��8,0q�$
��Bold"��� $��Center=�$��Right=�$	��8,0q����� *PageCountText+�� -    ��Name$ PageListBox��$��1��
�������$��0q�$��Disabled=� �� SelectionMode$ Extended=�$
 True��$ �� IsSynchronizedWithCurrentItem$
 TrueY����� *Pages+����  �� 
TargetType" ��.���  �� Property � �� �� Value$ 4q���  � ��$	 4,2q���  � ��$ Stretch=���  �@��
IsSelected �� *
IsSelected+ �� Mode$ TwoWay=� �� Triggers P�  P� Property �@��
IsSelected P� Value$
 True��. ���  � �� �#3399FF��  � ��
 ��������� .R��� ���#CCCCCC$��1q�
�������._��  �� $
��Auto ��� $	��120 �.+��� $��0�����#F0F0F0���#CCCCCC$��0,0,0,1q�._��� $��Center=�$	��4,2q�$��11(����� *
PageNumber+ �� StringFormat$	 {0}���� $��1��
�������$��4q�._��� $��Uniform=� �� BitmapScalingMode$ HighQuality=����� *ThumbnailImage+�� $��1��$��Stretch=����#CCCCCC  �� ShowsPreview$  False���� -   $��2�����#808080$
 True��._�� .+��� $
��Auto=�$
��Auto=����#808080$��Stretch=�$��Stretch=�.���� 
�������$��20q����#666666$��1q�$��Center=�$��Center=�._��� $��Uniform=�$ HighQuality=����� *CurrentPageImage+���� *PreviewWidth+���� *PreviewHeight+�� -   ! ��Name$! DropOverlay�����	#80000000$y�	Collapsed=�$
 True��._��� $��Center=�$��Center=�.+��� $<��6ここにPDFまたは画像ファイルをドロップ��
�������$��20(�$
��Bold"��� $'��!(PDF, HEIC, JPG, PNG, JPEG対応)�����	#CCFFFFFF$��14(�$��Center=�$��0,8,0,0q��� -   
�������$
 True��y��� *EmptyStateVisibility+._��� $��Center=�$��Center=�.+��� $<��6PDFまたは画像ファイルを開いてください�����#666666$��16(�$��Center=��� $(��"ここにドラッグ&ドロップ�����#666666$��18(�$
��Bold"�$��Center=�$��0,12,0,0q��� ���#E3F2FD���#1976D2$��2q�" �� CornerRadius$" 8��# �� Padding$
# 16,8q�$��0,16,0,0q�._��� .+��� $��対応形式:�����#1976D2$
��Bold"�$��Center=��� $��PDF, HEIC, JPG, PNG, JPEG�����#1976D2$��Center=�$��0,4,0,0q��� $?��9画像ファイルは自動的にPDFに変換されます�����#1976D2$��12(�$��Center=�$��Italic$�$��0,4,0,0q��� $��3�����#F0F0F0���� .R��  �� $��* ��� $
��Auto ��� $
��Auto ��� $
��Auto �.@��� $��0��.���� ���� *StatusMessage+�� $��1��.��� $	��150��$��16��$ � Value$ �� *ProgressValue+y��� *ProgressVisibility+�� $��2��.���� $	��8,0q����� *
PageInfo+�� $��3��.���� $	��8,0q����� *
FileInfo+