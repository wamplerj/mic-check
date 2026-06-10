using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicCheck.Api.Identities;

public record TraitInput(
    [property: JsonPropertyName("trait_key")] string TraitKey,
    [property: JsonPropertyName("trait_value")] object? TraitValue)
{
    public string GetStringValue() => TraitValue switch
    {
        JsonElement el when el.ValueKind == JsonValueKind.String => el.GetString() ?? string.Empty,
        JsonElement el when el.ValueKind == JsonValueKind.True => "true",
        JsonElement el when el.ValueKind == JsonValueKind.False => "false",
        JsonElement el when el.ValueKind == JsonValueKind.Null => string.Empty,
        JsonElement el => el.ToString(),
        null => string.Empty,
        _ => TraitValue.ToString() ?? string.Empty
    };

    public TraitValueType GetValueType() => TraitValue switch
    {
        JsonElement el when el.ValueKind is JsonValueKind.True or JsonValueKind.False => TraitValueType.Boolean,
        JsonElement el when el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out _) => TraitValueType.Integer,
        JsonElement el when el.ValueKind == JsonValueKind.Number => TraitValueType.Float,
        bool => TraitValueType.Boolean,
        int or long => TraitValueType.Integer,
        float or double or decimal => TraitValueType.Float,
        _ => TraitValueType.String
    };
}
