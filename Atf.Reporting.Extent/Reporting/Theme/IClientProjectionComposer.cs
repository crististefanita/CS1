namespace Atf.Reporting.Extent.Theme.Client.Ports;

/// <summary>
/// Renderer-client composition seam. Navigation, disclosure, failure and tag
/// policy controllers remain independent fragments; this port only assembles
/// their self-contained script in the approved order.
/// </summary>
internal interface IClientProjectionComposer
{
    string Compose(ReportingSettings? settings = null);
}
