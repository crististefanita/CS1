using AventStack.ExtentReports;
using Atf.Core.Security;
using System.Net;

namespace Atf.Reporting.Extent;

/// <summary>
/// Encapsulates the Extent test tree: class roots contain scenario nodes and
/// class status is aggregated from those scenario results. The manager only
/// coordinates lifecycle and locking; it does not own tree semantics.
/// </summary>
internal sealed class ExtentTestHierarchy
{
    private readonly object _sync;
    private readonly Dictionary<string, ExtentTest> _classTests = new(StringComparer.Ordinal);
    private readonly ReportStatusAggregator _classStatuses = new();

    public ExtentTestHierarchy(object synchronizationGate)
    {
        _sync = synchronizationGate ?? throw new ArgumentNullException(nameof(synchronizationGate));
    }

    public ExtentTestScope Start(
        ExtentReports reports,
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters,
        SensitiveDataRedactor? redactor = null)
    {
        lock (_sync)
        {
            ArgumentNullException.ThrowIfNull(reports);
            ArgumentException.ThrowIfNullOrWhiteSpace(testClassName);
            ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
            ArgumentNullException.ThrowIfNull(categories);

            if (!_classTests.TryGetValue(testClassName, out var classTest))
            {
                classTest = reports.CreateTest(testClassName, description);
                _classTests[testClassName] = classTest;
            }

            var scenario = classTest.CreateNode(WebUtility.HtmlEncode(scenarioName), description);
            // Categories describe the executable scenario, not the shared
            // class container. Assigning them to the class root makes Extent
            // propagate the union of every scenario tag to every sibling and
            // corrupts status-aware Overview/tag totals (for example a single
            // soft warning appears as several passed scenarios too).
            foreach (var category in categories
                         .Where(category => !string.IsNullOrWhiteSpace(category))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                scenario.AssignCategory(category);
            }

            return new ExtentTestScope(
                scenario,
                testClassName,
                RecordScenarioStatus,
                assertionMaxDisplayCharacters,
                redactor,
                _sync);
        }
    }

    public void RecordScenarioStatus(string testClassName, AtfReportStatus status)
    {
        lock (_sync)
        {
            if (!_classTests.TryGetValue(testClassName, out var classTest))
            {
                return;
            }

            classTest.Model.Status = ExtentStatusMapper.ToExtent(_classStatuses.Record(testClassName, status));
        }
    }
}
