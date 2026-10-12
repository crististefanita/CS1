using Atf.Core.Configuration;
using Atf.Core.Reporting;
using AventStack.ExtentReports;

namespace Atf.Core.Tests;

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

    [Fact(DisplayName = "Status aggregator clears state without affecting another instance")]
    public void StatusAggregatorClearIsIsolated()
    {
        var first = new ReportStatusAggregator();
        var second = new ReportStatusAggregator();

        first.Record("UsersApiTests", Status.Fail);
        second.Record("UsersApiTests", Status.Pass);
        first.Clear();

        Assert.False(first.TryGet("UsersApiTests", out _));
        Assert.True(second.TryGet("UsersApiTests", out var status));
        Assert.Equal(Status.Pass, status);
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

    private sealed class RecordingReportLifecycle : IAtfReportLifecycle
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

        public void Initialize(ReportingSettings settings, ApiSettings? api = null)
        {
            Reporting = settings;
            Api = api;
            InitializeCount++;
        }

        public ExtentTestScope StartTest(
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
