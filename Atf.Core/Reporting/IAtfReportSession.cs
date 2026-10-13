using Atf.Core.Configuration;
using System.Reflection;

namespace Atf.Core.Reporting;

/// <summary>
/// Renderer-neutral lifetime for one isolated ATF report session.
/// Implementations own their document, hierarchy and output path; callers
/// never need to reference a concrete reporting engine.
/// </summary>
public interface IAtfReportSession
{
    string ReportPath { get; }

    void RegisterTestAssembly(Assembly testAssembly);

    void Initialize(ReportingSettings settings, ApiSettings? api = null);

    IAtfScenarioScope StartTest(
        string testClassName,
        string scenarioName,
        string description,
        IEnumerable<string> categories);

    void Flush();
    void FinalizeReport();

    /// <summary>
    /// Finalizes the session without blocking the caller while renderer-owned
    /// artifact cleanup is waiting for a transient filesystem lock.
    /// Legacy implementations remain source-compatible through the default
    /// synchronous bridge.
    /// </summary>
    ValueTask FinalizeReportAsync(CancellationToken cancellationToken = default)
    {
        FinalizeReport();
        return ValueTask.CompletedTask;
    }
}
