namespace Atf.Reporting.Extent.Theme.Tokens;

/// <summary>
/// Resolves semantic theme placeholders to the single ATF palette and
/// typography sources. Keeping token ownership outside the CSS facade makes
/// palette changes independent from layout fragment changes.
/// </summary>
internal interface IThemeTokenProvider
{
    IReadOnlyDictionary<string, string> All { get; }
}

/// <summary>
/// Supplies the immutable ATF palette and typography substitutions consumed by
/// the CSS composer. Theme orchestration depends on this port rather than on a
/// static token registry, so token sources can be tested and replaced without
/// changing layout fragments.
/// </summary>
internal sealed class AtfReportThemeTokenProvider : IThemeTokenProvider
{
    public IReadOnlyDictionary<string, string> All { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["__ATF_JSONSURFACE__"] = AtfReportColors.JsonSurface,
            ["__ATF_ACCENTCONTRAST__"] = AtfReportColors.AccentContrast,
            ["__ATF_JSONCONTAINER__"] = AtfReportColors.JsonContainer,
            ["__ATF_DARK__"] = AtfReportColors.Dark,
            ["__ATF_BORDER__"] = AtfReportColors.Border,
            ["__ATF_REFERENCEBORDER__"] = AtfReportColors.ReferenceBorder,
            ["__ATF_GOOD__"] = AtfReportColors.Good,
            ["__ATF_TABLEBORDER__"] = AtfReportColors.TableBorder,
            ["__ATF_SETUP__"] = AtfReportColors.Setup,
            ["__ATF_RESPONSEGOOD__"] = AtfReportColors.ResponseGood,
            ["__ATF_METADATA__"] = AtfReportColors.Metadata,
            ["__ATF_ACCENT__"] = AtfReportColors.Accent,
            ["__ATF_OVERVIEWMUTED__"] = AtfReportColors.OverviewMuted,
            ["__ATF_MUTED__"] = AtfReportColors.Muted,
            ["__ATF_EXTENTPASS__"] = AtfReportColors.ExtentPass,
            ["__ATF_TABLELABEL__"] = AtfReportColors.TableLabel,
            ["__ATF_TABLETEXT__"] = AtfReportColors.TableText,
            ["__ATF_CODETEXT__"] = AtfReportColors.CodeText,
            ["__ATF_EXTENTFAIL__"] = AtfReportColors.ExtentFail,
            ["__ATF_TEXT__"] = AtfReportColors.Text,
            ["__ATF_JSONTEXT__"] = AtfReportColors.JsonText,
            ["__ATF_WARNING__"] = AtfReportColors.Warning,
            ["__ATF_DANGER__"] = AtfReportColors.Danger,
            ["__ATF_EXTENTSKIP__"] = AtfReportColors.ExtentSkip,
            ["__ATF_WHITE__"] = AtfReportColors.White,
            ["__ATF_SHADOWSOFT__"] = AtfReportColors.ShadowSoft,
            ["__ATF_SHADOWSTRONG__"] = AtfReportColors.ShadowStrong,
            ["__ATF_ACCENT045__"] = AtfReportColors.Accent045,
            ["__ATF_ACCENT055__"] = AtfReportColors.Accent055,
            ["__ATF_ACCENT07__"] = AtfReportColors.SecondarySectionHeadingBackground,
            ["__ATF_ACCENT08__"] = AtfReportColors.Accent08,
            ["__ATF_ACCENT12__"] = AtfReportColors.SecondarySectionHeadingHoverBackground,
            ["__ATF_ACCENT14__"] = AtfReportColors.Accent14,
            ["__ATF_ACCENT42__"] = AtfReportColors.SecondarySectionHeadingBorder,
            ["__ATF_ACCENT45__"] = AtfReportColors.Accent45,
            ["__ATF_GRAYBADGE__"] = AtfReportColors.GrayBadge,
            ["__ATF_WARNING10__"] = AtfReportColors.Warning10,
            ["__ATF_DANGER08__"] = AtfReportColors.Danger08,
            ["__ATF_DANGER10__"] = AtfReportColors.Danger10,
            ["__ATF_DANGER55__"] = AtfReportColors.Danger55,
            ["__ATF_WHITE018__"] = AtfReportColors.White018,
            ["__ATF_WHITE02__"] = AtfReportColors.White02,
            ["__ATF_WHITE025__"] = AtfReportColors.White025,
            ["__ATF_WHITE03__"] = AtfReportColors.White03,
            ["__ATF_WHITE055__"] = AtfReportColors.White055,
            ["__ATF_GOOD10__"] = AtfReportColors.Good10,
            ["__ATF_FONT_SANS__"] = AtfReportTypography.SansFontFamily,
            ["__ATF_FONT_CODE__"] = AtfReportTypography.CodeFontFamily,
            ["__ATF_FONT_BASE_SIZE__"] = AtfReportTypography.BaseFontSize,
            ["__ATF_FONT_OVERVIEW_SIZE__"] = AtfReportTypography.OverviewFontSize,
            ["__ATF_FONT_JSON_SIZE__"] = AtfReportTypography.JsonFontSize,
            ["__ATF_FONT_TABLE_HEADER_SIZE__"] = AtfReportTypography.TableHeaderFontSize,
            ["__ATF_FONT_TABLE_CELL_SIZE__"] = AtfReportTypography.TableCellFontSize,
            ["__ATF_FONT_BADGE_SIZE__"] = AtfReportTypography.BadgeFontSize,
        };
}
