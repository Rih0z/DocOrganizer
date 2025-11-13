# DocOrganizer テストフレームワーク実装 - 基本設計書

## 概要

本ドキュメントは、要件定義フェーズで定義した要件を実現するための基本設計を記述する。

## 前フェーズからの引き継ぎ事項

### 要件定義フェーズの成果物

- **ドキュメント**: `docs/test/0_requirement/requirements.md`
- **メタデータ**: `docs/test/0_requirement/metadata.json`
- **レビュースコア**: 92/100点

### 主要決定事項

1. **V3.0.153の23箇所を100%検証**
   - トレーサビリティID: V153-001～V153-024

2. **V3.0.145-152の回帰防止**
   - トレーサビリティID: REG-001～REG-004

3. **核心機能5つの品質保証**
   - トレーサビリティID: CORE-001～CORE-025

4. **Phase定義**
   - Phase 1（必須）: 7時間、57テストケース、70%カバレッジ
   - Phase 2（推奨）: 2.5時間、12テストケース、+20%カバレッジ
   - Phase 3（オプション）: 4時間、8テストケース、+10%カバレッジ

5. **テストフレームワーク仮選定**: xUnit

## 本フェーズの成果物

### 1. テストプロジェクト構造設計

#### 1.1 全体構造

```
DocOrganizer/
├── src/                           # プロダクションコード
│   ├── DocOrganizer.Core/
│   ├── DocOrganizer.Application/
│   ├── DocOrganizer.Infrastructure/
│   └── DocOrganizer.UI/
│
├── tests/                         # テストコード（新規作成）
│   ├── DocOrganizer.Core.Tests/
│   │   ├── Models/
│   │   │   ├── PdfDocumentTests.cs
│   │   │   └── PdfPageTests.cs
│   │   └── DocOrganizer.Core.Tests.csproj
│   │
│   ├── DocOrganizer.Application.Tests/
│   │   ├── Services/
│   │   │   ├── PdfServiceTests.cs          # CORE-001～005
│   │   │   ├── RotationServiceTests.cs     # CORE-006～010
│   │   │   └── UndoRedoServiceTests.cs     # CORE-011～015
│   │   └── DocOrganizer.Application.Tests.csproj
│   │
│   ├── DocOrganizer.Infrastructure.Tests/
│   │   ├── Services/
│   │   │   ├── PdfEditorServiceTests.cs    # CORE-016～020
│   │   │   └── ImageLoaderServiceTests.cs
│   │   ├── Analyzers/
│   │   │   ├── V3_0_153_VerificationTests/ # V153-001～024
│   │   │   │   ├── V3DragDropInfoTests.cs  # V153-001～017
│   │   │   │   ├── PdfPerformanceMonitorTests.cs # V153-018
│   │   │   │   ├── SimpleDebugTestTests.cs # V153-019～020
│   │   │   │   ├── AppXamlTests.cs         # V153-021～023
│   │   │   │   └── PerformanceTests.cs     # V153-024
│   │   │   └── RegressionTests/            # REG-001～004
│   │   │       ├── PageOperationViewModelRegressionTests.cs # REG-001
│   │   │       ├── MainWindowRegressionTests.cs             # REG-002
│   │   │       ├── MainCompositeViewModelRegressionTests.cs # REG-003
│   │   │       └── IntegratedRegressionTests.cs             # REG-004
│   │   └── DocOrganizer.Infrastructure.Tests.csproj
│   │
│   ├── DocOrganizer.UI.Tests/
│   │   ├── ViewModels/
│   │   │   ├── V3DragDropInfoTests.cs      # CORE-021～025
│   │   │   ├── PageOperationViewModelTests.cs
│   │   │   └── MainCompositeViewModelTests.cs
│   │   ├── Integration/                    # Phase 3（オプション）
│   │   │   ├── AppStartupTests.cs          # IT-001～003
│   │   │   ├── DragDropIntegrationTests.cs # IT-004～006
│   │   │   └── RotationIntegrationTests.cs
│   │   └── DocOrganizer.UI.Tests.csproj
│   │
│   ├── DocOrganizer.Performance.Tests/     # Phase 2（推奨）
│   │   ├── Benchmarks/
│   │   │   ├── V3DragDropInfoBenchmarks.cs # PT-001～003
│   │   │   └── RotationServiceBenchmarks.cs # PT-004～005
│   │   └── DocOrganizer.Performance.Tests.csproj
│   │
│   ├── DocOrganizer.StaticAnalysis/        # Phase 1（必須）
│   │   ├── Analyzers/
│   │   │   ├── DebugCodeGuardAnalyzer.cs   # SA-001
│   │   │   └── DebugCodeGuardAnalyzer.Test.cs
│   │   ├── CodeFixes/
│   │   │   └── DebugCodeGuardCodeFixProvider.cs
│   │   └── DocOrganizer.StaticAnalysis.csproj
│   │
│   └── TestData/                           # テストデータ
│       ├── Pdfs/
│       │   ├── sample_10pages.pdf          # 10ページPDF（500KB）
│       │   ├── sample_empty.pdf            # 0ページPDF
│       │   ├── sample_corrupted.pdf        # 破損PDF
│       │   └── sample_rotated.pdf          # 回転済みPDF
│       ├── Images/
│       │   ├── sample.jpg
│       │   ├── sample.png
│       │   └── sample.heic
│       └── Expected/
│           ├── rotated_90.pdf
│           └── pages_reordered.pdf
│
├── docs/test/
│   ├── 0_requirement/                      # 要件定義（完了）
│   ├── 1_basic_design/                     # 基本設計（本フェーズ）
│   └── 2_detailed_design/                  # 詳細設計（次フェーズ）
│
└── .github/
    └── workflows/
        └── test.yml                        # CI/CD統合
```

#### 1.2 各プロジェクトの責務

| プロジェクト | 責務 | Phase |
|------------|------|-------|
| **DocOrganizer.Core.Tests** | Coreレイヤーのドメインモデルテスト | Phase 2 |
| **DocOrganizer.Application.Tests** | Applicationレイヤーのビジネスロジックテスト（核心機能） | Phase 1 |
| **DocOrganizer.Infrastructure.Tests** | Infrastructureレイヤーの技術的実装テスト（V3.0.153検証、回帰防止） | Phase 1 |
| **DocOrganizer.UI.Tests** | UIレイヤーのViewModelテスト（ドラッグ&ドロップ） | Phase 1 & 3 |
| **DocOrganizer.Performance.Tests** | パフォーマンステスト（ベンチマーク） | Phase 2 |
| **DocOrganizer.StaticAnalysis** | 静的解析ルール（Roslynアナライザー） | Phase 1 |

### 2. テストフレームワーク選定

#### 2.1 候補フレームワーク比較

| 観点 | xUnit | MSTest | NUnit |
|------|-------|--------|-------|
| **.NET統合** | ✅ 完全統合 | ✅ 完全統合 | ✅ 完全統合 |
| **Visual Studio統合** | ✅ 完璧 | ✅ 完璧 | ✅ 良好 |
| **並列実行** | ✅ デフォルト有効 | ⚠️ 設定必要 | ⚠️ 設定必要 |
| **BenchmarkDotNet統合** | ✅ 簡単 | ⚠️ 複雑 | ⚠️ 複雑 |
| **FluentAssertions統合** | ✅ 完璧 | ✅ 良好 | ✅ 良好 |
| **学習コスト** | 低（シンプル） | 低（従来型） | 中（多機能） |
| **コミュニティサポート** | ✅ 非常に活発 | ✅ Microsoft公式 | ✅ 活発 |
| **テスト実行速度** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **拡張性** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **推奨度** | **最高** | 高 | 高 |

#### 2.2 最終選定: xUnit

**選定理由**:
1. **並列実行がデフォルト**: テスト実行時間を大幅短縮（CI/CDで5分以内達成に貢献）
2. **BenchmarkDotNetとの統合が簡単**: Phase 2のパフォーマンステストで必須
3. **シンプルな構文**: `[Fact]`, `[Theory]` で学習コスト低
4. **コミュニティサポートが活発**: トラブルシューティングが容易
5. **FluentAssertionsとの相性抜群**: 可読性の高いテストコード

**使用例**:
```csharp
public class PdfServiceTests
{
    [Fact]
    public async Task LoadPdfAsync_ValidPdf_ReturnsCorrectPageCount()
    {
        // Arrange
        var pdfService = new PdfService();
        var filePath = "TestData/Pdfs/sample_10pages.pdf";

        // Act
        var document = await pdfService.LoadPdfAsync(filePath);

        // Assert
        document.Pages.Should().HaveCount(10);
    }

    [Theory]
    [InlineData("sample_10pages.pdf", 10)]
    [InlineData("sample_empty.pdf", 0)]
    public async Task LoadPdfAsync_VariousPdfs_ReturnsExpectedPageCount(
        string fileName, int expectedPageCount)
    {
        // Arrange
        var pdfService = new PdfService();
        var filePath = $"TestData/Pdfs/{fileName}";

        // Act
        var document = await pdfService.LoadPdfAsync(filePath);

        // Assert
        document.Pages.Should().HaveCount(expectedPageCount);
    }
}
```

### 3. 依存パッケージ定義

#### 3.1 各テストプロジェクトの共通パッケージ

**DocOrganizer.*.Tests.csproj** (Core, Application, Infrastructure, UI):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- テストフレームワーク -->
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <!-- アサーションライブラリ -->
    <PackageReference Include="FluentAssertions" Version="6.12.0" />

    <!-- モックライブラリ -->
    <PackageReference Include="Moq" Version="4.20.70" />

    <!-- カバレッジ -->
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <!-- テストヘルパー -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- プロダクションコード参照 -->
    <ProjectReference Include="..\..\src\DocOrganizer.Core\DocOrganizer.Core.csproj" />
    <ProjectReference Include="..\..\src\DocOrganizer.Application\DocOrganizer.Application.csproj" />
    <ProjectReference Include="..\..\src\DocOrganizer.Infrastructure\DocOrganizer.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

#### 3.2 DocOrganizer.UI.Tests固有のパッケージ

```xml
<ItemGroup>
  <!-- GUI自動化（Phase 3） -->
  <PackageReference Include="FlaUI.Core" Version="4.0.0" Condition="'$(IncludeIntegrationTests)' == 'true'" />
  <PackageReference Include="FlaUI.UIA3" Version="4.0.0" Condition="'$(IncludeIntegrationTests)' == 'true'" />

  <!-- WPFテストヘルパー -->
  <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />
</ItemGroup>
```

#### 3.3 DocOrganizer.Performance.Tests固有のパッケージ

```xml
<ItemGroup>
  <PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
  <PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.13.12" />
</ItemGroup>
```

#### 3.4 DocOrganizer.StaticAnalysis固有のパッケージ

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
  <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### 4. V3.0.153検証テストの設計

#### 4.1 テストアプローチ: ILコード解析

**戦略**: コンパイル済みアセンブリのILコードを解析し、`Debug.WriteLine`や`File.WriteAllText`が正しく除外されているか検証

**使用ライブラリ**: Mono.Cecil

**テストフロー**:
```
1. リリース版EXE読み込み
   ↓
2. 対象クラス・メソッドを取得（V153-001～024に対応）
   ↓
3. IL命令をスキャン
   ↓
4. Debug.WriteLine / File.WriteAllText の呼び出しが存在しないことを検証
   ↓
5. デバッグ版EXEでも同様にスキャン（存在することを検証）
```

#### 4.2 V3DragDropInfo.cs検証テスト（V153-001～017）

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Analyzers/V3_0_153_VerificationTests/V3DragDropInfoTests.cs`

**テストケース構成**:

```csharp
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using FluentAssertions;

namespace DocOrganizer.Infrastructure.Tests.Analyzers.V3_0_153_VerificationTests
{
    public class V3DragDropInfoTests
    {
        private const string ReleaseExePath = @"..\..\..\..\..\..\release\DocOrganizer.exe";
        private const string DebugExePath = @"..\..\..\..\..\..\release-debug\DocOrganizer.exe";

        [Fact]
        [Trait("Category", "V3.0.153-Verification")]
        [Trait("TraceabilityID", "V153-001-017")]
        public void V3DragDropInfo_ReleaseVersion_ShouldNotContainDebugWriteLine()
        {
            // Arrange
            using var module = ModuleDefinition.ReadModule(ReleaseExePath);
            var type = module.GetType("DocOrganizer.Infrastructure.DragDrop.V3DragDropInfo");
            type.Should().NotBeNull("V3DragDropInfo クラスが存在する必要があります");

            // Act
            var calculateInsertIndexMethod = type.Methods
                .FirstOrDefault(m => m.Name == "CalculateInsertIndex");
            calculateInsertIndexMethod.Should().NotBeNull();

            var debugWriteLineCalls = CountDebugWriteLineCalls(calculateInsertIndexMethod);

            // Assert
            debugWriteLineCalls.Should().Be(0,
                "リリース版では CalculateInsertIndex の 12箇所の Debug.WriteLine が除外されている必要があります");
        }

        [Fact]
        [Trait("Category", "V3.0.153-Verification")]
        [Trait("TraceabilityID", "V153-001-017")]
        public void V3DragDropInfo_DebugVersion_ShouldContainDebugWriteLine()
        {
            // Arrange
            using var module = ModuleDefinition.ReadModule(DebugExePath);
            var type = module.GetType("DocOrganizer.Infrastructure.DragDrop.V3DragDropInfo");

            // Act
            var calculateInsertIndexMethod = type.Methods
                .FirstOrDefault(m => m.Name == "CalculateInsertIndex");
            var debugWriteLineCalls = CountDebugWriteLineCalls(calculateInsertIndexMethod);

            // Assert
            debugWriteLineCalls.Should().Be(12,
                "デバッグ版では CalculateInsertIndex の 12箇所の Debug.WriteLine が含まれている必要があります");
        }

        [Theory]
        [InlineData("V153-001", 107)]
        [InlineData("V153-002", 115)]
        [InlineData("V153-003", 121)]
        [InlineData("V153-004", 129)]
        [InlineData("V153-005", 137)]
        [InlineData("V153-006", 147)]
        [InlineData("V153-007", 157)]
        [InlineData("V153-008", 163)]
        [InlineData("V153-009", 169)]
        [InlineData("V153-010", 177)]
        [InlineData("V153-011", 184)]
        [InlineData("V153-012", 186)]
        [Trait("Category", "V3.0.153-Verification-Detailed")]
        public void V3DragDropInfo_CalculateInsertIndex_DebugWriteLineAt_ShouldBeGuarded(
            string traceabilityId, int lineNumber)
        {
            // Arrange: ソースコードを読み込み
            var sourceCode = File.ReadAllText(
                @"..\..\..\..\..\..\src\DocOrganizer.Infrastructure\DragDrop\V3DragDropInfo.cs");

            // Act: 指定行の前後を取得
            var lines = sourceCode.Split('\n');
            var targetLine = lines[lineNumber - 1]; // 0-indexedに変換
            var previousLine = lines[lineNumber - 2];

            // Assert: #if DEBUG が直前にあることを検証
            previousLine.Trim().Should().Be("#if DEBUG",
                $"{traceabilityId}: {lineNumber}行目の Debug.WriteLine は #if DEBUG で囲まれている必要があります");
        }

        private int CountDebugWriteLineCalls(MethodDefinition method)
        {
            if (!method.HasBody) return 0;

            return method.Body.Instructions
                .Count(i => i.OpCode == OpCodes.Call &&
                           i.Operand is MethodReference mr &&
                           mr.DeclaringType.FullName == "System.Diagnostics.Debug" &&
                           mr.Name == "WriteLine");
        }
    }
}
```

#### 4.3 PdfPerformanceMonitor.cs検証テスト（V153-018）

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Analyzers/V3_0_153_VerificationTests/PdfPerformanceMonitorTests.cs`

```csharp
public class PdfPerformanceMonitorTests
{
    [Fact]
    [Trait("Category", "V3.0.153-Verification")]
    [Trait("TraceabilityID", "V153-018")]
    public void PdfPerformanceMonitor_ReleaseVersion_ShouldNotContainGenerateMonthlyReportAsync()
    {
        // Arrange
        using var module = ModuleDefinition.ReadModule(ReleaseExePath);
        var type = module.GetType("DocOrganizer.Infrastructure.Services.PdfPerformanceMonitor");

        // Act
        var method = type.Methods
            .FirstOrDefault(m => m.Name == "GenerateMonthlyReportAsync");

        // Assert
        if (method != null)
        {
            // メソッドは存在するが、中身が空（return Task.CompletedTask;のみ）であることを検証
            method.Body.Instructions.Should().HaveCountLessThan(5,
                "リリース版では GenerateMonthlyReportAsync は空メソッドである必要があります");
        }
    }

    [Fact]
    [Trait("Category", "V3.0.153-Verification")]
    [Trait("TraceabilityID", "V153-018")]
    public void PdfPerformanceMonitor_DebugVersionWithLogging_ShouldContainFileWriteAllTextAsync()
    {
        // Arrange: ENABLE_LOGGING定義でビルドされたEXEを読み込み
        // 注: このテストは ENABLE_LOGGING=true でビルドされた場合のみ実行
        var loggingEnabledExePath = @"..\..\..\..\..\..\release-debug-logging\DocOrganizer.exe";

        if (!File.Exists(loggingEnabledExePath))
        {
            // ENABLE_LOGGING版がビルドされていない場合はスキップ
            return;
        }

        using var module = ModuleDefinition.ReadModule(loggingEnabledExePath);
        var type = module.GetType("DocOrganizer.Infrastructure.Services.PdfPerformanceMonitor");

        // Act
        var method = type.Methods
            .FirstOrDefault(m => m.Name == "GenerateMonthlyReportAsync");

        var fileWriteCalls = method.Body.Instructions
            .Count(i => i.OpCode == OpCodes.Call &&
                       i.Operand is MethodReference mr &&
                       mr.DeclaringType.FullName == "System.IO.File" &&
                       mr.Name.Contains("WriteAllText"));

        // Assert
        fileWriteCalls.Should().BeGreaterThan(0,
            "ENABLE_LOGGING有効版では GenerateMonthlyReportAsync に File.WriteAllTextAsync が含まれている必要があります");
    }
}
```

#### 4.4 SimpleDebugTest.cs検証テスト（V153-019～020）

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Analyzers/V3_0_153_VerificationTests/SimpleDebugTestTests.cs`

```csharp
public class SimpleDebugTestTests
{
    [Fact]
    [Trait("Category", "V3.0.153-Verification")]
    [Trait("TraceabilityID", "V153-019-020")]
    public void SimpleDebugTest_ReleaseVersion_ShouldNotExist()
    {
        // Arrange
        using var module = ModuleDefinition.ReadModule(ReleaseExePath);

        // Act
        var type = module.GetType("DocOrganizer.Infrastructure.Services.SimpleDebugTest");

        // Assert
        type.Should().BeNull(
            "リリース版では SimpleDebugTest クラスは除外されている必要があります");
    }

    [Fact]
    [Trait("Category", "V3.0.153-Verification")]
    [Trait("TraceabilityID", "V153-019-020")]
    public void SimpleDebugTest_DebugVersionWithLogging_ShouldExist()
    {
        // Arrange
        var loggingEnabledExePath = @"..\..\..\..\..\..\release-debug-logging\DocOrganizer.exe";

        if (!File.Exists(loggingEnabledExePath))
        {
            return; // スキップ
        }

        using var module = ModuleDefinition.ReadModule(loggingEnabledExePath);

        // Act
        var type = module.GetType("DocOrganizer.Infrastructure.Services.SimpleDebugTest");

        // Assert
        type.Should().NotBeNull(
            "ENABLE_LOGGING有効版では SimpleDebugTest クラスが含まれている必要があります");

        // V153-019: WriteTestFile内のFile.WriteAllText
        var writeTestFileMethod = type.Methods.FirstOrDefault(m => m.Name == "WriteTestFile");
        writeTestFileMethod.Should().NotBeNull();

        // V153-020: Fallback内のFile.WriteAllText
        var fallbackMethod = type.Methods.FirstOrDefault(m => m.Name == "Fallback");
        fallbackMethod.Should().NotBeNull();
    }
}
```

#### 4.5 App.xaml.cs検証テスト（V153-021～023）

**ファイル**: `tests/DocOrganizer.UI.Tests/Analyzers/V3_0_153_VerificationTests/AppXamlTests.cs`

```csharp
public class AppXamlTests
{
    [Fact]
    [Trait("Category", "V3.0.153-Verification")]
    [Trait("TraceabilityID", "V153-021-023")]
    public void AppXaml_ReleaseVersion_ShouldNotContainDebugWriteLine()
    {
        // Arrange
        using var module = ModuleDefinition.ReadModule(ReleaseExePath);
        var type = module.GetType("DocOrganizer.App");

        // Act: App.xaml.csの全メソッドをスキャン
        var debugWriteLineCalls = type.Methods
            .Where(m => m.HasBody)
            .Sum(m => CountDebugWriteLineCalls(m));

        // Assert
        debugWriteLineCalls.Should().Be(0,
            "リリース版では App.xaml.cs の 3箇所の Debug.WriteLine が除外されている必要があります");
    }

    [Theory]
    [InlineData("V153-021", 72, "OnStartup")]
    [InlineData("V153-022", 257, "LoadButtonSizeSettings")]
    [InlineData("V153-023", 317, "UpdateButtonStyles")]
    [Trait("Category", "V3.0.153-Verification-Detailed")]
    public void AppXaml_DebugWriteLineAt_ShouldBeGuarded(
        string traceabilityId, int lineNumber, string methodName)
    {
        // Arrange: ソースコードを読み込み
        var sourceCode = File.ReadAllText(
            @"..\..\..\..\..\..\src\DocOrganizer.UI\App.xaml.cs");

        // Act: 指定行の前後を取得
        var lines = sourceCode.Split('\n');
        var targetLine = lines[lineNumber - 1];
        var previousLine = lines[lineNumber - 2];

        // Assert: #if DEBUG が直前にあることを検証
        previousLine.Trim().Should().Be("#if DEBUG",
            $"{traceabilityId}: {methodName}の{lineNumber}行目の Debug.WriteLine は #if DEBUG で囲まれている必要があります");
    }
}
```

#### 4.6 パフォーマンス検証テスト（V153-024）

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Analyzers/V3_0_153_VerificationTests/PerformanceTests.cs`

```csharp
public class PerformanceTests
{
    [Fact]
    [Trait("Category", "V3.0.153-Verification")]
    [Trait("TraceabilityID", "V153-024")]
    public async Task V3DragDropInfo_CalculateInsertIndex_ReleaseVersion_ShouldBeUnder5ms()
    {
        // Arrange
        var listBox = new ListBox();
        for (int i = 0; i < 100; i++)
        {
            listBox.Items.Add(new ListBoxItem { Content = $"Page {i}" });
        }

        var stopwatch = Stopwatch.StartNew();

        // Act: 1000回実行
        for (int i = 0; i < 1000; i++)
        {
            var point = new Point(100, 100 + i * 50);
            V3DragDropInfo.CalculateInsertIndex(listBox, point);
        }

        stopwatch.Stop();
        var averageMs = stopwatch.ElapsedMilliseconds / 1000.0;

        // Assert
        averageMs.Should().BeLessThan(5.0,
            "リリース版では CalculateInsertIndex は 1回あたり5ms未満で実行される必要があります");
    }
}
```

### 5. V3.0.145-152回帰テストの設計

#### 5.1 PageOperationViewModel回帰テスト（REG-001）

**ファイル**: `tests/DocOrganizer.UI.Tests/Analyzers/RegressionTests/PageOperationViewModelRegressionTests.cs`

```csharp
public class PageOperationViewModelRegressionTests
{
    [Fact]
    [Trait("Category", "Regression-Test")]
    [Trait("TraceabilityID", "REG-001")]
    public void RefreshPageListWithSelection_ShouldNotUsePagesClear()
    {
        // Arrange: ソースコードを静的解析
        var sourceCode = File.ReadAllText(
            @"..\..\..\..\..\..\src\DocOrganizer.UI\ViewModels\V3\PageOperationViewModel.cs");

        // Act: RefreshPageListWithSelectionメソッドを抽出
        var methodStart = sourceCode.IndexOf("private async Task RefreshPageListWithSelection");
        var methodEnd = sourceCode.IndexOf("}", methodStart + 1000);
        var methodBody = sourceCode.Substring(methodStart, methodEnd - methodStart);

        // Assert: Pages.Clear() が使用されていないことを検証
        methodBody.Should().NotContain("Pages.Clear()",
            "REG-001: RefreshPageListWithSelection で Pages.Clear() を使用してはいけません（選択が外れるため）");
    }

    [Fact]
    [Trait("Category", "Regression-Test")]
    [Trait("TraceabilityID", "REG-001")]
    public async Task RotatePage_ShouldMaintainSelection()
    {
        // Arrange
        var viewModel = new PageOperationViewModel();
        await viewModel.LoadPdfAsync("TestData/Pdfs/sample_10pages.pdf");

        // 5ページ目を選択
        viewModel.SelectedPages.Add(viewModel.Pages[4]);
        var selectedPageId = viewModel.Pages[4].Id;

        // Act: 回転実行
        await viewModel.RotateSelectedPagesAsync(90);

        // Assert: 5ページ目の選択が維持されている
        viewModel.SelectedPages.Should().ContainSingle(
            p => p.Id == selectedPageId,
            "REG-001: 回転後も選択が維持される必要があります");
    }
}
```

#### 5.2 MainWindow回帰テスト（REG-002）

**ファイル**: `tests/DocOrganizer.UI.Tests/Analyzers/RegressionTests/MainWindowRegressionTests.cs`

```csharp
public class MainWindowRegressionTests
{
    [Fact]
    [Trait("Category", "Regression-Test")]
    [Trait("TraceabilityID", "REG-002")]
    public void SyncSelectionFromViewModel_ShouldNotUseSelectedItemsClear()
    {
        // Arrange: ソースコードを静的解析
        var sourceCode = File.ReadAllText(
            @"..\..\..\..\..\..\src\DocOrganizer.UI\Views\MainWindow.xaml.cs");

        // Act: SyncSelectionFromViewModelメソッドを抽出
        var methodStart = sourceCode.IndexOf("private void SyncSelectionFromViewModel");
        var methodEnd = sourceCode.IndexOf("}", methodStart + 1000);
        var methodBody = sourceCode.Substring(methodStart, methodEnd - methodStart);

        // Assert: PageListBox.SelectedItems.Clear() が使用されていないことを検証
        methodBody.Should().NotContain("PageListBox.SelectedItems.Clear()",
            "REG-002: SyncSelectionFromViewModel で SelectedItems.Clear() を使用してはいけません");
        methodBody.Should().NotContain("SelectedItems.Clear()",
            "REG-002: SyncSelectionFromViewModel で SelectedItems.Clear() を使用してはいけません");
    }
}
```

#### 5.3 MainCompositeViewModel回帰テスト（REG-003）

**ファイル**: `tests/DocOrganizer.UI.Tests/Analyzers/RegressionTests/MainCompositeViewModelRegressionTests.cs`

```csharp
public class MainCompositeViewModelRegressionTests
{
    [Fact]
    [Trait("Category", "Regression-Test")]
    [Trait("TraceabilityID", "REG-003")]
    public void OnPageRotated_ShouldNotUseDirectIndexAssignment()
    {
        // Arrange: ソースコードを静的解析
        var sourceCode = File.ReadAllText(
            @"..\..\..\..\..\..\src\DocOrganizer.UI\ViewModels\V3\MainCompositeViewModel.cs");

        // Act: OnPageRotatedメソッドを抽出
        var methodStart = sourceCode.IndexOf("private void OnPageRotated");
        var methodEnd = sourceCode.IndexOf("}", methodStart + 500);
        var methodBody = sourceCode.Substring(methodStart, methodEnd - methodStart);

        // Assert: Pages[pageIndex] = e.Page が実行されていない（コメントアウトまたは削除）
        var activeAssignment = Regex.IsMatch(methodBody,
            @"^\s*Pages\[.*\]\s*=\s*e\.Page",
            RegexOptions.Multiline);

        activeAssignment.Should().BeFalse(
            "REG-003: OnPageRotated で Pages[pageIndex] = e.Page を実行してはいけません（冗長な更新のため）");
    }
}
```

#### 5.4 統合回帰テスト（REG-004）

**ファイル**: `tests/DocOrganizer.UI.Tests/Analyzers/RegressionTests/IntegratedRegressionTests.cs`

```csharp
public class IntegratedRegressionTests
{
    [Theory]
    [InlineData(1, "単一選択回転")]
    [InlineData(3, "複数選択回転")]
    [Trait("Category", "Regression-Test")]
    [Trait("TraceabilityID", "REG-004")]
    public async Task RotatePages_ShouldMaintainSelection_InAllScenarios(
        int selectedPageCount, string scenario)
    {
        // Arrange
        var viewModel = new PageOperationViewModel();
        await viewModel.LoadPdfAsync("TestData/Pdfs/sample_10pages.pdf");

        // ページを選択
        var selectedPageIds = new List<Guid>();
        for (int i = 0; i < selectedPageCount; i++)
        {
            var page = viewModel.Pages[i * 2]; // 0, 2, 4ページ目など
            viewModel.SelectedPages.Add(page);
            selectedPageIds.Add(page.Id);
        }

        // Act: 回転実行
        await viewModel.RotateSelectedPagesAsync(90);

        // Assert: 選択が維持されている
        viewModel.SelectedPages.Should().HaveCount(selectedPageCount,
            $"REG-004: {scenario} - 回転後も選択数が維持される必要があります");

        foreach (var pageId in selectedPageIds)
        {
            viewModel.SelectedPages.Should().Contain(p => p.Id == pageId,
                $"REG-004: {scenario} - 回転後も各ページの選択が維持される必要があります");
        }
    }

    [Fact]
    [Trait("Category", "Regression-Test")]
    [Trait("TraceabilityID", "REG-004")]
    public async Task RotatePages_AfterKeyboardNavigation_ShouldMaintainSelection()
    {
        // Arrange
        var viewModel = new PageOperationViewModel();
        await viewModel.LoadPdfAsync("TestData/Pdfs/sample_10pages.pdf");

        // キーボードナビゲーションをシミュレーション（5ページ目へ移動）
        viewModel.SelectedPages.Clear();
        viewModel.SelectedPages.Add(viewModel.Pages[4]);
        var selectedPageId = viewModel.Pages[4].Id;

        // Act: 回転実行
        await viewModel.RotateSelectedPagesAsync(90);

        // Assert: 選択が維持されている
        viewModel.SelectedPages.Should().ContainSingle(
            p => p.Id == selectedPageId,
            "REG-004: キーボードナビゲーション後の回転でも選択が維持される必要があります");
    }
}
```

### 6. 核心機能テストの設計

#### 6.1 PDF読み込みテスト（CORE-001～005）

**ファイル**: `tests/DocOrganizer.Application.Tests/Services/PdfServiceTests.cs`

```csharp
public class PdfServiceTests
{
    private readonly PdfService _sut; // System Under Test
    private readonly string _testDataPath = "TestData/Pdfs";

    public PdfServiceTests()
    {
        _sut = new PdfService();
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-001")]
    public async Task LoadPdfAsync_ValidPdf_ReturnsCorrectPageCount()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "sample_10pages.pdf");

        // Act
        var document = await _sut.LoadPdfAsync(filePath);

        // Assert
        document.Should().NotBeNull();
        document.Pages.Should().HaveCount(10,
            "CORE-001: 10ページのPDFを読み込むとページ数が10になる必要があります");
        document.Pages.Should().OnlyContain(p => p.Width > 0 && p.Height > 0,
            "CORE-001: 各ページの幅・高さが正しく設定されている必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-002")]
    public async Task LoadPdfAsync_EmptyPdf_ReturnsZeroPages()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "sample_empty.pdf");

        // Act
        var document = await _sut.LoadPdfAsync(filePath);

        // Assert
        document.Should().NotBeNull();
        document.Pages.Should().BeEmpty(
            "CORE-002: 0ページのPDFを読み込むとページ数が0になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-003")]
    public async Task LoadPdfAsync_CorruptedPdf_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "sample_corrupted.pdf");

        // Act
        Func<Task> act = async () => await _sut.LoadPdfAsync(filePath);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>(
            "CORE-003: 破損PDFを読み込むと適切な例外が発生する必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-004")]
    public async Task LoadPdfAsync_LargePdf_CompletesWithoutMemoryLeak()
    {
        // Arrange
        var filePath = GenerateLargePdf(1000); // 1000ページPDFを動的生成
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Act
        var document = await _sut.LoadPdfAsync(filePath);

        // Assert
        document.Pages.Should().HaveCount(1000,
            "CORE-004: 1000ページのPDFを正常に読み込める必要があります");

        // メモリリークチェック
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var memoryIncrease = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB

        memoryIncrease.Should().BeLessThan(100,
            "CORE-004: 1000ページPDF読み込み後のメモリ増加は100MB未満である必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-005")]
    public async Task LoadPdfAsync_MultipleConcurrentLoads_CompletesSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "sample_10pages.pdf");
        var tasks = new List<Task<PdfDocument>>();

        // Act: 10個のPDFを同時読み込み
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_sut.LoadPdfAsync(filePath));
        }

        var documents = await Task.WhenAll(tasks);

        // Assert
        documents.Should().HaveCount(10);
        documents.Should().OnlyContain(d => d.Pages.Count == 10,
            "CORE-005: 複数PDF同時読み込みでも正常に動作する必要があります");
    }

    private string GenerateLargePdf(int pageCount)
    {
        // PDFSharpで動的生成（詳細設計フェーズで実装）
        var outputPath = Path.Combine(Path.GetTempPath(), $"large_{pageCount}pages.pdf");
        // ... 生成ロジック ...
        return outputPath;
    }
}
```

#### 6.2 ページ回転テスト（CORE-006～010）

**ファイル**: `tests/DocOrganizer.Application.Tests/Services/RotationServiceTests.cs`

```csharp
public class RotationServiceTests
{
    private readonly RotationService _sut;
    private readonly Mock<IPdfDocument> _mockDocument;

    public RotationServiceTests()
    {
        _sut = new RotationService();
        _mockDocument = new Mock<IPdfDocument>();
    }

    [Theory]
    [InlineData(0, 90, "CORE-006")]
    [InlineData(0, 180, "CORE-007")]
    [InlineData(0, 270, "CORE-008")]
    [InlineData(270, 90, "CORE-009")] // 270 + 90 = 360 = 0
    [Trait("Category", "Core-Functionality")]
    public async Task RotatePageAsync_VariousDegrees_AppliesCorrectRotation(
        int initialRotation, int rotationDegree, string traceabilityId)
    {
        // Arrange
        var page = new PdfPage { Id = Guid.NewGuid(), Rotation = initialRotation };

        // Act
        await _sut.RotatePageAsync(page, rotationDegree);

        // Assert
        var expectedRotation = (initialRotation + rotationDegree) % 360;
        page.Rotation.Should().Be(expectedRotation,
            $"{traceabilityId}: {initialRotation}度のページを{rotationDegree}度回転すると{expectedRotation}度になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-010")]
    public async Task RotateMultiplePagesAsync_AllPagesRotatedCorrectly()
    {
        // Arrange
        var pages = Enumerable.Range(0, 10)
            .Select(i => new PdfPage { Id = Guid.NewGuid(), Rotation = 0 })
            .ToList();

        var stopwatch = Stopwatch.StartNew();

        // Act
        await _sut.RotateMultiplePagesAsync(pages, 90);

        stopwatch.Stop();

        // Assert
        pages.Should().OnlyContain(p => p.Rotation == 90,
            "CORE-010: 10ページ一括回転ですべてのページが90度になる必要があります");

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "CORE-010: 10ページ一括回転は500ms以内に完了する必要があります");
    }
}
```

#### 6.3 Undo/Redoテスト（CORE-011～015）

**ファイル**: `tests/DocOrganizer.Application.Tests/Services/UndoRedoServiceTests.cs`

```csharp
public class UndoRedoServiceTests
{
    private readonly UndoRedoService _sut;

    public UndoRedoServiceTests()
    {
        _sut = new UndoRedoService();
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-011")]
    public async Task Undo_AfterRotation_RestoresOriginalState()
    {
        // Arrange
        var page = new PdfPage { Id = Guid.NewGuid(), Rotation = 0 };
        var rotateCommand = new RotatePageCommand(page, 90);

        await _sut.ExecuteAsync(rotateCommand);
        page.Rotation.Should().Be(90);

        // Act
        await _sut.UndoAsync();

        // Assert
        page.Rotation.Should().Be(0,
            "CORE-011: 回転後Undoすると0度に戻る必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-012")]
    public async Task Redo_AfterUndo_RestoresRotatedState()
    {
        // Arrange
        var page = new PdfPage { Id = Guid.NewGuid(), Rotation = 0 };
        var rotateCommand = new RotatePageCommand(page, 90);

        await _sut.ExecuteAsync(rotateCommand);
        await _sut.UndoAsync();
        page.Rotation.Should().Be(0);

        // Act
        await _sut.RedoAsync();

        // Assert
        page.Rotation.Should().Be(90,
            "CORE-012: Undo後Redoすると90度に戻る必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-013")]
    public async Task MultipleUndo_RestoresInitialState()
    {
        // Arrange
        var page = new PdfPage { Id = Guid.NewGuid(), Rotation = 0 };

        await _sut.ExecuteAsync(new RotatePageCommand(page, 90));
        await _sut.ExecuteAsync(new RotatePageCommand(page, 90));
        await _sut.ExecuteAsync(new RotatePageCommand(page, 90));

        page.Rotation.Should().Be(270);

        // Act
        await _sut.UndoAsync();
        await _sut.UndoAsync();
        await _sut.UndoAsync();

        // Assert
        page.Rotation.Should().Be(0,
            "CORE-013: 3回回転後3回Undoすると初期状態に戻る必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-014")]
    public async Task UndoRedo_With100Operations_WorksCorrectly()
    {
        // Arrange
        var page = new PdfPage { Id = Guid.NewGuid(), Rotation = 0 };

        // 100回回転
        for (int i = 0; i < 100; i++)
        {
            await _sut.ExecuteAsync(new RotatePageCommand(page, 90));
        }

        page.Rotation.Should().Be(0); // 100回 × 90度 = 9000度 % 360 = 0度

        // Act: 100回Undo
        for (int i = 0; i < 100; i++)
        {
            await _sut.UndoAsync();
        }

        // Assert
        page.Rotation.Should().Be(0,
            "CORE-014: 100回操作後100回Undoしても正常に動作する必要があります");

        _sut.CanUndo.Should().BeFalse(
            "CORE-014: すべてUndoした後はCanUndoがfalseになる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-015")]
    public async Task Undo_AfterDeletePage_RestoresPage()
    {
        // Arrange
        var pages = new ObservableCollection<PdfPage>
        {
            new PdfPage { Id = Guid.NewGuid(), PageNumber = 1 },
            new PdfPage { Id = Guid.NewGuid(), PageNumber = 2 },
            new PdfPage { Id = Guid.NewGuid(), PageNumber = 3 }
        };

        var deletedPageId = pages[1].Id;
        var deleteCommand = new DeletePageCommand(pages, 1);

        await _sut.ExecuteAsync(deleteCommand);
        pages.Should().HaveCount(2);

        // Act
        await _sut.UndoAsync();

        // Assert
        pages.Should().HaveCount(3,
            "CORE-015: ページ削除後Undoするとページが復活する必要があります");
        pages[1].Id.Should().Be(deletedPageId,
            "CORE-015: 復活したページのIDは元のIDと一致する必要があります");
    }
}
```

#### 6.4 PDF保存テスト（CORE-016～020）

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Services/PdfEditorServiceTests.cs`

```csharp
public class PdfEditorServiceTests
{
    private readonly PdfEditorService _sut;
    private readonly string _testDataPath = "TestData/Pdfs";
    private readonly string _outputPath;

    public PdfEditorServiceTests()
    {
        _sut = new PdfEditorService();
        _outputPath = Path.Combine(Path.GetTempPath(), "DocOrganizerTests");
        Directory.CreateDirectory(_outputPath);
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-016")]
    public async Task SavePdfAsync_AfterRotation_SavesRotatedPage()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataPath, "sample_10pages.pdf");
        var outputPath = Path.Combine(_outputPath, "rotated_output.pdf");

        var document = await LoadAndRotatePage(inputPath, pageIndex: 4, rotation: 90);

        // Act
        await _sut.SavePdfAsync(document, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue(
            "CORE-016: PDF保存後にファイルが存在する必要があります");

        // 保存したPDFを読み込んで検証
        var savedDocument = await new PdfService().LoadPdfAsync(outputPath);
        savedDocument.Pages[4].Rotation.Should().Be(90,
            "CORE-016: 保存後のPDFで5ページ目が90度回転している必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-017")]
    public async Task SavePdfAsync_AfterDeletePage_SavesWithCorrectPageCount()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataPath, "sample_10pages.pdf");
        var outputPath = Path.Combine(_outputPath, "deleted_output.pdf");

        var document = await LoadAndDeletePage(inputPath, pageIndex: 4);

        // Act
        await _sut.SavePdfAsync(document, outputPath);

        // Assert
        var savedDocument = await new PdfService().LoadPdfAsync(outputPath);
        savedDocument.Pages.Should().HaveCount(9,
            "CORE-017: ページ削除後の保存でページ数が9になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-018")]
    public async Task SavePdfAsync_AfterReorderPages_SavesWithCorrectOrder()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataPath, "sample_10pages.pdf");
        var outputPath = Path.Combine(_outputPath, "reordered_output.pdf");

        var document = await LoadAndReorderPages(inputPath);

        // Act
        await _sut.SavePdfAsync(document, outputPath);

        // Assert
        var savedDocument = await new PdfService().LoadPdfAsync(outputPath);
        // 順序検証ロジック
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-019")]
    public async Task SavePdfAsync_Overwrite_SuccessfullyOverwrites()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataPath, "sample_10pages.pdf");
        var outputPath = Path.Combine(_outputPath, "overwrite_test.pdf");

        var document = await new PdfService().LoadPdfAsync(inputPath);
        await _sut.SavePdfAsync(document, outputPath);

        var originalFileInfo = new FileInfo(outputPath);
        await Task.Delay(100); // ファイルタイムスタンプが変わるのを待つ

        // Act: 上書き保存
        document.Pages[0].Rotation = 90;
        await _sut.SavePdfAsync(document, outputPath);

        // Assert
        var newFileInfo = new FileInfo(outputPath);
        newFileInfo.LastWriteTime.Should().BeAfter(originalFileInfo.LastWriteTime,
            "CORE-019: 上書き保存でファイルタイムスタンプが更新される必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-020")]
    public async Task SavePdfAsync_SaveAs_CreatesNewFileWithoutModifyingOriginal()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataPath, "sample_10pages.pdf");
        var outputPath = Path.Combine(_outputPath, "saveas_output.pdf");

        var document = await new PdfService().LoadPdfAsync(inputPath);
        var originalFileInfo = new FileInfo(inputPath);
        var originalLastWriteTime = originalFileInfo.LastWriteTime;

        document.Pages[0].Rotation = 90;

        // Act: 別名保存
        await _sut.SavePdfAsync(document, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue(
            "CORE-020: 別名保存で新しいファイルが作成される必要があります");

        var newOriginalFileInfo = new FileInfo(inputPath);
        newOriginalFileInfo.LastWriteTime.Should().Be(originalLastWriteTime,
            "CORE-020: 別名保存で元ファイルが変更されない必要があります");
    }
}
```

#### 6.5 ドラッグ&ドロップテスト（CORE-021～025）

**ファイル**: `tests/DocOrganizer.UI.Tests/ViewModels/V3DragDropInfoTests.cs`

```csharp
public class V3DragDropInfoTests
{
    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-021")]
    public void CalculateInsertIndex_EmptyListBox_ReturnsZero()
    {
        // Arrange
        var listBox = new ListBox();
        var point = new Point(100, 100);

        // Act
        var insertIndex = V3DragDropInfo.CalculateInsertIndex(listBox, point);

        // Assert
        insertIndex.Should().Be(0,
            "CORE-021: 空のListBoxではInsertIndexが0になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-022")]
    public void CalculateInsertIndex_DropOnTopHalf_ReturnsCurrentIndex()
    {
        // Arrange
        var listBox = CreateListBoxWithItems(10);
        var firstItem = listBox.Items[0] as ListBoxItem;
        var itemPosition = firstItem.TranslatePoint(new Point(0, 0), listBox);
        var dropPoint = new Point(itemPosition.X + 10, itemPosition.Y + 10); // 上半分

        // Act
        var insertIndex = V3DragDropInfo.CalculateInsertIndex(listBox, dropPoint);

        // Assert
        insertIndex.Should().Be(0,
            "CORE-022: 1ページ目の上半分にドロップするとInsertIndex=0になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-023")]
    public void CalculateInsertIndex_DropOnBottomHalf_ReturnsNextIndex()
    {
        // Arrange
        var listBox = CreateListBoxWithItems(10);
        var fifthItem = listBox.Items[4] as ListBoxItem;
        var itemPosition = fifthItem.TranslatePoint(new Point(0, 0), listBox);
        var itemHeight = fifthItem.ActualHeight;
        var dropPoint = new Point(itemPosition.X + 10, itemPosition.Y + itemHeight - 10); // 下半分

        // Act
        var insertIndex = V3DragDropInfo.CalculateInsertIndex(listBox, dropPoint);

        // Assert
        insertIndex.Should().Be(5,
            "CORE-023: 5ページ目の下半分にドロップするとInsertIndex=5になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-024")]
    public void CalculateInsertIndex_DropBelowLastItem_ReturnsPageCount()
    {
        // Arrange
        var listBox = CreateListBoxWithItems(10);
        var lastItem = listBox.Items[9] as ListBoxItem;
        var itemPosition = lastItem.TranslatePoint(new Point(0, 0), listBox);
        var itemHeight = lastItem.ActualHeight;
        var dropPoint = new Point(itemPosition.X + 10, itemPosition.Y + itemHeight + 50); // 最後のページより下

        // Act
        var insertIndex = V3DragDropInfo.CalculateInsertIndex(listBox, dropPoint);

        // Assert
        insertIndex.Should().Be(10,
            "CORE-024: 最後のページより下にドロップするとInsertIndex=ページ数になる必要があります");
    }

    [Fact]
    [Trait("Category", "Core-Functionality")]
    [Trait("TraceabilityID", "CORE-025")]
    public void FindParentListBox_NullInput_ReturnsNull()
    {
        // Arrange
        DependencyObject? nullObject = null;

        // Act
        var listBox = V3DragDropInfo.FindParentListBox(nullObject);

        // Assert
        listBox.Should().BeNull(
            "CORE-025: null入力の場合はnullを返す必要があります（例外を投げない）");
    }

    private ListBox CreateListBoxWithItems(int count)
    {
        var listBox = new ListBox();
        for (int i = 0; i < count; i++)
        {
            listBox.Items.Add(new ListBoxItem
            {
                Content = $"Page {i + 1}",
                Height = 150 // 固定高さ
            });
        }

        // ListBoxをレンダリング（テスト用）
        listBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        listBox.Arrange(new Rect(listBox.DesiredSize));

        return listBox;
    }
}
```

### 7. 静的解析ルール設計

#### 7.1 Roslynアナライザー実装

**ファイル**: `tests/DocOrganizer.StaticAnalysis/Analyzers/DebugCodeGuardAnalyzer.cs`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DocOrganizer.StaticAnalysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DebugCodeGuardAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdDA001 = "DA001";
        public const string DiagnosticIdDA002 = "DA002";

        private static readonly DiagnosticDescriptor RuleDA001 = new DiagnosticDescriptor(
            DiagnosticIdDA001,
            title: "Debug.WriteLine は #if DEBUG で囲む必要があります",
            messageFormat: "Debug.WriteLine の呼び出しは #if DEBUG ディレクティブで囲む必要があります",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Debug.WriteLine の呼び出しはリリースビルドで除外されるよう #if DEBUG で囲む必要があります。");

        private static readonly DiagnosticDescriptor RuleDA002 = new DiagnosticDescriptor(
            DiagnosticIdDA002,
            title: "File.WriteAllText は #if ENABLE_LOGGING で囲む必要があります",
            messageFormat: "File.WriteAllText の呼び出しは #if ENABLE_LOGGING ディレクティブで囲む必要があります",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "File.WriteAllText の呼び出しはリリースビルドで除外されるよう #if ENABLE_LOGGING で囲む必要があります。");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(RuleDA001, RuleDA002);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // tests/フォルダ内は除外
            if (context.Node.SyntaxTree.FilePath.Contains("\\tests\\"))
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            // Debug.WriteLine のチェック
            if (methodSymbol.ContainingType.ToString() == "System.Diagnostics.Debug" &&
                methodSymbol.Name == "WriteLine")
            {
                if (!IsGuardedByPreprocessorDirective(invocation, "DEBUG"))
                {
                    var diagnostic = Diagnostic.Create(RuleDA001, invocation.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // File.WriteAllText のチェック
            if (methodSymbol.ContainingType.ToString() == "System.IO.File" &&
                (methodSymbol.Name == "WriteAllText" || methodSymbol.Name == "WriteAllTextAsync"))
            {
                if (!IsGuardedByPreprocessorDirective(invocation, "ENABLE_LOGGING"))
                {
                    var diagnostic = Diagnostic.Create(RuleDA002, invocation.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool IsGuardedByPreprocessorDirective(SyntaxNode node, string directiveName)
        {
            // ノードの前後のトリビアからプリプロセッサディレクティブを探す
            var tree = node.SyntaxTree;
            var span = node.Span;

            // 現在のノードより前のすべてのトリビアを取得
            var triviaList = tree.GetRoot().DescendantTrivia(new Text.TextSpan(0, span.Start));

            // 最後の #if DEBUG または #if ENABLE_LOGGING を探す
            DirectiveTriviaSyntax? lastIfDirective = null;
            DirectiveTriviaSyntax? lastEndIfDirective = null;

            foreach (var trivia in triviaList.Reverse())
            {
                if (trivia.GetStructure() is DirectiveTriviaSyntax directive)
                {
                    if (directive is IfDirectiveTriviaSyntax ifDirective)
                    {
                        lastIfDirective = ifDirective;
                        break;
                    }
                    else if (directive is EndIfDirectiveTriviaSyntax endIfDirective)
                    {
                        lastEndIfDirective = endIfDirective;
                    }
                }
            }

            // #endif の後に #if がない場合は、ガードされていない
            if (lastEndIfDirective != null &&
                (lastIfDirective == null || lastEndIfDirective.SpanStart > lastIfDirective.SpanStart))
            {
                return false;
            }

            // #if DEBUG または #if ENABLE_LOGGING でガードされているか確認
            if (lastIfDirective is IfDirectiveTriviaSyntax ifDir)
            {
                var conditionText = ifDir.Condition.ToString().Trim();
                return conditionText == directiveName;
            }

            return false;
        }
    }
}
```

#### 7.2 静的解析テスト

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Analyzers/DebugCodeGuardAnalyzerTests.cs`

```csharp
public class DebugCodeGuardAnalyzerTests
{
    [Fact]
    [Trait("Category", "Static-Analysis")]
    [Trait("TraceabilityID", "SA-001")]
    public async Task Analyzer_UnguardedDebugWriteLine_ProducesDiagnostic()
    {
        // Arrange
        var testCode = @"
using System.Diagnostics;

public class TestClass
{
    public void TestMethod()
    {
        Debug.WriteLine(""test""); // ガードされていない
    }
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(testCode);

        // Assert
        diagnostics.Should().ContainSingle(
            d => d.Id == "DA001",
            "SA-001: ガードされていない Debug.WriteLine は DA001 診断を生成する必要があります");
    }

    [Fact]
    [Trait("Category", "Static-Analysis")]
    [Trait("TraceabilityID", "SA-001")]
    public async Task Analyzer_GuardedDebugWriteLine_ProducesNoDiagnostic()
    {
        // Arrange
        var testCode = @"
using System.Diagnostics;

public class TestClass
{
    public void TestMethod()
    {
#if DEBUG
        Debug.WriteLine(""test""); // ガードされている
#endif
    }
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(testCode);

        // Assert
        diagnostics.Should().NotContain(
            d => d.Id == "DA001",
            "SA-001: ガードされている Debug.WriteLine は診断を生成しない必要があります");
    }

    private async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string code)
    {
        // Roslynアナライザーのテストヘルパー（詳細設計フェーズで実装）
        var analyzer = new DebugCodeGuardAnalyzer();
        // ... アナライザー実行ロジック ...
        return ImmutableArray<Diagnostic>.Empty;
    }
}
```

#### 7.3 全量スキャンテスト（SA-002）

**ファイル**: `tests/DocOrganizer.Infrastructure.Tests/Analyzers/FullCodebaseScanTests.cs`

```csharp
public class FullCodebaseScanTests
{
    [Fact]
    [Trait("Category", "Static-Analysis")]
    [Trait("TraceabilityID", "SA-002")]
    public async Task FullCodebaseScan_AllSourceFiles_ShouldHaveZeroWarnings()
    {
        // Arrange
        var projectRoot = @"..\..\..\..\..\..\src";
        var sourceFiles = Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
            .ToList();

        sourceFiles.Should().NotBeEmpty("ソースファイルが存在する必要があります");

        var analyzer = new DebugCodeGuardAnalyzer();
        var allDiagnostics = new List<(string FilePath, Diagnostic Diagnostic)>();

        // Act: 全ファイルをスキャン
        foreach (var filePath in sourceFiles)
        {
            var code = await File.ReadAllTextAsync(filePath);
            var diagnostics = await GetDiagnosticsAsync(code, analyzer);

            foreach (var diagnostic in diagnostics)
            {
                allDiagnostics.Add((filePath, diagnostic));
            }
        }

        // Assert
        if (allDiagnostics.Any())
        {
            var report = GenerateReport(allDiagnostics);
            Assert.Fail($"SA-002: 未ガードのデバッグコードが{allDiagnostics.Count}件見つかりました:\n{report}");
        }

        allDiagnostics.Should().BeEmpty(
            "SA-002: 全ソースファイルで未ガードのデバッグコードが0件である必要があります");
    }

    private string GenerateReport(List<(string FilePath, Diagnostic Diagnostic)> diagnostics)
    {
        var sb = new StringBuilder();
        foreach (var (filePath, diagnostic) in diagnostics)
        {
            var relativePath = Path.GetRelativePath(
                @"..\..\..\..\..\..\src", filePath);
            var lineNumber = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
            sb.AppendLine($"{relativePath}:{lineNumber}: {diagnostic.Id}: {diagnostic.GetMessage()}");
        }
        return sb.ToString();
    }
}
```

### 8. CI/CD統合設計

#### 8.1 GitHub Actions ワークフロー

**ファイル**: `.github/workflows/test.yml`

```yaml
name: Test & Coverage

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: windows-latest
    timeout-minutes: 10

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        lfs: true

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build Release
      run: dotnet build --configuration Release --no-restore

    - name: Build Debug
      run: dotnet build --configuration Debug --no-restore

    - name: Run Phase 1 Tests (必須)
      run: |
        dotnet test tests/DocOrganizer.Application.Tests/ `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=test_results_app.trx" `
          --collect:"XPlat Code Coverage" `
          --filter "Category=Core-Functionality|Category=V3.0.153-Verification|Category=Regression-Test|Category=Static-Analysis"

        dotnet test tests/DocOrganizer.Infrastructure.Tests/ `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=test_results_infra.trx" `
          --collect:"XPlat Code Coverage" `
          --filter "Category=Core-Functionality|Category=V3.0.153-Verification|Category=Regression-Test|Category=Static-Analysis"

        dotnet test tests/DocOrganizer.UI.Tests/ `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=test_results_ui.trx" `
          --collect:"XPlat Code Coverage" `
          --filter "Category=Core-Functionality|Category=V3.0.153-Verification|Category=Regression-Test"
      timeout-minutes: 5

    - name: Upload Test Results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results
        path: '**/TestResults/*.trx'

    - name: Upload Coverage Reports
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: coverage-reports
        path: '**/TestResults/*/coverage.cobertura.xml'

    - name: Generate Coverage Report
      uses: danielpalme/ReportGenerator-GitHub-Action@5.2.0
      with:
        reports: '**/TestResults/*/coverage.cobertura.xml'
        targetdir: 'coverage-report'
        reporttypes: 'HtmlInline;Badges'

    - name: Upload Coverage HTML
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: coverage-html
        path: 'coverage-report/'

    - name: Check Coverage Threshold
      run: |
        $coverage = [xml](Get-Content '**/TestResults/*/coverage.cobertura.xml' | Select-Object -First 1)
        $lineRate = [double]$coverage.coverage.'line-rate' * 100
        Write-Host "Current Coverage: $lineRate%"

        if ($lineRate -lt 70) {
          Write-Error "Coverage $lineRate% is below threshold 70%"
          exit 1
        }
      shell: pwsh

  performance-test:
    runs-on: windows-latest
    timeout-minutes: 5
    needs: test
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'

    steps:
    - name: Checkout
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Run Performance Tests (Phase 2)
      run: |
        dotnet run --project tests/DocOrganizer.Performance.Tests/ `
          --configuration Release `
          --framework net8.0-windows

    - name: Upload Benchmark Results
      uses: actions/upload-artifact@v4
      with:
        name: benchmark-results
        path: '**/BenchmarkDotNet.Artifacts/**'
```

#### 8.2 テスト実行時間の最適化

**戦略**:

1. **並列実行**: xUnitのデフォルト並列実行を活用
2. **フィルタリング**: Phase 1のみCI/CDで実行（Phase 3はローカルのみ）
3. **キャッシュ**: NuGetパッケージキャッシュ、テストデータキャッシュ
4. **タイムアウト**: 各ジョブに5分のタイムアウト設定

**目標**: Phase 1テスト実行時間を5分以内に抑える

### 9. テストデータ準備

#### 9.1 リポジトリ管理データ

**場所**: `tests/TestData/`

**Git LFS設定** (`.gitattributes`):
```
tests/TestData/Pdfs/*.pdf filter=lfs diff=lfs merge=lfs -text
tests/TestData/Images/*.jpg filter=lfs diff=lfs merge=lfs -text
tests/TestData/Images/*.png filter=lfs diff=lfs merge=lfs -text
tests/TestData/Images/*.heic filter=lfs diff=lfs merge=lfs -text
```

#### 9.2 動的生成ヘルパー

**ファイル**: `tests/TestData/TestDataGenerator.cs`

```csharp
public static class TestDataGenerator
{
    public static string GenerateLargePdf(int pageCount)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_large_{pageCount}pages.pdf");

        using (var document = new PdfDocument())
        {
            for (int i = 0; i < pageCount; i++)
            {
                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210); // A4
                page.Height = XUnit.FromMillimeter(297);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    gfx.DrawString($"Page {i + 1}",
                        new XFont("Arial", 20),
                        XBrushes.Black,
                        new XRect(0, 0, page.Width, page.Height),
                        XStringFormats.Center);
                }
            }

            document.Save(outputPath);
        }

        return outputPath;
    }

    public static string GenerateCorruptedPdf(CorruptionType type)
    {
        // 破損PDFのバリエーション生成
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_corrupted_{type}.pdf");

        switch (type)
        {
            case CorruptionType.TruncatedFile:
                // ファイルの途中で切断
                File.WriteAllBytes(outputPath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // "%PDF"
                break;

            case CorruptionType.InvalidHeader:
                // 不正なヘッダー
                File.WriteAllText(outputPath, "This is not a PDF");
                break;

            // ... 他のケース ...
        }

        return outputPath;
    }
}

public enum CorruptionType
{
    TruncatedFile,
    InvalidHeader,
    MissingXref,
    InvalidObjects
}
```

### 10. 次フェーズへの引き継ぎ事項

#### 10.1 重要決定事項

1. **テストフレームワーク最終選定: xUnit**
   - 並列実行、BenchmarkDotNet統合、シンプルな構文

2. **テストプロジェクト構造確定**
   - 5つのテストプロジェクト（Core, Application, Infrastructure, UI, Performance）
   - 1つの静的解析プロジェクト

3. **V3.0.153検証方法: ILコード解析**
   - Mono.Cecilで Release/Debug のアセンブリ解析
   - トレーサビリティID（V153-001～024）による完全追跡

4. **V3.0.145-152回帰防止方法: 静的解析 + 動的テスト**
   - ソースコード静的解析で禁止パターン検出
   - 動的テストで選択維持を検証

5. **CI/CD統合: GitHub Actions**
   - Phase 1のみ自動実行（5分以内）
   - Phase 2はmain pushで実行
   - Phase 3はローカルのみ

#### 10.2 次フェーズ（詳細設計）での検討事項

以下は詳細設計フェーズで具体化する必要があります：

1. **各テストケースの詳細仕様**
   - テストデータの具体的な値
   - アサーションの詳細条件
   - モックの詳細設定

2. **テストヘルパークラスの設計**
   - テストデータ生成ヘルパー
   - アサーションヘルパー
   - モック生成ヘルパー

3. **パフォーマンステストの閾値決定**
   - 初回測定による実測値取得
   - ベースライン設定

4. **GUI統合テストの詳細実装方法**
   - FlaUIの具体的な使用方法
   - テスト環境のセットアップ

5. **CI/CDパイプラインの最適化**
   - キャッシュ戦略の詳細
   - 並列実行の詳細設定

#### 10.3 解決済みの課題

1. ✅ テストフレームワーク選定完了（xUnit）
2. ✅ テストプロジェクト構造設計完了
3. ✅ V3.0.153検証方法確定（ILコード解析）
4. ✅ V3.0.145-152回帰防止方法確定（静的解析+動的テスト）
5. ✅ CI/CD統合方法確定（GitHub Actions）
6. ✅ 依存パッケージ選定完了

#### 10.4 残存する課題

なし（すべて解決済み）

## 補足資料

### 参考リンク

1. **xUnit公式ドキュメント**: https://xunit.net/
2. **Mono.Cecil公式**: https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/
3. **Roslyn Analyzer チュートリアル**: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/
4. **BenchmarkDotNet公式**: https://benchmarkdotnet.org/
5. **FluentAssertions公式**: https://fluentassertions.com/
6. **GitHub Actions .NET**: https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net

### 用語集

| 用語 | 定義 |
|------|------|
| **IL (Intermediate Language)** | .NETの中間言語。C#コードがコンパイルされた後の形式 |
| **Mono.Cecil** | .NETアセンブリを読み書きするためのライブラリ |
| **Roslyn Analyzer** | C#コンパイラ（Roslyn）に統合されるコード解析ツール |
| **xUnit** | .NET用の単体テストフレームワーク |
| **FluentAssertions** | 可読性の高いアサーションライブラリ |
| **Moq** | .NET用のモックライブラリ |
| **BenchmarkDotNet** | .NET用のベンチマークフレームワーク |
| **FlaUI** | WPF/WinFormsアプリケーションのUI自動化ライブラリ |
| **coverlet** | .NET用のコードカバレッジツール |
| **Git LFS** | Git Large File Storage。大容量ファイルを効率的に管理 |

## まとめ

本基本設計フェーズにより、以下を確定しました：

### 確定事項

1. **テストフレームワーク**: xUnit（並列実行、BenchmarkDotNet統合）
2. **テストプロジェクト構造**: 5プロジェクト + 静的解析1プロジェクト
3. **V3.0.153検証方法**: Mono.CecilによるILコード解析
4. **V3.0.145-152回帰防止**: 静的解析 + 動的テスト
5. **CI/CD統合**: GitHub Actions（Phase 1: 5分以内）

### 次のステップ

詳細設計フェーズで以下を具体化：
- 各テストケースの詳細仕様（入力値、期待値、アサーション詳細）
- テストヘルパークラスの詳細設計
- パフォーマンステストのベースライン設定
- GUI統合テストの詳細実装方法

### 実装準備完了

基本設計が完了し、詳細設計フェーズへ進む準備が整いました。
