using Atf.Core.Configuration;
using Atf.Core.Reporting;
using AventStack.ExtentReports;
using System.Reflection;

namespace Atf.Core.Tests.Reporting;

public sealed class ReportingLifecycleComponentTests
{
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
            root.Delete(recursive: true);
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
}
