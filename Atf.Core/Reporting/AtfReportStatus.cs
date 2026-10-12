namespace Atf.Core.Reporting;

/// <summary>
/// Runner-neutral status used by the ATF reporting contract. Concrete adapters
/// map it to their own status model at the infrastructure boundary.
/// </summary>
public enum AtfReportStatus
{
    Info,
    Pass,
    Fail,
    Skip,
    Warning
}
