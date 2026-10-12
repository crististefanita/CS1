using AventStack.ExtentReports;
using System.Net;

namespace Atf.Core.Reporting;

/// <summary>
/// Encapsulates the Extent test tree: class roots contain scenario nodes and
/// class status is aggregated from those scenario results. The manager only
/// coordinates lifecycle and locking; it does not own tree semantics.
/// </summary>
internal sealed class ExtentTestHierarchy
{
    private readonly Dictionary<string, ExtentTest> _classTests = new(StringComparer.Ordinal);
    private readonly ReportStatusAggregator _classStatuses = new();

    public ExtentTestScope Start(
        ExtentReports reports,
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories,
        int assertionMaxDisplayCharacters = ReportingDefaults.AssertionMaxDisplayCharacters)
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

        foreach (var category in categories
                     .Where(category => !string.IsNullOrWhiteSpace(category))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            classTest.AssignCategory(category);
        }

        var scenario = classTest.CreateNode(WebUtility.HtmlEncode(scenarioName), description);
        return new ExtentTestScope(scenario, testClassName, assertionMaxDisplayCharacters);
    }

    public void RecordScenarioStatus(string testClassName, Status status)
    {
        if (!_classTests.TryGetValue(testClassName, out var classTest))
        {
            return;
        }

        classTest.Model.Status = _classStatuses.Record(testClassName, status);
    }
}
