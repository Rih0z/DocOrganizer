# パフォーマンステストのベースライン設定

## 概要

パフォーマンステストの閾値を初回測定により決定し、CI/CDで継続的に監視する方法を定義します。

## 1. 測定対象と閾値

| ID | 測定対象 | 初回測定手順 | 閾値設定方法 | 期待値 |
|----|---------|------------|------------|--------|
| PT-001 | CalculateInsertIndex実行時間 | 1000回実行の平均 | 平均 + 2σ | 5ms以内 |
| PT-002 | FindParentListBox実行時間 | 1000回実行の平均 | 平均 + 2σ | 3ms以内 |
| PT-003 | メモリアロケーション | GC測定 | 実測値 + 20% | 10KB以内/回 |
| PT-004 | 1ページ回転処理時間 | 100回実行の平均 | 平均 + 2σ | 50ms以内 |
| PT-005 | 10ページ一括回転時間 | 10回実行の平均 | 平均 + 2σ | 500ms以内 |

## 2. BenchmarkDotNet設定

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[MarkdownExporter]
[HtmlExporter]
public class V3DragDropInfoBenchmarks
{
    private ListBox _listBox;
    private List<Point> _testPoints;

    [GlobalSetup]
    public void Setup()
    {
        _listBox = CreateListBoxWithItems(100);
        _testPoints = GenerateTestPoints(1000);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CalculateInsertIndex")]
    public void CalculateInsertIndex_Benchmark()
    {
        foreach (var point in _testPoints)
        {
            V3DragDropInfo.CalculateInsertIndex(_listBox, point);
        }
    }

    [Benchmark]
    [BenchmarkCategory("FindParentListBox")]
    public void FindParentListBox_Benchmark()
    {
        var item = _listBox.Items[50] as ListBoxItem;
        for (int i = 0; i < 1000; i++)
        {
            V3DragDropInfo.FindParentListBox(item);
        }
    }
}
```

## 3. 初回測定プロセス

### ステップ1: ローカル環境で測定

```bash
cd tests/DocOrganizer.Performance.Tests
dotnet run -c Release
```

### ステップ2: 結果分析

BenchmarkDotNetの出力例：
```
| Method                         | Mean     | StdDev   | Median   | Allocated |
|------------------------------- |---------:|---------:|---------:|----------:|
| CalculateInsertIndex_Benchmark | 3.245 ms | 0.124 ms | 3.210 ms |   8.12 KB |
| FindParentListBox_Benchmark    | 1.876 ms | 0.089 ms | 1.850 ms |   4.56 KB |
```

### ステップ3: 閾値決定

- **CalculateInsertIndex**: Mean + 2×StdDev = 3.245 + 2×0.124 = 3.493ms → 閾値: **5ms**（余裕を持たせる）
- **FindParentListBox**: Mean + 2×StdDev = 1.876 + 2×0.089 = 2.054ms → 閾値: **3ms**

### ステップ4: 閾値をテストに反映

```csharp
[Fact]
public void CalculateInsertIndex_Performance_ShouldBeUnder5ms()
{
    // 閾値: 5ms（初回測定: 平均3.245ms + 2σ）
    var threshold = TimeSpan.FromMilliseconds(5);

    await (() => RunCalculateInsertIndex1000Times())
        .ShouldCompleteWithinAsync(threshold);
}
```

## 4. CI/CDでの継続監視

### GitHub Actions設定

```yaml
- name: Run Performance Benchmarks
  run: |
    dotnet run --project tests/DocOrganizer.Performance.Tests/ `
      --configuration Release `
      --framework net8.0-windows `
      --exporters json markdown

- name: Upload Benchmark Results
  uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: BenchmarkDotNet.Artifacts/results/benchmarks.json
    github-token: ${{ secrets.GITHUB_TOKEN }}
    auto-push: true
    alert-threshold: '150%'
    comment-on-alert: true
```

### パフォーマンス回帰検出

- 前回のベースラインと比較して**150%以上**遅くなった場合は警告
- PR に自動コメントを投稿
- トレンドグラフを GitHub Pages で公開

## 5. まとめ

初回測定で閾値を決定し、CI/CDで継続的に監視することで、パフォーマンス回帰を早期に検出できます。
