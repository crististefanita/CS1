using AventStack.ExtentReports;

namespace Atf.Reporting.Extent;

/// <summary>
/// Enforces the functional reporting contract independently from step-stack
/// mechanics: every HTTP request is followed by one aggregate validation
/// sibling containing one assertion table in the same lifecycle phase.
/// </summary>
internal static class ExtentPhaseContractValidator
{
    public static void EnsureSuccessfulPhaseHasContractValidation(ExtentTestScope.StepFrame phase)
    {
        ArgumentNullException.ThrowIfNull(phase);

        var children = phase.Node.Model.Children.ToArray();
        for (var index = 0; index < children.Length; index++)
        {
            var request = children[index];
            if (!ExtentStepClassifier.IsHttp(request.Name))
            {
                continue;
            }

            if (request.Children.Any(child => ExtentStepClassifier.IsValidation(child.Name)))
            {
                throw new InvalidOperationException(
                    $"API request '{request.Name}' contains a nested contract validation. " +
                    "Validation must be its direct sibling under the lifecycle phase.");
            }

            if (index + 1 >= children.Length || !ExtentStepClassifier.IsValidation(children[index + 1].Name))
            {
                throw new InvalidOperationException(
                    $"API request '{request.Name}' completed without contract validation. " +
                    "Every successful response must be followed by exactly one aggregate validation sibling.");
            }

            var validation = children[index + 1];
            var assertionEvidenceCount = validation.Logs.Count(log =>
                log.Details.Contains("assertion-evidence", StringComparison.Ordinal));
            if (assertionEvidenceCount != 1)
            {
                throw new InvalidOperationException(
                    $"Validation '{validation.Name}' must publish exactly one aggregate AssertionTable.");
            }

            index++;
        }
    }
}
