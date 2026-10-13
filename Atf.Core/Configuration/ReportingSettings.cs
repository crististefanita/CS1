namespace Atf.Core.Configuration.Models;

using Atf.Core.Configuration.Tagging;

public sealed class ReportingSettings
{
    public string Engine { get; init; } = "extent";
    public string ReportPath { get; init; } = "artifacts/reports/api-atf-report.html";
    public bool TimestampEnabled { get; init; } = true;
    public string TimestampFormat { get; init; } = "yyyy-MM-dd_HH-mm-ss";
    public string DocumentTitle { get; init; } = "ATF API — Extent report";
    public string ReportName { get; init; } = "Configurable API Automation";
    public bool ConsoleDetails { get; init; } = true;
    public int AssertionMaxDisplayCharacters { get; init; } = ReportingDefaults.AssertionMaxDisplayCharacters;
    public string[] TestClassOrder { get; init; } = [];
    public string[] TestScenarioOrder { get; init; } = [];
    public string[] TagOrder { get; init; } = [];
    public TagTaxonomySettings TagTaxonomy { get; init; } = new();

    public ReportingSettings Snapshot() => new()
    {
        Engine = Engine,
        ReportPath = ReportPath,
        TimestampEnabled = TimestampEnabled,
        TimestampFormat = TimestampFormat,
        DocumentTitle = DocumentTitle,
        ReportName = ReportName,
        ConsoleDetails = ConsoleDetails,
        AssertionMaxDisplayCharacters = AssertionMaxDisplayCharacters,
        TestClassOrder = TestClassOrder.ToArray(),
        TestScenarioOrder = TestScenarioOrder.ToArray(),
        TagOrder = TagOrder.ToArray(),
        TagTaxonomy = TagTaxonomy.Snapshot()
    };
}
