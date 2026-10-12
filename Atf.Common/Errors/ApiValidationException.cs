namespace Atf.Common.Errors;

/// <summary>
/// A structured validation diagnostic used when response data cannot satisfy
/// a reusable API assertion or data extraction contract.
/// </summary>
public sealed class ApiValidationException(string rule, string expected, string actual)
    : Exception($"{rule}: expected '{expected}', actual '{actual}'.")
{
    public string Rule { get; } = rule;
    public string Expected { get; } = expected;
    public string Actual { get; } = actual;
}
