using Atf.Core.Configuration;
using Atf.Core.Reporting.Artifacts;
using Atf.Reporting.Extent.Lifecycle;

namespace Atf.Reporting.Extent.Tests;

public sealed class ExtentReportPathAllocatorTests
{
    [Fact(DisplayName = "Path allocator keeps native rendering private and publishes beside the report")]
    public void AllocatesPublishedAndNativePaths()
    {
        var root = Directory.CreateTempSubdirectory("atf-paths-");
        try
        {
            var allocator = new ExtentReportPathAllocator();
            var paths = allocator.Allocate(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false
            });

            Assert.Equal(Path.Combine(root.FullName, "api-report.html"), paths.PublishedPath);
            Assert.Equal(Path.Combine(root.FullName, ".atf-render-" + Path.GetFileName(paths.NativeDirectory), "api-report.html"), paths.NativePath);
            Assert.True(Directory.Exists(paths.NativeDirectory));
            Assert.StartsWith(root.FullName, paths.NativeDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("extent", paths.NativeDirectory, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Path allocator formats a timestamp using reporting settings")]
    public void AppliesConfiguredTimestamp()
    {
        var root = Directory.CreateTempSubdirectory("atf-path-timestamp-");
        try
        {
            var allocator = new ExtentReportPathAllocator(
                clock: () => new DateTime(2026, 10, 13, 14, 15, 16));

            var paths = allocator.Allocate(new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = true,
                TimestampFormat = "yyyyMMdd-HHmmss"
            });

            Assert.EndsWith("api-report_20261013-141516.html", paths.PublishedPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Path allocator resolves a relative report path through the project root")]
    public void ResolvesRelativePathThroughProjectRoot()
    {
        var root = Directory.CreateTempSubdirectory("atf-path-root-");
        try
        {
            var resolver = new ReportPathResolver(
                environment: name => name == "ATF_PROJECT_ROOT" ? root.FullName : null,
                currentDirectory: root.FullName,
                baseDirectory: root.FullName);
            var allocator = new ExtentReportPathAllocator(resolver);

            var paths = allocator.Allocate(new ReportingSettings
            {
                ReportPath = "artifacts/reports/api-report.html",
                TimestampEnabled = false
            });

            Assert.Equal(
                Path.Combine(root.FullName, "artifacts", "reports", "api-report.html"),
                paths.PublishedPath);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }

    [Fact(DisplayName = "Path allocator adds a run suffix when the published name is reserved")]
    public void AddsRunSuffixForCollision()
    {
        var root = Directory.CreateTempSubdirectory("atf-path-collision-");
        try
        {
            var allocator = new ExtentReportPathAllocator(
                runIdFactory: () => Guid.Parse("12345678-1234-1234-1234-123456789012"));
            var settings = new ReportingSettings
            {
                ReportPath = Path.Combine(root.FullName, "api-report.html"),
                TimestampEnabled = false
            };

            var first = allocator.Allocate(settings);
            var second = allocator.Allocate(settings);

            Assert.NotEqual(first.PublishedPath, second.PublishedPath);
            Assert.EndsWith("api-report_run-1234567812.html", second.PublishedPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TestDirectoryCleanup.DeleteEventually(root);
        }
    }
}
