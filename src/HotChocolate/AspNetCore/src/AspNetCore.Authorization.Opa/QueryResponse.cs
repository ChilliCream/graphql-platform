using System.Text.Json;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The class representing OPA query response.
/// </summary>
public sealed class OpaQueryResponse(JsonDocument document) : IDisposable
{
    private readonly JsonElement _root = document.RootElement;

    public Guid? DecisionId
        => _root.TryGetProperty("decision_id", out var value)
            ? value.GetGuid()
            : null;

    public T? GetResult<T>()
        => _root.TryGetProperty("result", out var value)
            ? value.Deserialize<T>()
            : default;

    public bool IsEmpty
        => _root.ValueKind is JsonValueKind.Object
            && _root.EnumerateObject().Any();

    public void Dispose()
        => document.Dispose();
}
