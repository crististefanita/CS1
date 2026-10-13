namespace Atf.Reporting.Extent.Tests;

public sealed class ExtentScopeStatusPolicyTests
{
    [Fact(DisplayName = "Scenario warning preserves warning precedence without becoming a failure")]
    public void ScenarioWarningPreservesWarningPrecedence()
    {
        var policy = new ExtentScopeStatusPolicy();

        policy.RecordWarning("TEST BODY");

        Assert.True(policy.HasWarnings);
        Assert.Equal(AtfReportStatus.Warning, policy.ResolveScenarioStatus(AtfReportStatus.Pass));
        Assert.Equal(AtfReportStatus.Fail, policy.ResolveScenarioStatus(AtfReportStatus.Fail));
        Assert.Equal(AtfReportStatus.Skip, policy.ResolveScenarioStatus(AtfReportStatus.Skip));
    }

    [Fact(DisplayName = "Phase warning is scoped to the phase and is consumed on completion")]
    public void PhaseWarningIsScopedAndConsumedOnCompletion()
    {
        var policy = new ExtentScopeStatusPolicy();

        policy.RecordWarning("TEST BODY");

        Assert.Equal(AtfReportStatus.Warning, policy.ResolvePhaseStatus("TEST BODY", AtfReportStatus.Pass));
        Assert.Equal(AtfReportStatus.Pass, policy.ResolvePhaseStatus("SETUP", AtfReportStatus.Pass));

        policy.CompletePhase("TEST BODY");

        Assert.Equal(AtfReportStatus.Pass, policy.ResolvePhaseStatus("TEST BODY", AtfReportStatus.Pass));
        Assert.True(policy.HasWarnings, "Scenario warning evidence must remain visible after the phase closes.");
    }

    [Fact(DisplayName = "Warning without an active phase still affects scenario completion")]
    public void WarningWithoutPhaseStillAffectsScenarioCompletion()
    {
        var policy = new ExtentScopeStatusPolicy();

        policy.RecordWarning(null);

        Assert.Equal(AtfReportStatus.Warning, policy.ResolveScenarioStatus(AtfReportStatus.Pass));
        Assert.Equal(AtfReportStatus.Pass, policy.ResolvePhaseStatus("SETUP", AtfReportStatus.Pass));
    }

    [Fact(DisplayName = "A completed phase cannot erase the warning status of another phase")]
    public void CompletingOnePhaseDoesNotEraseAnotherPhaseWarning()
    {
        var policy = new ExtentScopeStatusPolicy();

        policy.RecordWarning("SETUP");
        policy.RecordWarning("TEST BODY");
        policy.CompletePhase("SETUP");

        Assert.Equal(AtfReportStatus.Warning, policy.ResolvePhaseStatus("TEST BODY", AtfReportStatus.Pass));
        Assert.Equal(AtfReportStatus.Warning, policy.ResolveScenarioStatus(AtfReportStatus.Pass));
    }
}
