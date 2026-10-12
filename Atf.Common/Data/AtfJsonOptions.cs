using System.Text.Json;

namespace Atf.Common.Data;

/// <summary>
/// One JSON policy for product-facing fixtures and request DTOs.
/// Keeping the options immutable prevents every service from inventing a
/// slightly different payload contract.
/// </summary>
public static class AtfJsonOptions
{
    public static JsonSerializerOptions Web { get; } = new(JsonSerializerDefaults.Web);

    public static JsonSerializerOptions CaseInsensitive { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
