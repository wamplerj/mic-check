using System.Text.Json.Serialization;

namespace MicCheck.Api.Webhooks;

public record WebhookPayload(
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("data")] object Data
);

public record FlagUpdatedData(
    [property: JsonPropertyName("changed_by")] string? ChangedBy,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("new_state")] FlagStateSnapshot? NewState,
    [property: JsonPropertyName("previous_state")] FlagStateSnapshot? PreviousState
);

public record FlagDeletedData(
    [property: JsonPropertyName("changed_by")] string? ChangedBy,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("feature")] FeatureSummary Feature
);

public record FlagStateSnapshot(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("feature_state_value")] string? FeatureStateValue,
    [property: JsonPropertyName("feature")] FeatureSummary Feature,
    [property: JsonPropertyName("environment")] EnvironmentSummary Environment
);

public record FeatureSummary(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);

public record EnvironmentSummary(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);
