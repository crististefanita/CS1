namespace Atf.Core.Configuration;

/// <summary>
/// Stable diagnostic emitted when the configuration boundary rejects a value.
/// The inherited exception message remains human-readable for existing hosts,
/// while code and path give tooling a deterministic contract.
/// </summary>
public sealed class AtfConfigurationException : InvalidOperationException
{
    public AtfConfigurationException(string code, string path, string message, Exception? innerException = null)
        : base($"{code} [{path}] {message}", innerException)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A diagnostic code is required.", nameof(code))
            : code;
        Path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("A configuration path is required.", nameof(path))
            : path;
        Detail = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException("A diagnostic message is required.", nameof(message))
            : message;
    }

    public string Code { get; }

    public string Path { get; }

    public string Detail { get; }
}
