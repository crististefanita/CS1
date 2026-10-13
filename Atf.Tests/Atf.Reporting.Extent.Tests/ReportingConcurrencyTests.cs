namespace Atf.Reporting.Extent.Tests;

public sealed class ReportingConcurrencyTests
{
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

    [Fact(DisplayName = "Overlapping lifecycle sessions keep nodes, status and native assets isolated")]
    public async Task OverlappingLifecycleSessionsKeepStateIsolated()
    {
        var root = Directory.CreateTempSubdirectory("atf-session-overlap-");
        using var barrier = new Barrier(2);

        try
        {
            var sessions = new[]
            {
                (Name: "OVERLAP PASS", File: "pass.html", Status: AtfReportStatus.Pass),
                (Name: "OVERLAP FAIL", File: "fail.html", Status: AtfReportStatus.Fail)
            };

            var tasks = sessions.Select(session => Task.Run(() =>
            {
                var lifecycle = new ExtentReportLifecycle();
                lifecycle.Initialize(new ReportingSettings
                {
                    ReportPath = Path.Combine(root.FullName, session.File),
                    TimestampEnabled = false,
                    Engine = "extent"
                });

                Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));

                using (var scope = lifecycle.StartTest(
                           "OverlappingSessions",
                           session.Name,
                           "interleaved lifecycle session",
                           ["api", "reporting"]))
                {
                    if (session.Status == AtfReportStatus.Pass)
                    {
                        scope.StartStep(ApiScenarioPhaseLayout.TestBody);
                        scope.StartStep("GET /public/v2/users");
                        scope.Detail("response.status", "200");
                        scope.StartStep("Validate: users collection contract");
                        scope.AssertionTable([
                            new AtfAssertion("HTTP status", "200 OK", "200 OK", true)
                        ]);
                        scope.CompleteStep(AtfReportStatus.Pass);
                        scope.CompletePhase(ApiScenarioPhaseLayout.TestBody, AtfReportStatus.Pass);
                    }
                    else
                    {
                        scope.FailTest(new InvalidOperationException("isolated failure"));
                    }

                    scope.CompleteScenario(session.Status);
                }

                Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
                lifecycle.FinalizeReport();
                return lifecycle.ReportPath;
            })).ToArray();

            var paths = await Task.WhenAll(tasks);

            Assert.Equal(2, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            var passHtml = File.ReadAllText(paths.Single(path => path.EndsWith("pass.html", StringComparison.OrdinalIgnoreCase)));
            var failHtml = File.ReadAllText(paths.Single(path => path.EndsWith("fail.html", StringComparison.OrdinalIgnoreCase)));
            Assert.Contains("OVERLAP PASS", passHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("OVERLAP FAIL", passHtml, StringComparison.Ordinal);
            Assert.Contains("OVERLAP FAIL", failHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("OVERLAP PASS", failHtml, StringComparison.Ordinal);
            Assert.Empty(Directory.GetDirectories(root.FullName, ".atf-render-*"));
            Assert.Equal(0, ExtentReportPathAllocator.ReservedPathCount);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }
}
