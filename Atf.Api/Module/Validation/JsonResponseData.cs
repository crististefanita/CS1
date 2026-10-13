using Atf.Common.Errors;
using Atf.Core.Http;
using System.Text.Json;

namespace Atf.Common.Validation;

public static class JsonResponseData
{
    public static int FirstArrayItemInt(ApiResponse response, string property)
    {
        using var document = JsonDocument.Parse(response.Body);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            throw new ApiValidationException("JSON collection", "at least one item", response.Body);
        }

        var first = document.RootElement[0];
        if (!first.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var result))
        {
            throw new ApiValidationException($"JSON collection property {property}", "integer", first.ToString());
        }

        return result;
    }
}
