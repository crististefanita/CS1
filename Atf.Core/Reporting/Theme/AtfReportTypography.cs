namespace Atf.Core.Reporting.Theme;

/// <summary>
/// Single source of truth for report typography. Values are CSS fragments
/// because the Extent artifact is delivered as one self-contained HTML file.
/// </summary>
public static class AtfReportTypography
{
    public const string SansFontFamily = "'Segoe UI', 'Inter', 'Helvetica Neue', Arial, sans-serif";
    public const string CodeFontFamily = "ui-monospace, Consolas, monospace";
    public const string BaseFontSize = "14px";
    public const string OverviewFontSize = "12.6px";
    public const string JsonFontSize = "12.5px";
    public const string TableHeaderFontSize = ".8rem";
    public const string TableCellFontSize = ".9rem";
    public const string BadgeFontSize = ".68rem";
}
