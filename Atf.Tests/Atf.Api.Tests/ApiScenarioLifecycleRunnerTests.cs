using Atf.Api.Infrastructure;
using Atf.Common.Lifecycle;
using Xunit;

namespace Atf.Api.Tests;

public sealed class ApiScenarioLifecycleRunnerTests
{
    [Fact(DisplayName = "Lifecycle runner executes setup, body, teardown and cleanup in order")]
    public async Task RunsPhasesAndCleanupInDeterministicOrder()
    {
        var scope = new RecordingScenarioScope();
        var context = Context(scope);
        var events = new List<string>();
        context.Cleanup.Own("users", "1", () =>
        {
            events.Add("cleanup");
            return Task.CompletedTask;
        });

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            context,
            _ => Record(events, "setup"),
            _ => Record(events, "body"),
            _ => Record(events, "teardown"));

        Assert.True(result.IsPassed);
        Assert.Equal(["setup", "body", "teardown", "cleanup"], events);
        Assert.Equal(
            [
                "start:SETUP", "complete:SETUP:Pass",
                "start:TEST BODY", "complete:TEST BODY:Pass",
                "start:TEARDOWN",
                "start:Cleanup: DELETE users/1", "pass:Cleanup completed.",
                "complete-step:Pass",
                "complete:TEARDOWN:Pass"
            ],
            scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner keeps dynamic skip separate from failure")]
    public async Task ReturnsSkipAndClosesSkippedPhase()
    {
        var scope = new RecordingScenarioScope();
        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => throw new ApiScenarioSkipException("token missing"),
            teardown: null);

        Assert.True(result.IsSkipped);
        Assert.Equal("token missing", result.Skip!.Message);
        Assert.Null(result.Failure);
        Assert.Contains("skip:token missing", scope.Events);
        Assert.Contains("complete:TEST BODY:Skip", scope.Events);
    }

    [Theory(DisplayName = "Lifecycle runner preserves skipped phase and passing teardown")]
    [InlineData(true, "SETUP")]
    [InlineData(false, "TEST BODY")]
    public async Task SkipStillRunsTeardownAndKeepsPhaseVerdicts(bool skipSetup, string skippedPhase)
    {
        var scope = new RecordingScenarioScope();
        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: skipSetup
                ? _ => throw new ApiScenarioSkipException("phase disabled")
                : _ => Task.CompletedTask,
            body: skipSetup
                ? _ => Task.CompletedTask
                : _ => throw new ApiScenarioSkipException("phase disabled"),
            teardown: _ => Task.CompletedTask);

        Assert.True(result.IsSkipped);
        Assert.Contains($"complete:{skippedPhase}:Skip", scope.Events);
        Assert.Contains("complete:TEARDOWN:Pass", scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner keeps a body failure authoritative when teardown passes")]
    public async Task BodyFailureWithPassingTeardownRemainsFailure()
    {
        var scope = new RecordingScenarioScope();
        var bodyFailure = new InvalidOperationException("body failure");

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => throw bodyFailure,
            _ => Task.CompletedTask);

        Assert.True(result.IsFailed);
        Assert.Same(bodyFailure, result.Failure);
        Assert.Contains("complete:TEST BODY:Fail", scope.Events);
        Assert.Contains("complete:TEARDOWN:Pass", scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner preserves teardown skip after a passing body")]
    public async Task TeardownSkipAfterPassingBodyRemainsSkip()
    {
        var scope = new RecordingScenarioScope();
        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => Task.CompletedTask,
            _ => throw new ApiScenarioSkipException("teardown disabled"));

        Assert.True(result.IsSkipped);
        Assert.Contains("complete:TEST BODY:Pass", scope.Events);
        Assert.Contains("complete:TEARDOWN:Skip", scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner preserves the primary failure when teardown also fails")]
    public async Task PreservesPrimaryFailureAndReportsTeardownInfrastructureFailure()
    {
        var scope = new RecordingScenarioScope();
        var infrastructureFailures = new List<Exception>();
        var bodyFailure = new InvalidOperationException("body failure");

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => throw bodyFailure,
            _ => throw new InvalidOperationException("teardown failure"),
            infrastructureFailures.Add);

        Assert.Same(bodyFailure, result.Failure);
        Assert.False(result.IsSkipped);
        Assert.Single(infrastructureFailures);
        Assert.Equal("teardown failure", infrastructureFailures[0].Message);
        Assert.Contains("complete:TEST BODY:Fail", scope.Events);
        Assert.Contains("complete:TEARDOWN:Fail", scope.Events);
        var failed = Assert.IsType<ApiScenarioOutcome.Failed>(result.Outcome);
        Assert.Equal("Scenario execution failed.", failed.Reason);
    }

    [Fact(DisplayName = "Lifecycle runner always executes cleanup after teardown failure")]
    public async Task AlwaysRunsCleanupWhenTeardownFails()
    {
        var scope = new RecordingScenarioScope();
        var events = new List<string>();
        var teardownFailure = new InvalidOperationException("teardown failure");
        var context = Context(scope);
        context.Cleanup.Own("users", "1", () =>
        {
            events.Add("cleanup");
            return Task.CompletedTask;
        });

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            context,
            setup: null,
            _ => Record(events, "body"),
            _ => throw teardownFailure);

        Assert.True(result.IsFailed);
        Assert.Same(teardownFailure, result.Failure);
        Assert.Equal(["body", "cleanup"], events);
        Assert.Contains("complete:TEARDOWN:Fail", scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner executes cleanup when the body is skipped")]
    public async Task RunsCleanupWhenBodySkips()
    {
        var scope = new RecordingScenarioScope();
        var events = new List<string>();
        var context = Context(scope);
        context.Cleanup.Own("users", "1", () =>
        {
            events.Add("cleanup");
            return Task.CompletedTask;
        });

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            context,
            setup: null,
            _ => throw new ApiScenarioSkipException("feature disabled"),
            teardown: null);

        Assert.True(result.IsSkipped);
        Assert.Equal(["cleanup"], events);
        Assert.Contains("complete:TEARDOWN:Pass", scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner keeps the teardown failure when its callback throws")]
    public async Task CallbackFailureDoesNotReplaceTeardownFailure()
    {
        var scope = new RecordingScenarioScope();
        var teardownFailure = new InvalidOperationException("teardown failure");
        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => Task.CompletedTask,
            _ => throw teardownFailure,
            _ => throw new InvalidOperationException("callback failure"));

        Assert.True(result.IsFailed);
        Assert.Same(teardownFailure, result.Failure);
        Assert.Contains("warning:Teardown failure callback failed: callback failure", scope.Events);
    }

    [Fact(DisplayName = "Lifecycle runner cleans resources when teardown reporting cannot start")]
    public async Task RunsCleanupWhenTeardownStartStepFails()
    {
        var scope = new RecordingScenarioScope { ThrowOnTeardownStart = true };
        var events = new List<string>();
        var context = Context(scope);
        context.Cleanup.Own("users", "1", () =>
        {
            events.Add("cleanup");
            return Task.CompletedTask;
        });

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            context,
            setup: null,
            _ => Task.CompletedTask,
            _ => Record(events, "teardown"));

        Assert.True(result.IsPassed);
        Assert.Null(result.Failure);
        Assert.Equal(["teardown", "cleanup"], events);
    }

    [Fact(DisplayName = "Lifecycle runner keeps setup outcome when phase reporting fails")]
    public async Task PhaseReportingFailureDoesNotReplacePassingOutcome()
    {
        var scope = new RecordingScenarioScope
        {
            ThrowOnStart = true,
            ThrowOnCompletePhase = true
        };

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => Task.CompletedTask,
            teardown: null);

        Assert.True(result.IsPassed);
        Assert.Null(result.Failure);
    }

    [Fact(DisplayName = "Lifecycle runner keeps body failure when phase reporting fails")]
    public async Task PhaseReportingFailureDoesNotReplaceBodyFailure()
    {
        var scope = new RecordingScenarioScope
        {
            ThrowOnStart = true,
            ThrowOnCompletePhase = true
        };
        var bodyFailure = new InvalidOperationException("body failure");

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => throw bodyFailure,
            teardown: null);

        Assert.True(result.IsFailed);
        Assert.Same(bodyFailure, result.Failure);
    }

    [Fact(DisplayName = "Lifecycle outcome makes teardown failure precedence explicit")]
    public async Task TeardownFailurePromotesSkippedScenarioToFailure()
    {
        var scope = new RecordingScenarioScope();
        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => throw new ApiScenarioSkipException("feature disabled"),
            _ => throw new InvalidOperationException("cleanup unavailable"));

        var failed = Assert.IsType<ApiScenarioOutcome.Failed>(result.Outcome);
        Assert.Equal("Teardown failed.", failed.Reason);
        Assert.Equal("cleanup unavailable", failed.Exception.Message);
        Assert.False(result.IsSkipped);
    }

    [Fact(DisplayName = "Lifecycle outcome is a closed semantic pass value")]
    public async Task PassedOutcomeDoesNotCarryNullableState()
    {
        var scope = new RecordingScenarioScope();
        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => Task.CompletedTask,
            teardown: null);

        Assert.IsType<ApiScenarioOutcome.Passed>(result.Outcome);
        Assert.Null(result.Failure);
        Assert.Null(result.Skip);
    }

    [Fact(DisplayName = "Lifecycle runner exposes warning as a semantic outcome")]
    public async Task WarningOutcomeIsExplicitAndCannotHideInPass()
    {
        var scope = new RecordingScenarioScope();

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            setup: null,
            _ => Warn(scope, "body warning"),
            teardown: null);

        Assert.True(result.IsWarning);
        Assert.Equal("Scenario completed with warnings.", result.WarningReason);
        Assert.False(result.IsPassed);
        Assert.Null(result.Failure);
        Assert.Null(result.Skip);
    }

    [Theory(DisplayName = "Lifecycle matrix preserves phase status and verdict precedence: {0}")]
    [InlineData("setup fail / teardown pass", "fail", "pass", "pass", 0, 0)]
    [InlineData("setup fail / teardown warning", "fail", "pass", "warning", 0, 0)]
    [InlineData("setup fail / teardown fail", "fail", "pass", "fail", 0, 1)]
    [InlineData("setup warning / body fail / teardown pass", "warning", "fail", "pass", 0, 0)]
    [InlineData("setup warning / body fail / teardown warning", "warning", "fail", "warning", 0, 0)]
    [InlineData("setup warning / body fail / teardown fail", "warning", "fail", "fail", 0, 1)]
    [InlineData("setup warning / body pass / teardown fail", "warning", "pass", "fail", 0, 1)]
    [InlineData("setup pass / body warning / teardown fail", "pass", "warning", "fail", 0, 1)]
    [InlineData("body fail / teardown warning", "none", "fail", "warning", 0, 0)]
    [InlineData("body fail / teardown fail", "none", "fail", "fail", 0, 1)]
    [InlineData("body skip / teardown warning", "none", "skip", "warning", 1, 0)]
    [InlineData("body skip / teardown fail", "none", "skip", "fail", 1, 1)]
    [InlineData("setup skip / teardown skip", "skip", "pass", "skip", 2, 0)]
    [InlineData("setup skip / teardown fail", "skip", "pass", "fail", 1, 1)]
    [InlineData("teardown-only fail", "none", "pass", "fail", 0, 1)]
    [InlineData("warnings across all executed phases", "warning", "warning", "warning", 0, 0)]
    public async Task LifecycleMatrixPreservesPhaseStatusAndVerdict(
        string name,
        string setupShape,
        string bodyShape,
        string teardownShape,
        int expectedSkip,
        int expectedTeardownInfrastructureFailures)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));
        var scope = new RecordingScenarioScope();
        var infrastructureFailures = new List<Exception>();

        var result = await ApiScenarioLifecycleRunner.RunAsync(
            scope,
            Context(scope),
            Phase(setupShape, "SETUP", scope),
            Phase(bodyShape, "TEST BODY", scope)!,
            Phase(teardownShape, "TEARDOWN", scope),
            infrastructureFailures.Add);

        var expectedOutcome = setupShape is "fail"
            || (setupShape is not ("fail" or "skip") && bodyShape is "fail")
            || teardownShape is "fail"
                ? "Fail"
                : setupShape is "skip"
                    || (setupShape is not ("fail" or "skip") && bodyShape is "skip")
                    || teardownShape is "skip"
                        ? "Skip"
                        : setupShape is "warning"
                            || (setupShape is not ("fail" or "skip") && bodyShape is "warning")
                            || teardownShape is "warning"
                                ? "Warning"
                                : "Pass";

        var actualOutcome = result.Outcome switch
        {
            ApiScenarioOutcome.Passed => "Pass",
            ApiScenarioOutcome.Failed => "Fail",
            ApiScenarioOutcome.Skipped => "Skip",
            ApiScenarioOutcome.Warning => "Warning",
            _ => throw new InvalidOperationException("Unknown lifecycle outcome.")
        };

        Assert.Equal(expectedOutcome, actualOutcome);
        Assert.Equal(expectedSkip, scope.Events.Count(static value => value.StartsWith("skip:", StringComparison.Ordinal)));
        Assert.Equal(expectedTeardownInfrastructureFailures, infrastructureFailures.Count);

        if (setupShape is not "none")
        {
            Assert.Contains($"complete:SETUP:{StatusFor(setupShape)}", scope.Events);
        }

        if (setupShape is not ("fail" or "skip"))
        {
            Assert.Contains($"complete:TEST BODY:{StatusFor(bodyShape)}", scope.Events);
        }

        if (teardownShape is not "none")
        {
            Assert.Contains($"complete:TEARDOWN:{StatusFor(teardownShape)}", scope.Events);
        }

        var expectedWarnings =
            (setupShape is "warning" ? 1 : 0)
            + (setupShape is not ("fail" or "skip") && bodyShape is "warning" ? 1 : 0)
            + (teardownShape is "warning" ? 1 : 0);

        Assert.Equal(
            expectedWarnings,
            scope.Events.Count(static value => value.StartsWith("warning:", StringComparison.Ordinal)));
        Assert.Equal(
            expectedOutcome is "Fail",
            result.IsFailed);
        Assert.Equal(
            expectedOutcome is "Skip",
            result.IsSkipped);
        Assert.Equal(
            expectedOutcome is "Warning",
            result.IsWarning);
    }

    private static ApiScenarioContext Context(RecordingScenarioScope scope) =>
        new(
            new Atf.Core.Configuration.AtfConfiguration(),
            scope,
            null!,
            null!,
            new CleanupRegistry(scope));

    private static Task Record(List<string> events, string value)
    {
        events.Add(value);
        return Task.CompletedTask;
    }

    private static Func<ApiScenarioContext, Task>? Phase(
        string shape,
        string phase,
        RecordingScenarioScope scope)
        => shape switch
        {
            "none" => null,
            "pass" => _ => Task.CompletedTask,
            "warning" => _ => Warn(scope, $"{phase} warning"),
            "fail" => _ => throw new InvalidOperationException($"{phase} failure"),
            "skip" => _ => throw new ApiScenarioSkipException($"{phase} skip"),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown phase shape.")
        };

    private static Task Warn(RecordingScenarioScope scope, string message)
    {
        scope.Warning(message);
        return Task.CompletedTask;
    }

    private static string StatusFor(string shape) => shape switch
    {
        "fail" => AtfReportStatus.Fail.ToString(),
        "skip" => AtfReportStatus.Skip.ToString(),
        _ => AtfReportStatus.Pass.ToString()
    };

    private sealed class RecordingScenarioScope : IAtfScenarioScope
    {
        public List<string> Events { get; } = [];
        public bool HasWarnings { get; private set; }
        public bool ThrowOnTeardownStart { get; init; }
        public bool ThrowOnStart { get; init; }
        public bool ThrowOnCompletePhase { get; init; }
        public void StartStep(string title)
        {
            if ((ThrowOnTeardownStart && title == ApiScenarioPhaseLayout.Teardown) || ThrowOnStart)
            {
                throw new InvalidOperationException("teardown start failed");
            }

            Events.Add($"start:{title}");
        }
        public void CompleteStep(AtfReportStatus status) => Events.Add($"complete-step:{status}");
        public void CompletePhase(string phase, AtfReportStatus status)
        {
            if (ThrowOnCompletePhase)
            {
                throw new InvalidOperationException("complete phase reporting failed");
            }

            Events.Add($"complete:{phase}:{status}");
        }
        public void CompleteScenario(AtfReportStatus status) => Events.Add($"scenario:{status}");
        public void Detail(string key, string value) { }
        public void Json(string label, string json) { }
        public void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions) { }
        public void Info(string message) => Events.Add($"info:{message}");
        public void Pass(string message) => Events.Add($"pass:{message}");
        public void Fail(string message, Exception? exception = null) => Events.Add($"fail:{message}");
        public void Warning(string message)
        {
            HasWarnings = true;
            Events.Add($"warning:{message}");
        }
        public void Skip(string reason) => Events.Add($"skip:{reason}");
        public void PassTest(string message) { }
        public void FailTest(Exception exception) { }
        public void SkipTest(string reason) { }
        public void Dispose() { }
    }
}
