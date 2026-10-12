namespace Atf.Core.Reporting;

/// <summary>
/// Resolves relative report paths against the configured project root or the
/// nearest solution. Keeping this policy outside ExtentManager makes it pure
/// enough to test and prevents path rules from leaking into report rendering.
/// </summary>
public sealed class ReportPathResolver
{
    private readonly Func<string, string?> _environment;
    private readonly string _currentDirectory;
    private readonly string _baseDirectory;

    public ReportPathResolver(
        Func<string, string?>? environment = null,
        string? currentDirectory = null,
        string? baseDirectory = null)
    {
        _environment = environment ?? Environment.GetEnvironmentVariable;
        _currentDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
    }

    public string Resolve(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new ArgumentException("A report path is required.", nameof(configuredPath));
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var configuredRoot = _environment("ATF_PROJECT_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot) && Directory.Exists(configuredRoot))
        {
            return Path.GetFullPath(Path.Combine(configuredRoot, configuredPath));
        }

        foreach (var start in new[] { _currentDirectory, _baseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (directory.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any()
                    || directory.EnumerateFiles("*.slnx", SearchOption.TopDirectoryOnly).Any())
                {
                    return Path.GetFullPath(Path.Combine(directory.FullName, configuredPath));
                }

                directory = directory.Parent;
            }
        }

        return Path.GetFullPath(configuredPath);
    }
}
