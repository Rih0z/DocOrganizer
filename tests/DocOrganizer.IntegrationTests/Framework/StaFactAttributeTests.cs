using System.Threading;
using Xunit;

namespace DocOrganizer.IntegrationTests.Framework;

/// <summary>
/// StaFactAttributeの機能検証テスト
/// Day 0品質検証の一環として、STAスレッド実行が正しく機能することを確認
/// </summary>
public class StaFactAttributeTests
{
    /// <summary>
    /// StaFactAttribute使用時、テストがSTAスレッドで実行されることを確認
    /// </summary>
    [StaFact]
    [Trait("Category", "Framework")]
    [Trait("Priority", "Critical")]
    public void StaFactAttribute_ShouldRunOnStaThread()
    {
        // Act - 現在のスレッドのアパートメント状態を取得
        var apartmentState = Thread.CurrentThread.GetApartmentState();

        // Assert - STAスレッドで実行されていることを確認
        Assert.Equal(ApartmentState.STA, apartmentState);
    }

    /// <summary>
    /// 通常のFactAttribute使用時、MTAスレッドで実行されることを確認（比較対照）
    /// </summary>
    [Fact]
    [Trait("Category", "Framework")]
    [Trait("Priority", "Critical")]
    public void FactAttribute_ShouldRunOnMtaThread()
    {
        // Act - 現在のスレッドのアパートメント状態を取得
        var apartmentState = Thread.CurrentThread.GetApartmentState();

        // Assert - MTA or Unknownスレッドで実行されていることを確認
        Assert.NotEqual(ApartmentState.STA, apartmentState);
    }
}
