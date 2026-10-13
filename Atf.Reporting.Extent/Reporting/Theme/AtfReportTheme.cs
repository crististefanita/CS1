namespace Atf.Reporting.Extent.Theme;

public static class AtfReportTheme
{
    /// <summary>
    /// Composes semantic theme fragments into one self-contained stylesheet.
    /// The facade owns ordering and token expansion; fragments own one visual
    /// concern each and remain independently reviewable.
    /// </summary>
    public static string BuildCss()
        => BuildCss(new AtfReportThemeTokenProvider());

    internal static string BuildCss(IThemeTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);

        var css =
            "<style id=\"atf-custom-theme\">\n"
            + AtfReportThemeLayoutFragments.Build()
            + AtfReportThemeOverviewFragments.Build()
            + AtfReportThemeExecutionFragments.Build()
            + AtfReportThemeEvidenceFragments.Build()
            + AtfReportThemeTypographyFragments.Build()
            + "\n    </style>";

        css = css.Replace(
            "__ATF_EXECUTION_HIERARCHY_CSS__",
            AtfReportThemeFragments.BuildExecutionHierarchyCss(),
            StringComparison.Ordinal);

        foreach (var token in tokenProvider.All)
        {
            css = css.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return css;
    }
}

