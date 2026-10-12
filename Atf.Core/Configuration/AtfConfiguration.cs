using Atf.Core.Configuration.Models;
using Atf.Core.Reporting;

namespace Atf.Core.Configuration;

public sealed class AtfConfiguration
{
    public Dictionary<string, AtfModuleSettings> Modules { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Compatibility facades keep the API and reporting layers independent from the
    // physical configuration shape while making Modules:Api the single ownership boundary.
    public ApiSettings Api => GetModule("api").Api;
    public ReportingSettings Reporting => GetModule("api").Reporting;

    public static AtfConfiguration Load(string? basePath = null)
        => new AtfConfigurationLoader().Load(basePath);

    public void Validate() => AtfConfigurationValidator.Validate(this);

    private AtfModuleSettings GetModule(string moduleId) =>
        Modules.TryGetValue(moduleId, out var module)
            ? module
            : throw new InvalidOperationException($"Modules:{moduleId} configuration is required.");
}
