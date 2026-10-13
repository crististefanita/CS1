using Atf.Core.Configuration;

namespace Atf.Core.Reporting;

/// <summary>
/// API-host reporting port. It adds the immutable runtime configuration needed
/// by API composition without exposing any concrete renderer or test fixture.
/// </summary>
public interface IAtfApiReportSession : IAtfReportSession
{
    AtfConfiguration Configuration { get; }
}
