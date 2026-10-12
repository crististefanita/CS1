namespace Atf.Common.Validation;

public sealed class ApiValidationException(string rule, string expected, string actual)
    : Exception($"{rule}: expected '{expected}', actual '{actual}'.")
{
    public string Rule { get; } = rule;
    public string Expected { get; } = expected;
    public string Actual { get; } = actual;
}
