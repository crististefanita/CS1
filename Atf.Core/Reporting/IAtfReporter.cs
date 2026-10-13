using Atf.Core.Reporting;
using Atf.Core.Reporting.Evidence;

namespace Atf.Core.Reporting.Ports;

public static class ReportingDefaults
{
    /// <summary>
    /// Maximum number of characters rendered for one assertion ACTUAL value.
    /// The module configuration can override this per run.
    /// </summary>
    public const int AssertionMaxDisplayCharacters = 240;
}

public interface IAtfLifecycleReporter
{
    /// <summary>
    /// Controls diagnostic truncation without changing the value used by the
    /// assertion itself. The default keeps lightweight test doubles compatible.
    /// </summary>
    void StartStep(string title);
    /// <summary>
    /// Closes the active technical/business step. Every reporter must
    /// implement this lifecycle boundary; silently dropping it can leave an
    /// orphaned step and produce a misleading artifact.
    /// </summary>
    void CompleteStep(AtfReportStatus status);
}

public interface IAtfEvidenceReporter
{
    /// <summary>
    /// Controls diagnostic truncation without changing the value used by the
    /// assertion itself. The default keeps lightweight test doubles compatible.
    /// </summary>
    int AssertionMaxDisplayCharacters => ReportingDefaults.AssertionMaxDisplayCharacters;

    void Detail(string key, string value);
    void Json(string label, string json);
    void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions);
    void Info(string message);
    void Fail(string message, Exception? exception = null);
    void Skip(string reason);
}

public interface IAtfStatusReporter
{
    void Pass(string message);
    void Warning(string message);
}

/// <summary>
/// The smallest port needed by validation orchestration. It intentionally
/// excludes scenario completion and keeps validation publication independent
/// from the concrete renderer.
/// </summary>
public interface IAtfValidationReporter : IAtfLifecycleReporter, IAtfEvidenceReporter, IAtfStatusReporter
{
}

/// <summary>
/// Cleanup only needs to open/close a step and publish pass/warning evidence.
/// Keeping that contract explicit prevents cleanup code from acquiring access
/// to scenario completion or secret-bearing JSON APIs.
/// </summary>
public interface IAtfCleanupReporter : IAtfLifecycleReporter, IAtfStatusReporter
{
}

public interface IAtfScenarioCompletionReporter
{
    void PassTest(string message);
    void FailTest(Exception exception);
    void SkipTest(string reason);
}

/// <summary>
/// Backwards-compatible aggregate façade. New infrastructure code should
/// depend on the narrowest port above instead of this interface.
/// </summary>
public interface IAtfReporter : IAtfValidationReporter, IAtfCleanupReporter
{
}
