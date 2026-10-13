using System.Text.Json;

namespace Atf.Common.Data;

/// <summary>
/// One JSON policy for product-facing fixtures and request DTOs.
/// Keeping the options immutable prevents every service from inventing a
/// slightly different payload contract.
/// </summary>
public static class AtfJsonOptions
{
    private static readonly object SerializationGate = new();

    public static JsonSerializerOptions Web { get; } = new(JsonSerializerDefaults.Web);

    public static JsonSerializerOptions CaseInsensitive { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static string SerializeWeb<T>(T value)
    {
        lock (SerializationGate)
        {
            return JsonSerializer.Serialize(value, Web);
        }
    }

    public static T DeserializeCaseInsensitive<T>(string json)
    {
        lock (SerializationGate)
        {
            return JsonSerializer.Deserialize<T>(json, CaseInsensitive)
                ?? throw new InvalidOperationException("JSON payload deserialized to null.");
        }
    }
}
