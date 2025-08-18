using System;
using System.Threading.Tasks;
using System.Windows;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3 OSS標準: ドロップハンドラーインターフェース
    /// GongSolutions.WPF.DragDropパターン準拠
    /// </summary>
    public interface IAdvancedDropHandler
    {
        /// <summary>
        /// ドロップ可能性判定
        /// </summary>
        Task<bool> CanDropAsync(IAdvancedDropInfo dropInfo);

        /// <summary>
        /// ドロップ処理実行
        /// </summary>
        Task DropAsync(IAdvancedDropInfo dropInfo);

        /// <summary>
        /// ドラッグオーバー処理
        /// </summary>
        Task DragOverAsync(IAdvancedDropInfo dropInfo);
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドラッグハンドラーインターフェース
    /// </summary>
    public interface IAdvancedDragHandler
    {
        /// <summary>
        /// ドラッグ開始処理
        /// </summary>
        Task<object> StartDragAsync(IAdvancedDragInfo dragInfo);

        /// <summary>
        /// ドラッグ完了処理
        /// </summary>
        Task DragCompletedAsync(IAdvancedDragCompletedInfo dragCompletedInfo);
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドロップ情報インターフェース
    /// </summary>
    public interface IAdvancedDropInfo
    {
        /// <summary>ドロップされるデータ</summary>
        object Data { get; }

        /// <summary>ドロップターゲット要素</summary>
        FrameworkElement TargetElement { get; }

        /// <summary>ドロップ位置</summary>
        Point DropPosition { get; }

        /// <summary>許可されたエフェクト</summary>
        DragDropEffects AllowedEffects { get; }

        /// <summary>キーボード修飾子</summary>
        DragDropKeyStates KeyStates { get; }

        /// <summary>ファイルパス一覧（ファイルドロップの場合）</summary>
        string[] FilePaths { get; }

        /// <summary>ドロップ先インデックス</summary>
        int InsertIndex { get; set; }

        /// <summary>ドロップエフェクト</summary>
        DragDropEffects Effects { get; set; }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドラッグ情報インターフェース
    /// </summary>
    public interface IAdvancedDragInfo
    {
        /// <summary>ドラッグソース要素</summary>
        FrameworkElement SourceElement { get; }

        /// <summary>ドラッグ開始位置</summary>
        Point StartPosition { get; }

        /// <summary>ドラッグされるアイテム</summary>
        object SourceItem { get; }

        /// <summary>マウスイベント引数</summary>
        System.Windows.Input.MouseEventArgs MouseEventArgs { get; }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドラッグ完了情報インターフェース
    /// </summary>
    public interface IAdvancedDragCompletedInfo
    {
        /// <summary>ドラッグ情報</summary>
        IAdvancedDragInfo DragInfo { get; }

        /// <summary>ドラッグ結果</summary>
        DragDropEffects DragResult { get; }

        /// <summary>キャンセル状況</summary>
        bool IsCancelled { get; }
    }
}