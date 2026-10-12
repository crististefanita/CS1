using Microsoft.Extensions.Configuration;

namespace Atf.Core.Configuration;

public interface IAtfConfigurationLoader
{
    AtfConfiguration Load(string? basePath = null);
}

/// <summary>
/// Loads the ATF configuration from the module-owned configuration directory.
/// Production uses ATF_ environment variables; tests or alternative hosts can
/// provide an explicit override dictionary without mutating process state.
/// </summary>
public sealed class AtfConfigurationLoader : IAtfConfigurationLoader
{
    private readonly Func<string, string?> _environment;
    private readonly IReadOnlyDictionary<string, string?>? _environmentOverrides;

    public AtfConfigurationLoader(
        Func<string, string?>? environment = null,
        IReadOnlyDictionary<string, string?>? environmentOverrides = null)
    {
        _environment = environment ?? Environment.GetEnvironmentVariable;
        _environmentOverrides = environmentOverrides;
    }

    public AtfConfiguration Load(string? basePath = null)
    {
        var environment = _environment("ATF_ENV")?.Trim();
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath ?? AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        if (!string.IsNullOrWhiteSpace(environment))
        {
            builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
        }

        if (_environmentOverrides is null)
        {
            builder.AddEnvironmentVariables("ATF_");
        }
        else
        {
            builder.AddInMemoryCollection(_environmentOverrides);
        }

        var configuration = builder.Build();
        var result = configuration.Get<AtfConfiguration>() ?? new AtfConfiguration();
        result.Validate();
        return result;
    }
}
