using Atf.Core.Configuration;
using Atf.Core.Reporting;
using Atf.Core.Reporting.Artifacts;
using Atf.Core.Reporting.Phases;
using Atf.Reporting.Extent.Lifecycle;
using AventStack.ExtentReports;
using System.Reflection;

namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingLifecycleComponentTests
{
    [Fact(DisplayName = "Extent lifecycle delegates output identity to the path allocator contract")]
    public void LifecycleUsesPathAllocatorAbstraction()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-allocator-contract-");
        try
        {
            var allocator = new RecordingPathAllocator(new ExtentReportPathAllocator());
            var lifecycle = new ExtentReportLifecycle(allocator);
            var settings = new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            };

            lifecycle.Initialize(settings);

            Assert.NotNull(allocator.ReceivedSettings);
            Assert.Equal(settings.ReportPath, allocator.ReceivedSettings!.ReportPath);
            Assert.Equal(lifecycle.ReportPath, allocator.AllocatedPaths!.PublishedPath);
            lifecycle.FinalizeReport();
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle delegates final artifact publication to its publisher seam")]
    public void LifecycleUsesArtifactPublisherAbstraction()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-publisher-contract-");
        try
        {
            var publisher = new RecordingArtifactPublisher();
            var lifecycle = new ExtentReportLifecycle(new ExtentReportPathAllocator(), publisher);
            var settings = new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            };

            lifecycle.Initialize(settings);
            lifecycle.FinalizeReport();

            Assert.NotNull(publisher.Paths);
            Assert.Equal(lifecycle.ReportPath, publisher.Paths!.PublishedPath);
            Assert.Equal(settings.ReportName, publisher.Settings!.ReportName);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle requires initialization before exposing a report path")]
    public void ReportPathRequiresInitialization()
    {
        var lifecycle = new ExtentReportLifecycle();

        var exception = Assert.Throws<InvalidOperationException>(() => _ = lifecycle.ReportPath);

        Assert.Equal("Extent reporting has not been initialized.", exception.Message);
    }

    [Fact(DisplayName = "Extent lifecycle initializes only once until the current session is finalized")]
    public void InitializeIsIdempotentForActiveSession()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-idempotent-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            var settings = new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            };

            lifecycle.Initialize(settings);
            var firstPath = lifecycle.ReportPath;
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "a-different-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });

            Assert.Equal(firstPath, lifecycle.ReportPath);
            lifecycle.FinalizeReport();
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle finalization is idempotent and keeps the published artifact")]
    public void FinalizeIsIdempotent()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-finalize-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });
            using (lifecycle.StartTest("UsersApiTests", "GET users", "idempotent finalization", ["api"])) { }

            lifecycle.FinalizeReport();
            var reportPath = lifecycle.ReportPath;
            var firstHtml = File.ReadAllText(reportPath);
            lifecycle.FinalizeReport();

            Assert.True(File.Exists(reportPath));
            Assert.Equal(firstHtml, File.ReadAllText(reportPath));
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle releases the reserved path when publication fails")]
    public void FinalizeReleasesReservationAfterPublicationFailure()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-publish-failure-");
        try
        {
            var lifecycle = new ExtentReportLifecycle(
                new ExtentReportPathAllocator(),
                new ThrowingArtifactPublisher());
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });
            using (lifecycle.StartTest("UsersApiTests", "publish failure", "reservation release", ["api"])) { }

            var exception = Record.Exception(lifecycle.FinalizeReport);

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(0, ExtentReportPathAllocator.ReservedPathCount);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle rejects new scenarios after finalization")]
    public void StartTestAfterFinalizationIsRejected()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-finalized-start-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });

            lifecycle.FinalizeReport();

            var exception = Assert.Throws<InvalidOperationException>(() => lifecycle.StartTest(
                "UsersApiTests",
                "late scenario",
                "must not mutate a finalized session",
                ["api"]));

            Assert.Equal("Extent reporting is finalizing or has already been finalized.", exception.Message);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle tolerates an all-skipped run with no emitted scenario node")]
    public void FinalizeDoesNotFailWhenExtentHasNoNativeFile()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-empty-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "empty.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });

            var exception = Record.Exception(lifecycle.FinalizeReport);

            Assert.Null(exception);
            Assert.False(File.Exists(lifecycle.ReportPath));
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Extent lifecycle can be reused after finalization")]
    public void LifecycleCanStartANewSessionAfterFinalization()
    {
        var root = Directory.CreateTempSubdirectory("atf-lifecycle-reuse-");
        try
        {
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "first.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });
            using (lifecycle.StartTest("UsersApiTests", "FIRST SESSION", "first", ["api"])) { }
            lifecycle.FinalizeReport();
            var firstPath = lifecycle.ReportPath;

            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "second.html"),
                TimestampEnabled = false,
                Engine = "extent"
            });
            using (lifecycle.StartTest("UsersApiTests", "SECOND SESSION", "second", ["api"])) { }
            lifecycle.FinalizeReport();
            var secondPath = lifecycle.ReportPath;

            Assert.NotEqual(firstPath, secondPath);
            Assert.Contains("FIRST SESSION", File.ReadAllText(firstPath), StringComparison.Ordinal);
            Assert.DoesNotContain("SECOND SESSION", File.ReadAllText(firstPath), StringComparison.Ordinal);
            Assert.Contains("SECOND SESSION", File.ReadAllText(secondPath), StringComparison.Ordinal);
            Assert.DoesNotContain("FIRST SESSION", File.ReadAllText(secondPath), StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Collection fixture loads module configuration and initializes the report lifecycle")]
    public void FixtureInitializesInjectedLifecycle()
    {
        var configuration = Configuration();
        var loader = new RecordingConfigurationLoader(configuration);
        var lifecycle = new RecordingReportLifecycle();

        using var fixture = new ExtentCollectionFixture(loader, lifecycle);

        Assert.Same(configuration, fixture.Configuration);
        Assert.Same(configuration.Reporting, lifecycle.Reporting);
        Assert.Same(configuration.Api, lifecycle.Api);
        Assert.Equal(1, lifecycle.InitializeCount);
    }

    [Fact(DisplayName = "Collection fixture forwards the test identity and finalizes exactly once")]
    public void FixtureForwardsTestAndFinalizesOnDispose()
    {
        var lifecycle = new RecordingReportLifecycle();
        var fixture = new ExtentCollectionFixture(
            new RecordingConfigurationLoader(Configuration()),
            lifecycle);

        using (fixture.StartTest(
            "UsersApiTests",
            "GET users",
            "read-only contract",
            ["api", "smoke"]))
        {
        }

        fixture.Dispose();
        fixture.Dispose();

        Assert.Equal(("UsersApiTests", "GET users", "read-only contract"), lifecycle.StartedTest);
        Assert.Equal(["api", "smoke"], lifecycle.Categories);
        Assert.Equal(1, lifecycle.FinalizeCount);
    }

    [Fact(DisplayName = "Collection fixture forwards the test assembly to the scoped report session")]
    public void FixtureForwardsTestAssembly()
    {
        var lifecycle = new RecordingReportLifecycle();
        using var fixture = new ExtentCollectionFixture(
            new RecordingConfigurationLoader(Configuration()),
            lifecycle);

        fixture.RegisterTestAssembly(typeof(ReportingLifecycleComponentTests).Assembly);

        Assert.Same(typeof(ReportingLifecycleComponentTests).Assembly, lifecycle.TestAssembly);
    }

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

    [Fact(DisplayName = "Report sessions keep output paths and test trees isolated")]
    public void ReportSessionsAreIsolated()
    {
        var root = Directory.CreateTempSubdirectory("atf-session-");
        try
        {
            var firstPath = Path.Combine(root.FullName, "first.html");
            var secondPath = Path.Combine(root.FullName, "second.html");
            var first = new ExtentReportLifecycle();
            var second = new ExtentReportLifecycle();
            var firstSettings = new ReportingSettings
            {
                ReportPath = firstPath,
                TimestampEnabled = false,
                Engine = "extent"
            };
            var secondSettings = new ReportingSettings
            {
                ReportPath = secondPath,
                TimestampEnabled = false,
                Engine = "extent"
            };

            first.Initialize(firstSettings);
            using (first.StartTest("UsersApiTests", "FIRST SESSION", "first", ["api"])) { }
            first.FinalizeReport();

            second.Initialize(secondSettings);
            using (second.StartTest("UsersApiTests", "SECOND SESSION", "second", ["api"])) { }
            second.FinalizeReport();

            Assert.NotEqual(first.ReportPath, second.ReportPath);
            var firstHtml = File.ReadAllText(first.ReportPath);
            var secondHtml = File.ReadAllText(second.ReportPath);
            Assert.Contains("FIRST SESSION", firstHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("SECOND SESSION", firstHtml, StringComparison.Ordinal);
            Assert.Contains("SECOND SESSION", secondHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("FIRST SESSION", secondHtml, StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Parallel report sessions receive unique output paths even without timestamps")]
    public void ParallelReportSessionsUseUniquePaths()
    {
        var root = Directory.CreateTempSubdirectory("atf-session-parallel-");
        try
        {
            var settings = new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "parallel.html"),
                TimestampEnabled = false,
                Engine = "extent"
            };
            var first = new ExtentReportLifecycle();
            var second = new ExtentReportLifecycle();

            first.Initialize(settings);
            second.Initialize(settings);

            using (first.StartTest("ParallelFirst", "first", "first", ["api"])) { }
            using (second.StartTest("ParallelSecond", "second", "second", ["api"])) { }

            Assert.NotEqual(first.ReportPath, second.ReportPath);
            Assert.EndsWith(".html", first.ReportPath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".html", second.ReportPath, StringComparison.OrdinalIgnoreCase);

            first.FinalizeReport();
            second.FinalizeReport();
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Final HTML keeps the request-validation sibling hierarchy")]
    public void FinalHtmlPreservesRequestValidationSiblingHierarchy()
    {
        var root = Directory.CreateTempSubdirectory("atf-rendered-hierarchy-");
        try
        {
            var reportPath = Path.Combine(root.FullName, "api-atf-report.html");
            var lifecycle = new ExtentReportLifecycle();
            lifecycle.Initialize(new ReportingSettings
            {
                ReportPath = reportPath,
                TimestampEnabled = false,
                Engine = "extent"
            });

            using (var scope = lifecycle.StartTest(
                       "UsersApiTests",
                       "GET users",
                       "sibling validation contract",
                       ["api", "contract"]))
            {
                scope.StartStep(ApiScenarioPhaseLayout.TestBody);
                scope.StartStep("GET /public/v2/users");
                scope.Detail("response.status", "200");
                scope.Detail("response.duration.ms", "42");
                scope.Json("RESPONSE JSON", "{\"id\":1}");
                scope.StartStep("Validate: users collection contract");
                scope.AssertionTable([
                    new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
                ]);
                scope.CompleteStep(AtfReportStatus.Pass);
                scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);
                scope.CompleteScenario(AtfReportStatus.Pass);
            }

            lifecycle.FinalizeReport();

            var actualReportPath = lifecycle.ReportPath;
            var inspection = ReportArtifactInspector.Inspect(actualReportPath);
            Assert.True(inspection.IsCompliant, string.Join(", ", inspection.Violations));
            Assert.True(inspection.Satisfies("response-owned-execution-hierarchy"));

            var html = File.ReadAllText(actualReportPath);
            Assert.Contains("GET /public/v2/users", html, StringComparison.Ordinal);
            Assert.Contains("Validate: users collection contract", html, StringComparison.Ordinal);
            Assert.Contains(
                "validationCard.setAttribute('data-atf-parent-node', 'phase')",
                html,
                StringComparison.Ordinal);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    private static AtfConfiguration Configuration() => new()
    {
        Modules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Api"] = new AtfModuleSettings
            {
                Config = new AtfModuleConfig
                {
                    Api = new ApiSettings { BaseUrl = "https://example.test" },
                    Reporting = new ReportingSettings { Engine = "extent" }
                }
            }
        }
    };

    private sealed class RecordingConfigurationLoader(AtfConfiguration configuration) : IAtfConfigurationLoader
    {
        public AtfConfiguration Load(string? basePath = null) => configuration;
    }

    private sealed class RecordingReportLifecycle : IAtfReportSession
    {
        private readonly ExtentReports _reports = new();
        private ExtentTest? _test;

        public string ReportPath => "in-memory/report.html";
        public ReportingSettings? Reporting { get; private set; }
        public ApiSettings? Api { get; private set; }
        public int InitializeCount { get; private set; }
        public int FinalizeCount { get; private set; }
        public (string ClassName, string Scenario, string Description)? StartedTest { get; private set; }
        public IReadOnlyList<string> Categories { get; private set; } = [];

        public Assembly? TestAssembly { get; private set; }

        public void RegisterTestAssembly(Assembly testAssembly) => TestAssembly = testAssembly;

        public void Initialize(ReportingSettings settings, ApiSettings? api = null)
        {
            Reporting = settings;
            Api = api;
            InitializeCount++;
        }

        public IAtfScenarioScope StartTest(
            string testClassName,
            string scenarioName,
            string description,
            IEnumerable<string> categories)
        {
            StartedTest = (testClassName, scenarioName, description);
            Categories = categories.ToArray();
            _test = _reports.CreateTest(testClassName).CreateNode(scenarioName);
            return new ExtentTestScope(_test, testClassName);
        }

        public void Flush() { }

        public void FinalizeReport() => FinalizeCount++;
    }

    private sealed class RecordingPathAllocator(ExtentReportPathAllocator inner) : IExtentReportPathAllocator
    {
        public ReportingSettings? ReceivedSettings { get; private set; }
        public ExtentReportPaths? AllocatedPaths { get; private set; }

        public ExtentReportPaths Allocate(ReportingSettings settings)
        {
            ReceivedSettings = settings;
            AllocatedPaths = inner.Allocate(settings);
            return AllocatedPaths;
        }

        public void Release(ExtentReportPaths paths) => inner.Release(paths);
    }

    private sealed class RecordingArtifactPublisher : IExtentArtifactPublisher
    {
        public ExtentReportPaths? Paths { get; private set; }
        public ReportingSettings? Settings { get; private set; }

        public void Publish(ExtentReportPaths paths, ReportingSettings settings)
        {
            Paths = paths;
            Settings = settings;
        }
    }

    private sealed class ThrowingArtifactPublisher : IExtentArtifactPublisher
    {
        public void Publish(ExtentReportPaths paths, ReportingSettings settings) =>
            throw new InvalidOperationException("simulated publication failure");
    }
}

