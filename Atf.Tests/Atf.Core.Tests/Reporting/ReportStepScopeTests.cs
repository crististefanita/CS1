using Atf.Core.Reporting;

namespace Atf.Core.Tests.Reporting;

public sealed class ReportStepScopeTests
{
    [Fact(DisplayName = "ReportStepScope always closes the step exactly once")]
    public void DisposeClosesAnUnfinishedStep()
    {
        var reporter = new RecordingReporter();

        using (var scope = new ReportStepScope(reporter, "GET /items"))
        {
            scope.Complete(AtfReportStatus.Pass);
        }

        Assert.Equal(["start:GET /items", "complete:Pass"], reporter.Events);
    }

    private sealed class RecordingReporter : IAtfReporter
    {
        public List<string> Events { get; } = [];

        public void StartStep(string title) => Events.Add($"start:{title}");
        public void CompleteStep(AtfReportStatus status) => Events.Add($"complete:{status}");
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) { }
        public void Pass(string message) { }
        public void Fail(string message, Exception? exception = null) { }
        public void Warning(string message) { }
        public void Skip(string reason) { }
    }
}
