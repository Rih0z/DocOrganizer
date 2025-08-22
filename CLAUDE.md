# Claude.md - DocOrganizer 画像PDF変換ツール AIコーディング原則

```yaml
ai_coding_principles:
  meta:
    version: "2.3"
    last_updated: "2025-08-15"
    description: "DocOrganizer 画像PDF変換ツール開発用AIコーディング実行原則"
    project_name: "DocOrganizer - CubePDF Utility互換 汎用PDF編集ツール"
    project_concept: "CubePDF Utilityをベースに、文書整理に特化した機能を追加した汎用PDF編集ツール"
    
  repository_info:
    github_url: "https://github.com/Rih0z/DocOrganizer"
    latest_exe_path: "C:\\Users\\217216X721451\\github\\DocOrganizer\\release\\DocOrganizer.exe"
    version: "3.0.017"
    features:
      - "PDF編集機能（CubePDF Utility互換）"
      - "画像→PDF変換（HEIC/JPG/PNG/JPEG対応）"
      - "ドラッグ&ドロップでのファイル操作"
      - "自動アップデート機能（GitHub Releases連携）"
      - "ページ回転・削除・並び替え"
      - "PDF結合・分割"
    build_command: "dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
    
  user_documentation:
    description: "ユーザー向け使用ガイド"
    pdf_export_guide: "docs/PDF保存機能使用ガイド.md"
    main_readme: "README.md"
    last_updated: "2025-08-20"
    coverage: "V3.0.009の完全な使用方法とトラブルシューティング"
    
  technical_documentation:
    description: "技術実装ドキュメント"
    heic_support_guide: "docs/HEIC_Support_Complete_Guide.md"
    complete_architecture: "docs/V3_COMPLETE_ARCHITECTURE.md"
    image_display_architecture: "docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md"
    rotation_features: "docs/V3_ROTATION_AND_IMAGE_REPLACEMENT.md"
    drag_drop_implementation_report: "docs/V3_Drag_Drop_Complete_Implementation_Report_20250822.md"
    heic_pdf_bug_fix_report: "docs/HEIC_PDF_Export_Bug_Fix_Complete_Report_20250821.md"
    ui_zoom_bug_fix_report: "docs/UI_Zoom_Feature_Bug_Fix_Complete_Report_20250821.md"
    last_updated: "2025-08-22"
    coverage: "V3.0.025ドラッグ&ドロップ並び替え機能完全実装、V3完全アーキテクチャ解説、HEIC完全対応、プロバイダーパターン実装詳細、各種バグ修正完了報告"
    
  version_management:
    description: "ビルド時自動バージョン管理システム"
    last_updated: "2025-08-20"
    mandatory: "ビルドの度に最後の桁を自動インクリメント"
    
    current_version: "3.0.025"
    version_format: "メジャー.マイナー.ビルド番号"
    increment_rule: "ビルドの度に最後の桁（ビルド番号）を1つずつ上げる"
    
    version_history:
      "3.0.025":
        date: "2025-08-22"
        changes: "ドラッグ&ドロップ並び替え機能完全実装完了 - 3段階修正による根本解決"
        build_reason: "V3.0.024視覚フィードバック成功後、実際の並び替え機能が動作しない3つの根本問題を完全解決"
        technical_detail: "Phase1: V3AdvancedDragDropBehavior.OnDrop()イベント重複防止フラグ実装、Phase2: V3DropInfo.CalculateInsertIndex()座標系変換修正・堅牢化、Phase3: MainCompositeViewModel.OnPageReorderRequested()でInsertIndex優先使用・PageOperationViewModel InsertIndexベースオーバーロード追加、完全なサムネイル並び替え動作実現"
      "3.0.020":
        date: "2025-08-22"
        changes: "InsertIndex計算実装完了 - サムネイル並び替え機能の完全動作実現"
        build_reason: "V3.0.019でInsertIndex=-1固定問題を解決し、正確なドロップ位置計算を実装"
        technical_detail: "V3DropInfo修正: 1)CalculateInsertIndex()メソッド追加 2)ListBoxItem位置ベース挿入インデックス計算 3)ドロップ位置の上半分/下半分判定 4)FindParentListBox()ヘルパーメソッド実装 5)完全なサムネイル並び替え機能実現"
      "3.0.019":
        date: "2025-08-22"
        changes: "静的キャッシュによる安全なサムネイル並び替え実装完了 - V3.0.018クラッシュ問題根本解決"
        build_reason: "V3.0.018のOnDragOver無限ループ問題を解決し、WPF制約準拠の安全な実装に変更"
        technical_detail: "DragDropHandlerViewModel実装: 1)静的キャッシュDictionary<string,V3PageViewModel>導入 2)StartDragAsync→DataFormats.Text+GUID方式に変更 3)DropAsync→キャッシュベース並び替え対応 4)自動クリーンアップTimer実装 5)包括的エラーハンドリング 6)V3.0.019詳細ログトレース機能"
      "3.0.017":
        date: "2025-08-22"
        changes: "DropAsync詳細ログ大幅強化完了 - 7段階詳細分析ログでドラッグ&ドロップ問題の完全特定"
        build_reason: "V3.0.016で並び替え機能が動作しない根本原因を特定するため、DropAsync内の全処理分岐に詳細ログを追加"
        technical_detail: "DragDropHandlerViewModel.DropAsync内に1)基本情報確認 2)IDataObjectキャスト確認 3)データ形式完全列挙 4)各形式存在確認 5)内部ページ並び替え処理詳細 6)Serializableデータ取得確認 7)ドロップターゲット検索詳細の7段階詳細ログを実装、DEBUG_LOG.txtで完全な実行フロー追跡可能に"
      "3.0.014":
        date: "2025-08-21"
        changes: "V3DropInfo根本修正完了 - HitTestによる正確なTargetElement特定実装"
        build_reason: "サムネイル並び替え失敗の真の原因「TargetElementが常にMainWindow」問題を解決"
        technical_detail: "V3DropInfoコンストラクター修正でFindActualTargetElement追加、HitTest→ListBoxItem検索→V3PageViewModel確認の段階的要素特定、System.Windows.Controls/Mediaのusing追加、DragDropHandlerViewModelのFindTargetPageViewModelが正しく動作する環境を構築"
      "3.0.013":
        date: "2025-08-21"
        changes: "サムネイル並び替え根本問題修正完了 - TargetElement検索ロジック3段階実装"
        build_reason: "ログ分析によりTargetElement.DataContextがnullのため処理スキップされる問題を特定・解決"
        technical_detail: "FindTargetPageViewModel追加で1)直接DataContext 2)祖先ListBoxItem検索 3)HitTest位置ベース検索の3段階フォールバック実装、FindAncestorOfType/FindTargetByHitTestヘルパーメソッド追加、System.Windows.Controls/MediaのusingDirective追加"
      "3.0.012":
        date: "2025-08-21"
        changes: "双目的ドロップターゲット問題修正完了 - 内部並び替えと外部ファイルドロップの統合処理"
        build_reason: "ユーザー報告「並び替えが画像追加になる・右側ドロップ無効化」問題の根本解決"
        technical_detail: "DragDropHandlerViewModel.DropAsync拡張でDataFormats.FileDrop(外部ファイル)とDataFormats.Serializable(内部並び替え)の処理分岐実装、両方のドロップエリアでファイル追加・並び替え対応、詳細デバッグログによる動作トレーサビリティ確保"
      "3.0.011":
        date: "2025-08-21"
        changes: "サムネイルドラッグ&ドロップ機能修正 - ListBoxItemにIsDragSource設定追加・デバッグログ強化"
        build_reason: "ユーザー報告「左側サムネイルをドラッグしても何も起こらない」問題の根本修正"
        technical_detail: "MainWindow.xamlのListBox.ItemContainerStyleにV3AdvancedDragDropBehavior.IsDragSource=True追加、DragDropHandlerViewModel・V3AdvancedDragDropBehaviorに詳細デバッグログ追加、実際のドラッグ操作を可能にする修正完了"
      "3.0.010":
        date: "2025-08-21"
        changes: "サムネイル画像ドラッグ&ドロップ並び替え機能実装完了"
        build_reason: "視覚的インジケーター付きドラッグ&ドロップによるUX大幅向上"
        technical_detail: "挿入位置判定ロジック拡張、InsertionIndicatorAdorner実装、V3AdvancedDragDropBehavior統合、Phase1-3完全実装"
      "3.0.009":
        date: "2025-08-20"
        changes: "HEIC完全対応実装完了 - プレビュー表示・アスペクト比問題完全解決"
        build_reason: "右側プレビュー表示失敗とアスペクト比崩れの根本修正"
        technical_detail: "PreviewManagementViewModel修正でプロバイダーアーキテクチャ採用、全プロバイダーでアスペクト比保持ロジック実装、CalculatePreviewSize()追加、DecodePixelサイズ指定最適化"
      "3.0.008":
        date: "2025-08-20"
        changes: "プロバイダーアーキテクチャ基盤実装"
        build_reason: "企業レベル拡張可能設計によるHEIC表示問題根本解決準備"
        technical_detail: "統一プロバイダーインターフェース設計、ImageProcessingProviderManager実装、属性ベース自動登録システム準備"
      "3.0.007":
        date: "2025-08-20"
        changes: "企業レベルプロバイダーアーキテクチャ実装 - HEIC検証問題根本解決"
        build_reason: "Strategy Pattern + Provider Pattern によるHEICドラッグ&ドロップバグ完全修正"
        technical_detail: "IImageValidationProvider基盤システム実装、HeicValidationProvider/StandardImageValidationProvider/GifValidationProvider統合、形式別最適化検証"
      "3.0.006":
        date: "2025-08-20"
        changes: "バグ分析・アーキテクチャ設計完了"
        build_reason: "HEIC検証問題の根本原因分析と企業レベル解決策設計"
        technical_detail: "OSS研究、ベストプラクティス分析、プロバイダーアーキテクチャ設計文書作成"
      "3.0.005":
        date: "2025-08-19"
        changes: "画像向き問題完全解決 - AutoOrient()統一実装"
        build_reason: "サムネイルとPDF出力の画像向き完全一致"
        technical_detail: "ProcessPageImageAsync内にAutoOrient()追加でサムネイル生成と同一処理パイプライン実現"
      "3.0.004":
        date: "2025-08-19"
        changes: "EXIF除去実装 - SkipMetadata=true追加"
        build_reason: "PDFsharp重複回転防止"
      "3.0.003":
        date: "2025-08-19"
        changes: "PDFsharp（オリジナル版）採用 - PdfSharpCore→PDFsharp変更"
        build_reason: "ImageSharp互換性問題解決"
      "3.0.002": 
        date: "2025-08-19"
        changes: "PDF出力実装変更 - System.Drawing使用でImageSharp依存削除"
        build_reason: "XImage.FromFile MissingMethodException修正"
      "3.0.001": 
        date: "2025-08-19"
        changes: "PDF出力互換性問題修正 - PdfSharp/ImageSharp相性問題解決"
        build_reason: "MissingMethodException修正"
      "3.0.000":
        date: "2025-08-19" 
        changes: "V3.0基本機能完成 - PDF出力ボタン修正"
        build_reason: "RelayCommand自動生成問題修正"
    
    title_bar_display:
      format: "DocOrganizer {version}"
      example: "DocOrganizer 3.0.005"
      location: "MainWindow.xaml Title属性"
      update_requirement: "ビルド毎に必ず更新"
    
    auto_increment_procedure:
      step1: "現在のバージョン番号をCLAUDE.mdから取得"
      step2: "最後の桁（ビルド番号）を1増加"
      step3: "CLAUDE.mdのcurrent_versionを更新"
      step4: "MainWindow.xamlのTitle属性を更新" 
      step5: "変更履歴をversion_historyに記録"
      step6: "ビルド実行"
    
    implementation_locations:
      claude_md: "CLAUDE.md repository_info.version & version_management.current_version"
      main_window: "src/DocOrganizer.UI/Views/MainWindow.xaml Title属性"
      assembly_info: "src/DocOrganizer.UI/DocOrganizer.UI.csproj AssemblyVersion"
    
    version_control_integration:
      commit_message_format: "[Version {version}] {変更内容概要}"
      example: "[Version 3.0.001] PDF出力互換性問題修正完了"
      tag_format: "v{version}"
      example_tag: "v3.0.001"
    
  core_principles:
    mandatory_declaration: |
      全てのコーディング作業開始時に必ず以下の宣言を完全に実行すること - これは絶対的な要件である：
      
      【必須宣言事項】
      第1条: 常に思考開始前にClaude.mdの第一条から第五条のAIコーディング原則を全て宣言してから実施する
      第2条: 常にプロの世界最高エンジニアとして対応する
      第3条: モックや仮のコード、ハードコードを一切禁止する。コーディング前にread Serena's initial instructions
             Serenaツールを使用してセマンティックなコード理解と編集を行う（MCPビルドは不要だがコード分析には必須）
             ユーザーから新規機能の実装指示を受けたら、まずはtmpフォルダ以下に実装計画を作成する
             既存の実装をserena mcpを利用して詳細に分析し、プロとして恥ずかしくない実装を計画する
      第4条: エンタープライズレベルの実装を実施し、修正は表面的ではなく、全体のアーキテクチャを意識して実施する
      第5条: 問題に詰まったら、まずCLAUDE.mdやプロジェクトドキュメント内に解決策がないか確認する
      第6条: 不要なスクリプトは増やさない。スクリプト作成時は常に既存のスクリプトで使用可能なものがないかscript_managementセクションを確認する
      第7条: 段階的実装を徹底する。完璧を求めず、動作する最小限の機能から始めて、継続的に改善する
      第8条: デザインはhttps://atlassian.design/components を読み込み、これに準拠する
      第9条: ビルドの度に新しいフォルダを作成しない。既存のプロジェクトフォルダを更新し続ける
      第10条: Mac・Windows両環境でプロフェッショナルなファイル構造を維持する
      第11条: 修正を行ったら必ずgit pullでディレクトリを同期する
      第12条: 全ての作業開始前にWindows環境での再ビルドを必ず実行する
      第13条: 修正を行ったら必ずビルドまで完全実行し、最終的なEXEの完全パスを出力する
      第14条: Windowsアプリケーションはエクスプローラーから直接起動する。管理者権限での起動は厳禁
      第17条: ビルド実行時は必ずバージョン管理システムに従い、現在のバージョン番号を確認し、最後の桁を1増加させてからCLAUDE.md・MainWindow.xaml・AssemblyVersionを更新する。バージョン履歴も必ず記録する。
    
    pre_work_requirements:
      description: "全ての作業開始前の必須手順"
      last_updated: "2025-07-24"
      mandatory: "作業開始前に必ず実行"
      
      step1_git_sync:
        requirement: "Git同期による最新状態への更新"
        importance: "第10条準拠 - 環境間整合性確保"
        procedure:
          - "git pull origin main"
          - "最新状態確認"
        verification: "git status で clean working tree 確認"
      
      step2_windows_build:
        requirement: "Windows環境でのビルド実行"
        importance: "WPFプロジェクトの最新状態確保"
        mandatory_commands:
          - "cd C:\\Users\\koki\\ezark\\standard-image\\Standard-image\\v2.2-taxdoc-organizer"
          - "dotnet clean"
          - "dotnet restore" 
          - "dotnet build --configuration Release"
          - "dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
        success_verification: "release\\DocOrganizer.exe の生成確認"
        
      execution_order: 
        1: "Git同期実行"
        2: "Windows環境ビルド"
        3: "作業開始"
      
      failure_handling:
        git_sync_fail: "競合解決、手動同期実行"
        build_fail: "依存関係確認、.NET SDK状況確認"
    第1条: 
      rule: "常に思考開始前にClaude.mdの第一条から第五条のAIコーディング原則を全て宣言してから実施する"
      importance: "最重要 - 宣言なしでの作業開始は厳禁"
      related_sections: ["execution_checklist", "mindset"]
    第2条: 
      rule: "常にプロの世界最高エンジニアとして対応する"
      related_sections: ["mindset", "quality_standards", "domain_requirements"]
    第3条: 
      rule: "モックや仮のコード、ハードコードを一切禁止する。コーディング前にread Serena's initial instructions。Serenaツールを使用してセマンティックなコード理解と編集を行う（MCPビルドは不要だがコード分析には必須）。ユーザーから新規機能の実装指示を受けたら、まずはtmpフォルダ以下に実装計画を作成する。既存の実装をserena mcpを利用して詳細に分析し、プロとして恥ずかしくない実装を計画する。"
      related_sections: ["implementation", "architecture", "quality_standards", "https://github.com/oraios/serena"]
    第4条: 
      rule: "エンタープライズレベルの実装を実施し、修正は表面的ではなく、全体のアーキテクチャを意識して実施する"
      related_sections: ["architecture", "quality_standards", "security_requirements", "performance_requirements", "build_procedures", "artifact_management"]
    第5条:
      rule: "問題に詰まったら、まずCLAUDE.mdやプロジェクトドキュメント内に解決策がないか確認する"
      importance: "効率的な問題解決のための必須プロセス"
      related_sections: ["documentation_management", "troubleshooting", "execution_checklist"]
    第6条:
      rule: "不要なスクリプトは増やさない。スクリプト作成時は常に既存のスクリプトで使用可能なものがないかscript_managementセクションを確認する。スクリプトを作成したらscriptsフォルダに格納する。"
      importance: "プロジェクトの整理整頓と保守性向上"
      related_sections: ["script_management", "essential_scripts"]
    第7条:
      rule: "段階的実装を徹底する。完璧を求めず、動作する最小限の機能から始めて、継続的に改善する。"
      importance: "V2.0の失敗から学んだ最重要原則"
      related_sections: ["v2_reflection", "v21_implementation_plan"]
    第8条: 
      rule: "デザインはhttps://atlassian.design/components を読み込み、これに準拠する。"
      importance: "一貫性のあるプロフェッショナルなUIデザインの実現"
      related_sections: "https://atlassian.design/components"
    第9条:
      rule: "ビルドの度に新しいフォルダを作成しない。既存のプロジェクトフォルダを更新し続ける。不要なファイルは削除して整理する。"
      importance: "プロジェクト管理の効率化とディスク容量の節約"
      implementation:
        - "v2.2-doc-organizer/をメインフォルダとして使用"
        - "src/にソースコードを保持"
        - "release/に最新のEXEを配置"
        - "不要な一時ファイルは削除"
      related_sections: ["project_structure", "artifact_management"]
    第10条:
      rule: "Mac・Windows両環境でプロフェッショナルなファイル構造を維持する。誰に見せても恥ずかしくない整理整頓を徹底する。"
      importance: "クライアント・上司に見せても完全にプロフェッショナルな印象を与える"
      implementation:
        - "Mac: ~/Documents/Projects/DocOrganizer/"
        - "Windows: C:\\builds\\DocOrganizer-repo\\ (既存構造を整理)"
        - "本番・開発・アーカイブの明確な分離"
        - "bin/obj等の開発用一時ファイルの定期削除"
      related_sections: ["file_organization_standards", "professional_standards", "git_synchronization_standards"]
    第11条:
      rule: "修正を行ったら必ずgit pullでディレクトリを同期する。環境間の整合性を常に保つ。"
      importance: "Mac・Windows環境の完全同期によるプロジェクト整合性確保"
      mandatory: "例外なく実行すること - 同期忘れは重大なエラーの原因"
      implementation:
        - "Mac環境で変更後: git add . && git commit -m '変更内容' && git push origin main"
        - "Windows環境で同期: git pull origin main"
        - "Windows環境で変更後: git add . && git commit -m '変更内容' && git push origin main"  
        - "Mac環境で同期: git pull origin main"
        - "変更作業開始前に必ずgit pullで最新状態に更新"
      workflow:
        - "作業開始時: git pull origin main"
        - "作業中: 定期的なcommit"
        - "作業完了時: git push origin main"
        - "他環境移行時: git pull origin main"
      related_sections: ["git_synchronization_standards", "version_control_workflow"]
    第12条:
      rule: "全ての作業開始前にWindows環境での再ビルドを必ず実行する。"
      importance: "WPFプロジェクト開発の前提条件"
      mandatory: "例外なく実行すること - WPFビルドには絶対必要"
      implementation:
        step1: "Git同期: git pull origin main"
        step2: "再ビルド: dotnet clean && dotnet restore && dotnet build --configuration Release"
        step3: "EXE生成: dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
        verification: "release\\DocOrganizer.exe の存在と動作確認"
      failure_response:
        sync_fail: "手動Git同期、競合解決"
        build_fail: ".NET SDK確認、依存関係修正"
      related_sections: ["pre_work_requirements", "第11条"]
    第13条:
      rule: "修正を行ったら必ずビルドまで完全実行し、最終的なEXEの完全パスを出力する。"
      importance: "修正→ビルド→テスト→EXE確定の完全サイクル実行"
      mandatory: "例外なく実行すること - 修正のみで終了は厳禁"
      implementation:
        complete_workflow:
          step1: "修正実行（コード・設定・ドキュメント等）"
          step2: "Git同期: git add . && git commit -m '修正内容' && git push origin main"
          step3: "ビルド実行: dotnet clean, restore, build, publish実行"
          step4: "EXE生成確認とテスト実行"
          step5: "問題があれば即座に修正して step1 に戻る"
          step6: "問題がなくなった時点で最終EXEの完全パスを出力"
        build_commands:
          git_sync: "git pull origin main"
          build_execute: "dotnet clean && dotnet restore && dotnet build --configuration Release"
          publish_execute: "dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
          exe_test: "エクスプローラーからEXE起動テストと機能確認"
        final_output_requirement:
          format: "C:\\Users\\koki\\ezark\\standard-image\\Standard-image\\v2.2-taxdoc-organizer\\release\\DocOrganizer.exe"
          verification: "EXEファイル存在、サイズ（200MB前後）、作成日時（当日）"
          test_results: "起動成功、UI表示確認、ドラッグ&ドロップ動作確認"
          success_message: "✅ DocOrganizer V2.2 完成: [完全パス] - [ファイルサイズ]MB - [作成日時]"
        error_handling:
          build_error: "即座にエラー内容を分析し、修正してstep1に戻る"
          exe_missing: "publish設定確認、出力ディレクトリ確認、再実行"
          test_failure: "機能不具合の修正、再ビルド実行"
          infinite_loop_prevention: "同じエラーが3回連続で発生した場合は手動確認要求"
        completion_criteria:
          1: "EXEファイルが正常に生成されている"
          2: "EXE起動テストが成功している" 
          3: "基本機能（UI表示、ドラッグ&ドロップ）が動作している"
          4: "最終EXEの完全パスが出力されている"
        successful_execution_example:
          date: "2025-07-21"
          description: "V2.2 DocOrganizer初回成功事例"
          executed_steps:
            step1: "空画像ファイル問題修正（0バイトPNG削除）"
            step2: "Git同期完了 - 26ファイル更新（1,781行追加、195行削除）"
            step3: "MCP Windows環境接続成功（100.71.150.41:8080）"
            step4: "dotnet publish実行成功 - 自己完結型EXE生成"
            step5: "問題なし - 初回でビルド成功"
            step6: "✅ DocOrganizer V2.2 完成: C:\\builds\\DocOrganizer-repo\\v2.2-doc-organizer\\release\\DocOrganizer.UI.exe - 216.5MB - 2025-07-21 17:03:52"
          key_success_factors:
            - "MCP設定ファイル（.mcp.json）正常配置"
            - "Windows MCP Server正常稼働"
            - "プロジェクト構造適切（Clean Architecture）"
            - "依存関係解決成功（PDFsharp、SkiaSharp等）"
            - "WPF UI層正常コンパイル"
          generated_exe_specs:
            path: "C:\\builds\\DocOrganizer-repo\\v2.2-doc-organizer\\release\\DocOrganizer.UI.exe"
            size: "216.5MB"
            type: "自己完結型Windows実行ファイル"
            framework: ".NET 6.0"
            architecture: "win-x64"
            features: "CubePDF互換UI + 文書整理特化機能"
            startup_test: "成功（プロセスID: 17812）"
      related_sections: ["第12条", "mcp_environment_requirements", "execution_checklist"]
    第14条:
      rule: "Windowsアプリケーションはエクスプローラーから直接起動する。管理者権限での起動はドラッグ&ドロップ機能を無効化する。"
      importance: "ドラッグ&ドロップ機能の正常動作に必須 - Windowsセキュリティ制約の回避"
      mandatory: "例外なく守ること - 管理者権限起動は重大な機能制限を引き起こす"
      implementation:
        correct_startup_method:
          description: "正しいアプリケーション起動方法"
          procedure:
            - "エクスプローラーでEXEファイルの場所に移動"
            - "DocOrganizer.UI.exe を直接ダブルクリック"
            - "またはEXEファイルを右クリック→開く"
          location_examples:
            - "C:\\builds\\DocOrganizer-repo\\v2.2-doc-organizer\\release\\"
            - "デスクトップにショートカット作成して起動"
        incorrect_startup_methods:
          description: "ドラッグ&ドロップを無効化する起動方法"
          methods:
            - "管理者権限でコマンドプロンプトを開いてから起動"
            - "管理者権限でPowerShellから起動"
            - "Visual Studioから管理者権限で実行"
            - "右クリック→管理者として実行"
          consequence: "マウスカーソルが禁止マーク(⊘)になり、ドラッグ&ドロップが完全に無効化される"
      technical_background:
        windows_security_restriction:
          description: "Windows UAC（ユーザーアカウント制御）による権限分離セキュリティ"
          mechanism: "管理者権限プロセスは通常権限プロセス（エクスプローラー）からのドラッグ&ドロップを受け付けない"
          reason: "セキュリティ攻撃（特権昇格攻撃）の防止"
        privilege_level_mismatch:
          normal_privilege: "エクスプローラー（通常権限）→ファイルドラッグ可能"
          admin_privilege: "アプリケーション（管理者権限）→ドラッグ受信拒否"
          solution: "両方を同じ権限レベルで実行（通常権限推奨）"
      troubleshooting:
        symptom_identification:
          drag_drop_prohibition: "ファイルをドラッグした際にマウスカーソルが⊘マークになる"
          no_drop_overlay: "アプリケーション上でドロップオーバーレイが表示されない"
          ui_response_failure: "ドラッグ&ドロップイベントが一切発生しない"
        immediate_solution:
          step1: "現在のアプリケーションを終了"
          step2: "管理者権限のコマンドプロンプト/PowerShellを閉じる"
          step3: "エクスプローラーから直接EXEファイルを起動"
          verification: "サンプルファイルをドラッグして正常動作確認"
      testing_verification:
        test_procedure:
          description: "ドラッグ&ドロップ機能の動作確認手順"
          steps:
            step1: "エクスプローラーでsampleフォルダを開く"
            step2: "PNG、JPG、PDFファイルを選択"
            step3: "DocOrganizerのウィンドウにドラッグ"
            step4: "マウスカーソルがコピーアイコンに変わることを確認"
            step5: "ドロップオーバーレイが表示されることを確認"
            step6: "ドロップしてファイルが正常に読み込まれることを確認"
        success_indicators:
          cursor_change: "ドラッグ中にマウスカーソルがコピーアイコン(+)に変化"
          overlay_display: "アプリケーション上でドロップオーバーレイが青色で表示"
          file_processing: "ドロップ後に「X個のファイルを処理中...」メッセージ表示"
          thumbnail_generation: "左側パネルにページサムネイル生成"
        failure_indicators:
          prohibition_cursor: "ドラッグ中にマウスカーソルが禁止マーク(⊘)のまま"
          no_visual_feedback: "アプリケーション上で何も反応しない"
          event_not_triggered: "ドロップしても何も起こらない"
      prevention_guidelines:
        development_environment:
          description: "開発時の注意事項"
          rule: "Visual Studio起動時は通常権限で起動し、デバッグ実行も通常権限で行う"
          visual_studio_startup: "Visual Studioを右クリック→管理者として実行は避ける"
          debug_testing: "デバッグ中もドラッグ&ドロップ機能を随時テストする"
        deployment_documentation:
          description: "エンドユーザー向け説明"
          user_instruction: "アプリケーションは必ずエクスプローラーから起動してください"
          troubleshooting_guide: "ドラッグ&ドロップできない場合は管理者権限で起動していないか確認"
        documentation_requirement:
          readme_inclusion: "README.mdに起動方法の注意事項を必ず記載"
          user_manual_section: "ユーザーマニュアルにトラブルシューティング章を追加"
      success_case_record:
        date: "2025-07-22"
        description: "第13条制定の根拠となった実証成功事例"
        problem_identification:
          initial_symptom: "DocOrganizer V2.2でドラッグ&ドロップが一切動作しない"
          error_investigation: "コード、UI設定、イベントハンドラーは全て正常"
          root_cause_discovery: "管理者権限コマンドプロンプトからの起動が原因"
        solution_implementation:
          test_program_creation: "シンプルなテストプログラム（TestDragDrop）で問題再現"
          privilege_verification: "管理者権限 → 失敗、通常権限 → 成功を確認"
          production_application: "DocOrganizer本体も同様に成功確認"
        technical_validation:
          simple_test_success: "TestDragDrop.exe（シンプル版）で動作確認成功"
          full_application_success: "DocOrganizer.UI.exe（本格版）で動作確認成功"
          consistent_behavior: "両方のアプリケーションで同じ現象と解決方法を確認"
        lesson_learned: "Windows WPFアプリケーションの権限レベル制約は回避不可能な設計制約"
      related_sections: ["第13条", "exe_verification", "troubleshooting", "testing_verification"]
    第15条:
      rule: "バグを修正する場合は、serena mcpを利用して原因の分析をし、tmpフォルダ以下に報告資料を作成する。ユーザーに原因について報告する。すでに同様のバグの報告資料がある場合は、それを更新する。ユーザーが確認したら修正方法を提案する。修正方法が妥当か十分にレビューし、他の宣言に矛盾していないか確認した上でユーザーの確認をとり修正を実施する。バグ報告はドキュメントを作成し、tmpフォルダ以下に保存する。ユーザーがバグが解決したと言うまでドキュメントを残し、バグが解決したらドキュメントは削除する。"
      importance: "品質の高い修正と、ユーザーとの適切なコミュニケーションを確保"
      mandatory: "例外なく守ること - 即座の修正は予期しない副作用を引き起こす可能性がある"
      implementation:
        step1_analysis:
          description: "バグの原因を徹底的に分析"
          actions:
            - "エラーメッセージ、ログ、実行結果を確認"
            - "コードフローを追跡し、問題箇所を特定"
            - "根本原因と影響範囲を明確化"
        step2_report:
          description: "ユーザーに原因を報告"
          format: "## 原因分析結果\n1. 問題の症状\n2. 根本原因\n3. 影響範囲"
          wait_for: "ユーザーの確認と理解"
        step3_propose:
          description: "修正方法を提案"
          actions:
            - "複数の修正アプローチを検討"
            - "各アプローチのメリット・デメリットを明示"
            - "推奨する修正方法を理由付きで提案"
        step4_review:
          description: "修正方法の妥当性レビュー"
          checklist:
            - "他のコンポーネントへの影響確認"
            - "パフォーマンスへの影響評価"
            - "セキュリティリスクの検討"
            - "既存のCLAUDE.md原則との整合性確認"
        step5_confirm:
          description: "ユーザーの最終確認を取得"
          requirement: "修正実施前に必ずユーザーの明示的な承認を得る"
        step6_implement:
          description: "修正を実施"
          actions:
            - "承認された方法で修正を実装"
            - "テストを実施して動作確認"
            - "修正結果をユーザーに報告"
      example_workflow:
        bug_report: "PDFの回転が保存時に反映されない"
        analysis: "SavePdfAsyncで回転情報が渡されているが、ビルドが古い可能性を発見"
        user_confirmation: "分析結果を報告し、ユーザーが理解したことを確認"
        proposed_fix: "クリーンビルドと再パブリッシュを提案"
        review_result: "既存コードへの影響なし、第12条のビルド手順に準拠"
        user_approval: "ユーザーが修正方法を承認"
        implementation: "クリーンビルドを実行し、新しいEXEを生成"
      benefits:
        quality: "思慮深い修正により品質向上"
        communication: "ユーザーとの信頼関係構築"
        learning: "問題の理解が深まり、将来の問題予防に貢献"
        documentation: "修正履歴が明確になり、保守性向上"
      related_sections: ["第5条", "第7条", "troubleshooting", "quality_standards"]
    第16条:
      rule: "デバッグ用ログ出力は統一ファイル形式で実施する。ログファイルは release/DEBUG_LOG.txt に統一し、複数のログファイルを作成してはならない。デバッグログ機能を追加する場合は、AppendDebugLogAsync関数を使用して必ずDEBUG_LOG.txtに出力する。ログ出力は問題解決後に削除しないで残す。"
      importance: "統一的なデバッグ情報管理とトラブルシューティング効率化"
      mandatory: "例外なく守ること - ログファイルの分散は分析を困難にする"
      implementation:
        log_file_location: "C:\\Users\\217216X721451\\github\\DocOrganizer\\release\\DEBUG_LOG.txt"
        log_format: "[YYYY-MM-DD HH:MM:SS.fff] [機能名] メッセージ内容"
        function_template: |
          private async Task AppendDebugLogAsync(string message)
          {
              try
              {
                  var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                  var logPath = @"C:\Users\217216X721451\github\DocOrganizer\release\DEBUG_LOG.txt";
                  await System.IO.File.AppendAllTextAsync(logPath, logMessage + Environment.NewLine);
                  System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
              }
              catch { /* ログ出力エラーは無視 */ }
          }
        usage_examples:
          - "await AppendDebugLogAsync(\"[PreviewManagement] プレビュー更新開始\");"
          - "await AppendDebugLogAsync($\"[FileAddition] {files.Count}ファイル処理完了\");"
        prohibited_practices:
          - "新しいログファイル名の作成"
          - "Console.WriteLineのみでのログ出力"
          - "System.Diagnostics.Debug.WriteLineのみでのログ出力"
          - "ログファイルの削除（問題解決後も保持）"
      related_sections: ["第15条", "troubleshooting", "debugging_procedures"]
    
    file_organization_standards:
      description: "Mac・Windows両環境でのプロフェッショナルファイル構造基準"
      last_updated: "2025-07-24"
      mandatory: "全てのプロジェクト管理でこの基準に従うこと"
      
      mac_environment:
        base_path: "~/Documents/Projects/DocOrganizer/"
        structure:
          Production:
            description: "本番環境 - 配布版"
            contents:
              - "DocOrganizer/ - 最新本番アプリケーション"
              - "Documentation/ - ユーザーマニュアル・仕様書"
              - "Releases/ - バージョン別リリース"
          Development:
            description: "開発環境 - 作業中プロジェクト"
            contents:
              - "v2.2-current/ - 現在開発中版"
              - "testing/ - テスト環境"
              - "builds/ - ビルド作業用"
          Archive:
            description: "アーカイブ - 完成版保管"
            contents:
              - "v1.0-complete/ - V1完成版"
              - "v2.1-complete/ - V2.1完成版"
              - "experimental/ - 実験的実装"
          Resources:
            description: "共有リソース - 素材・ツール"
            contents:
              - "samples/ - サンプルファイル"
              - "templates/ - テンプレート"
              - "utilities/ - ユーティリティスクリプト"
      
      windows_environment:
        base_path: "C:\\builds\\DocOrganizer-repo\\"
        structure:
          Current:
            description: "現在運用版 - 本番利用"
            contents:
              - "DocOrganizer.exe - メイン実行ファイル"
              - "Config/ - 設定ファイル"
              - "Templates/ - 文書テンプレート"
              - "Logs/ - ログファイル"
          Development:
            description: "開発環境 - ソースコード"
            contents:
              - "Source/ - ソースコード"
              - "Build/ - ビルド作業用"
              - "Testing/ - テスト環境"
          Archive:
            description: "バージョン履歴 - 過去版保管"
            contents:
              - "v1.0/ - 旧バージョン保管"
              - "v2.1/ - 過去バージョン"
              - "v2.2-beta/ - ベータ版"
          Documentation:
            description: "ドキュメント - 仕様書・マニュアル"
            contents:
              - "UserManual.pdf - 利用者マニュアル"
              - "TechnicalSpecs.pdf - 技術仕様書"
              - "ReleaseNotes.md - リリースノート"
          SharedResources:
            description: "共有リソース - 素材・サンプル"
            contents:
              - "SampleFiles/ - サンプル画像・PDF"
              - "Templates/ - 定型テンプレート"
              - "Icons/ - アイコン素材"
      
      file_naming_standards:
        applications:
          - "DocOrganizer.exe - メインアプリケーション"
          - "DocOrganizer-v2.2.exe - バージョン付き"
          - "DocOrganizer-ImageSupport.exe - 機能付き"
        documentation:
          - "UserManual-v2.2.pdf - バージョン付きマニュアル"
          - "TechnicalSpecs-20250724.pdf - 日付付き仕様書"
          - "ReleaseNotes-v2.2.md - バージョン付きノート"
        development:
          - "Source/ - ソースコード"
          - "Build/ - ビルド用"
          - "Archive/ - アーカイブ"
          - "NO bin/, obj/, .vs/ - 開発用一時ファイル禁止"
      
      cleanup_rules:
        mandatory_deletion:
          - "bin/ フォルダ - ビルド生成物"
          - "obj/ フォルダ - 中間ファイル"
          - ".vs/ フォルダ - Visual Studio一時ファイル"
          - "*.user ファイル - ユーザー設定ファイル"
          - "temp*, tmp* - 一時ファイル"
        retention_policy:
          development: "現在開発中のみ保持"
          archive: "完成版のみ永続保管"
          resources: "必要最小限のサンプル・テンプレート"
      
      professional_standards:
        presentation_ready: "誰に対しても即座に見せられる状態"
        naming_consistency: "全ファイル・フォルダ名の統一性"
        documentation_complete: "README・マニュアル完備"
        security_compliance: "機密情報の適切な管理"
        maintenance_schedule: "月次での構造見直し"
      
      git_synchronization_standards:
        description: "Mac・Windows環境間のGit同期基準"
        last_updated: "2025-07-24"
        mandatory: "全ての変更作業で必須実行"
        
        synchronization_rules:
          before_work: "作業開始前に必ずgit pull origin main"
          during_work: "30分毎または機能完成毎にcommit"
          after_work: "作業完了時に必ずgit push origin main"
          environment_switch: "環境切り替え時に必ずgit pull origin main"
        
        commit_message_standards:
          format: "[環境] 変更内容の簡潔な説明"
          examples:
            - "[Mac] プロフェッショナル構造整理完了"
            - "[Windows] 最新ビルドとEXE生成"
            - "[共通] Claude.md整理規則追加"
            - "[機能] 画像ファイル対応実装完了"
        
        conflict_resolution:
          prevention: "作業前のgit pull必須実行"
          detection: "git status での状態確認"
          resolution: "競合発生時は両環境で相談して解決"
        
        branch_strategy:
          main_branch: "main - 安定版・本番用"
          development: "development - 開発中機能"
          feature: "feature/機能名 - 新機能開発"
        
        backup_policy:
          frequency: "重要変更前に必ずcommit"
          retention: "全履歴をGitで永続保管"
          recovery: "git log + git checkout で任意時点復元可能"
    
    project_structure:
      rule: "プロジェクト構造を常に以下に保つ（2025年7月24日更新）"
      structure:
        - "src/: ソースコード（Core/Business/UI/TestConsole）"
        - "scripts/: 実行スクリプト（build/test/mcp/utils）のみ"
        - "output/: 全てのPDF出力先（統一）"
        - "sample/: テスト画像（HEIC/JPG/PNG/jpeg）"
        - "tests/: テストプロジェクト"
        - "documents/: ドキュメント"
      output_policy: "PDFは必ずoutput/に出力。サブフォルダ作成は最小限に"

  cubepdf_reference:
    description: "CubePDF Utilityを参考にした設計指針"
    base_product: "CubePDF Utility（キューブ・ソフト社）"
    reference_reasons:
      - "日本国内で広く使用されているPDF編集ツール"
      - "シンプルで直感的なUI"
      - "安定した基本機能"
      - "多くのユーザーに馴染みがある"
    
    features_to_inherit:
      ui_layout:
        - "左側：ページサムネイル一覧"
        - "中央：プレビューエリア"
        - "上部：ツールバー"
        - "ドラッグ&ドロップ対応"
      
      basic_operations:
        - "PDF結合・分割"
        - "ページ回転・削除"
        - "ページ並び替え"
        - "セキュリティ設定"
      
      user_experience:
        - "シンプルな操作性"
        - "直感的なUI"
        - "高速な処理"
    
    enhancements:
      - "自動文書分類（領収書、請求書等）"
      - "向き自動補正（スキャン書類対応）"
      - "カスタムテンプレート"
      - "高度なファイル命名"
      - "フォルダ整理機能"
    
    differentiation_strategy:
      - "CubePDFの基本機能は全て網羅"
      - "文書整理に特化した追加機能"
      - "AI/機械学習による自動化"
      - "業務効率化に焦点"

  v2_reflection:
    description: "V2.0開発の反省点と教訓"
    failure_points:
      complexity_explosion:
        issue: "全機能を一度に実装しようとした"
        consequence: "複雑すぎて動作しない巨大なシステム"
        lesson: "MVPから始めて段階的に機能追加"
      
      interface_bloat:
        issue: "IStorageServiceに過剰な責務を持たせた"
        consequence: "実装が困難で、依存関係が複雑化"
        lesson: "Single Responsibility Principleの徹底"
      
      ui_coupling:
        issue: "ビジネスロジックとUIの分離不足"
        consequence: "UI層のエラーが多発"
        lesson: "クリーンアーキテクチャの徹底"
      
      test_absence:
        issue: "テストを後回しにした"
        consequence: "動作確認が困難、リグレッション多発"
        lesson: "Test Driven Development の採用"
      
      documentation_deficit:
        issue: "実装仕様が曖昧"
        consequence: "実装時の迷いと手戻り"
        lesson: "詳細設計書の事前作成"
    
    success_factors_from_v1:
      simple_architecture: "シンプルな3層構造"
      focused_features: "画像→PDF変換に特化"
      proven_components: "動作実績のあるライブラリ使用"
      clear_requirements: "実務に基づく明確な要件"

  v21_implementation_plan:
    description: "V2.1の段階的実装計画 - CubePDF Utilityベースの汎用ツール"
    core_concept: "CubePDF Utility互換機能から始めて、文書整理機能を段階的に追加"
    base_reference: "CubePDF Utilityの基本機能を参考に、文書整理に特化した機能を追加"
    
    development_principles:
      mvp_first: "最小限の価値ある製品から開始"
      test_driven: "テスト可能な設計を最優先"
      user_centric: "実業務フローに基づく設計"
      iterative: "短いサイクルでのリリースと改善"
    
    phase_overview:
      phase1:
        name: "Core PDF Operations (CubePDF Utility互換)"
        duration: "4週間"
        goal: "CubePDF Utilityと同等の基本PDF編集機能"
        deliverable: "CubePDF Utility互換の基本PDF編集ツール"
        features:
          - "PDFファイル読み込み・表示（CubePDF UI互換）"
          - "ページサムネイル生成（CubePDF スタイル）"
          - "ドラッグ&ドロップ並び替え（CubePDF 操作性）"
          - "PDF結合・分割（CubePDF 機能互換）"
          - "ページ回転・削除（CubePDF 同等機能）"
        cubepdf_compatibility:
          - "UI配置はCubePDF Utilityを参考"
          - "基本操作の互換性確保"
          - "ショートカットキーの互換性"
      
      phase2:
        name: "Document Organization Features (CubePDF拡張)"
        duration: "4週間"
        goal: "CubePDF Utilityにない文書整理機能の追加"
        deliverable: "CubePDF Utility + 文書整理機能のハイブリッドツール"
        features:
          - "高度なファイル名生成"
          - "文書タイプ設定"
          - "並び順テンプレート"
          - "フォルダ整理機能"
        differentiation_from_cubepdf:
          - "文書整理に特化した命名規則"
          - "カテゴリ別の自動分類"
          - "業務フローに最適化"
      
      phase3:
        name: "Orientation Detection"
        duration: "3週間"
        goal: "向き自動補正機能"
        deliverable: "向き自動補正対応版"
        features:
          - "V1の実績ある実装を活用"
          - "向き検出結果のプレビュー"
          - "一括向き修正"
          - "手動修正オプション"
      
      phase4:
        name: "Advanced Classification"
        duration: "4週間"
        goal: "AI文書分類機能"
        deliverable: "インテリジェント文書管理ツール"
        features:
          - "ルールベース分類"
          - "OCRテキスト抽出"
          - "キーワードマッチング"
          - "分類結果の学習機能"
      
      phase5:
        name: "Template System"
        duration: "3週間"
        goal: "カスタマイズ可能なテンプレート"
        deliverable: "完全な文書整理ツール"
        features:
          - "テンプレート作成・編集"
          - "カスタムルール設定"
          - "テンプレート適用プレビュー"

  v21_architecture:
    description: "V2.1のクリーンアーキテクチャ"
    layer_structure:
      presentation:
        technology: "WPF + Material Design"
        pattern: "MVVM"
        responsibilities:
          - "ユーザーインターフェース"
          - "ユーザー入力の処理"
          - "ViewModelとのバインディング"
      
      application:
        pattern: "Use Cases / Services"
        responsibilities:
          - "ビジネスロジックの調整"
          - "トランザクション管理"
          - "DTOマッピング"
      
      domain:
        pattern: "Domain Model"
        responsibilities:
          - "ビジネスルール"
          - "エンティティ"
          - "値オブジェクト"
      
      infrastructure:
        pattern: "Adapter"
        responsibilities:
          - "外部サービスとの統合"
          - "データ永続化"
          - "ファイルシステム操作"
    
    key_interfaces:
      IPdfService:
        purpose: "PDF操作の抽象化"
        methods:
          - "LoadPdfAsync"
          - "SavePdfAsync"
          - "MergePdfsAsync"
          - "SplitPdfAsync"
      
      IFileService:
        purpose: "ファイルシステム操作の抽象化"
        methods:
          - "FileExistsAsync"
          - "ReadAllBytesAsync"
          - "WriteAllBytesAsync"

  development_workflow:
    description: "V2.1開発の標準ワークフロー"
    daily_cycle:
      morning:
        - "前日の進捗確認"
        - "当日の目標設定"
        - "テストケース作成"
      
      coding:
        - "最小限の実装"
        - "テスト実行"
        - "リファクタリング"
      
      evening:
        - "動作確認"
        - "コミット"
        - "次の作業計画"
    
    quality_gates:
      before_commit:
        - "全テストがパス"
        - "ビルド成功"
        - "基本的な動作確認"
      
      before_merge:
        - "コードレビュー"
        - "統合テスト"
        - "パフォーマンステスト"
      
      before_release:
        - "ユーザー受け入れテスト"
        - "ドキュメント更新"
        - "リリースノート作成"

  current_project_status:
    v1_status:
      state: "✅ 完成・動作確認済み"
      location: "C:\\builds\\DocOrganizer-repo\\v1-image-to-pdf\\release\\DocOrganizer.UI.exe"
      features:
        - "画像→PDF変換（HEIC/JPG/PNG/JPEG）"
        - "汎用ファイル名生成"
        - "3段階圧縮"
        - "向き自動補正"
      recommendation: "画像→PDF変換にはV1を使用"
    
    v2_status:
      state: "❌ 廃止"
      issues:
        - "過度な複雑性により失敗"
        - "V2.1として再実装済み"
      recommendation: "使用しない"
    
    v21_status:
      state: "✅ Phase 1実装完了"
      features:
        - "基本的なPDF操作（読み込み・保存・回転・削除）"
        - "PDF結合・分割"
        - "Material Design UI"
        - "53個のテストで品質保証"
      issues:
        - "CubePDF Utilityとは異なるモダンUI"
      recommendation: "機能は完全だがUIが異なる"
    
    v22_status:
      state: "🚀 開発中"
      purpose: "CubePDF Utility互換UI版"
      changes:
        - "Material Design → Windows標準UI"
        - "CubePDFと同じ操作感"
        - "V2.1の機能をそのまま継承"
      location: "v2.2-doc-organizer/"
      next_action: "Windows標準UIでの再実装"

  next_steps:
    immediate_actions:
      - "V2.1プロジェクト作成スクリプトの実行"
      - "Phase 1モデルの実装"
      - "基本的なPDF読み込み機能の実装"
      - "最初のユニットテスト作成"
    
    week1_goals:
      - "Core層の完成"
      - "Infrastructure層の基本実装"
      - "テストカバレッジ80%達成"
    
    phase1_milestones:
      week1: "基盤実装完了"
      week2: "PDF基本操作実装"
      week3: "UI実装"
      week4: "統合・テスト・リリース"

  best_practices:
    coding:
      - "1機能1コミット"
      - "意味のあるコミットメッセージ"
      - "定期的なプッシュ"
    
    testing:
      - "Red-Green-Refactorサイクル"
      - "Arrange-Act-Assert パターン"
      - "モックの適切な使用"
    
    documentation:
      - "コードコメントは最小限"
      - "READMEの継続的更新"
      - "API変更の記録"
      - "V3アーキテクチャドキュメント: docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md"

  success_criteria:
    phase_completion:
      - "全機能が動作"
      - "テストカバレッジ80%以上"
      - "パフォーマンス基準達成"
      - "ユーザー受け入れ完了"
    
    overall_project:
      - "18週間での完成"
      - "業務効率70%向上"
      - "保守可能なコードベース"
      - "拡張可能なアーキテクチャ"

  active_bugs:
    heic_rotation_issue:
      description: "HEICファイルの回転編集機能が失敗する"
      issue_location: "tmp/DocOrganizer2.2_HEIC回転編集バグレポート_20250806.md"
      priority: "高"
      status: "調査中"
      reported_date: "2025-08-06"
      summary: "HEICファイルは読み込みとPDF生成は可能だが、90度回転などの編集操作が失敗する"

  mindset:
    philosophy:
      - "Perfect is the enemy of good - 完璧より動作を優先"
      - "Incremental improvement - 継続的な改善"
      - "User feedback driven - ユーザーフィードバック重視"
      - "Sustainable pace - 持続可能なペース"

  exe_verification:
    description: "実行可能ファイル（EXE）の確認ポリシー"
    principle: "常に最新版のEXEのみを確認・テストする"
    policy:
      latest_only: "過去のビルドは無視し、最新版のみをテスト対象とする"
      verification_steps:
        - "最新ビルドのパスを特定"
        - "エクスプローラーから起動テスト"
        - "プロセス確認とメモリ使用量チェック"
        - "正常終了の確認"
    current_latest_exe:
      v22: "C:\\Users\\koki\\ezark\\standard-image\\Standard-image\\v2.2-taxdoc-organizer\\release\\DocOrganizer.exe"
      v22_status: "完成版 - 自動アップデート機能付きPDF編集ツール"
    test_command: |
      # 最新版EXEの起動テスト（PowerShellで実行）
      cd C:\Users\koki\ezark\standard-image\Standard-image\v2.2-taxdoc-organizer\release
      $proc = Start-Process -FilePath "DocOrganizer.exe" -PassThru
      Write-Host "Process ID: $($proc.Id)"
      Start-Sleep -Seconds 5
      if (-not $proc.HasExited) {
        Write-Host "✅ EXE is running successfully"
        Stop-Process -Id $proc.Id -Force
      }

  windows_build_requirements:
    description: "Windows環境でのビルド要件"
    last_updated: "2025-01-24"
    mandatory: "WPFプロジェクトビルドにはWindows環境が必要"
    
    technical_requirements:
      dotnet_sdk: ".NET 6.0以上 + Windows Desktop SDK"
      build_tools: "Visual Studio Build Tools 2022以上"
      frameworks:
        - "Microsoft.NET.Sdk.WindowsDesktop対応"
        - "WPF (Windows Presentation Foundation)対応"
      runtime_identifiers:
        - "win-x64 (Windows 64-bit)"
    
    build_commands:
      windows_build_sequence:
        - "cd C:\\Users\\koki\\ezark\\standard-image\\Standard-image\\v2.2-taxdoc-organizer"
        - "git pull origin main"
        - "dotnet clean"
        - "dotnet restore"
        - "dotnet build --configuration Release"
        - "dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
      verification_command: "if exist release\\DocOrganizer.exe echo SUCCESS"
      
    priority: "最高優先度 - WPFプロジェクト完成に必須"
    related_sections: ["第4条", "第8条", "第10条", "exe_verification"]

# 以下、既存のセクションは変更なし（省略）
```