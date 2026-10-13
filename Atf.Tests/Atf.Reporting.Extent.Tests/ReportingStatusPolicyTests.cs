namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingStatusPolicyTests
{
    [Fact(DisplayName = "Status aggregator clears state without affecting another instance")]
    public void StatusAggregatorClearIsIsolated()
    {
        var first = new ReportStatusAggregator();
        var second = new ReportStatusAggregator();

        first.Record("UsersApiTests", AtfReportStatus.Fail);
        second.Record("UsersApiTests", AtfReportStatus.Pass);
        first.Clear();

        Assert.False(first.TryGet("UsersApiTests", out _));
        Assert.True(second.TryGet("UsersApiTests", out var status));
        Assert.Equal(AtfReportStatus.Pass, status);
    }

    [Theory(DisplayName = "Status precedence keeps the strongest verdict: {0} then {1} => {2}")]
    [InlineData(AtfReportStatus.Warning, AtfReportStatus.Pass, AtfReportStatus.Warning)]
    [InlineData(AtfReportStatus.Pass, AtfReportStatus.Warning, AtfReportStatus.Warning)]
    [InlineData(AtfReportStatus.Warning, AtfReportStatus.Fail, AtfReportStatus.Fail)]
    [InlineData(AtfReportStatus.Fail, AtfReportStatus.Warning, AtfReportStatus.Fail)]
    [InlineData(AtfReportStatus.Warning, AtfReportStatus.Skip, AtfReportStatus.Skip)]
    public void StatusPrecedenceKeepsStrongestVerdict(
        AtfReportStatus first,
        AtfReportStatus second,
        AtfReportStatus expected)
    {
        var aggregator = new ReportStatusAggregator();

        Assert.Equal(first, aggregator.Record("UsersApiTests", first));
        Assert.Equal(expected, aggregator.Record("UsersApiTests", second));
    }

    [Fact(DisplayName = "Warning then failure then pass remains a failure")]
    public void WarningFailurePassSequenceRemainsFailure()
    {
        var aggregator = new ReportStatusAggregator();

        aggregator.Record("UsersApiTests", AtfReportStatus.Warning);
        aggregator.Record("UsersApiTests", AtfReportStatus.Fail);

        Assert.Equal(AtfReportStatus.Fail, aggregator.Record("UsersApiTests", AtfReportStatus.Pass));
    }
}
