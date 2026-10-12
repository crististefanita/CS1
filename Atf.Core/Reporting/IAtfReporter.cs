namespace Atf.Core.Reporting;

public static class ReportingDefaults
{
    /// <summary>
    /// Maximum number of characters rendered for one assertion ACTUAL value.
    /// The module configuration can override this per run.
    /// </summary>
    public const int AssertionMaxDisplayCharacters = 240;
}

public interface IAtfReporter
{
    /// <summary>
    /// Controls diagnostic truncation without changing the value used by the
    /// assertion itself. The default keeps lightweight test doubles compatible.
    /// </summary>
    int AssertionMaxDisplayCharacters => ReportingDefaults.AssertionMaxDisplayCharacters;

    void StartStep(string title);
    void Detail(string key, string value);
    void Json(string label, string json);
    void AssertionTable(IReadOnlyCollection<AtfAssertion> assertions);
    void Info(string message);
    void Pass(string message);
    void Fail(string message, Exception? exception = null);
    void Warning(string message);
    void Skip(string reason);
}
