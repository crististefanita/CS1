namespace Atf.Core.Configuration.Models;

public sealed class AtfModuleSettings
{
    public AtfModuleConfig Config { get; init; } = new();
    public ApiSettings Api => Config.Api;
    public ReportingSettings Reporting => Config.Reporting;
}

public sealed class AtfModuleConfig
{
    public ApiSettings Api { get; init; } = new();
    public ReportingSettings Reporting { get; init; } = new();
}
