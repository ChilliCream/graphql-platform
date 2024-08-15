using System.Text.Json;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaQueryResponse : IDisposable
{
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public OpaQueryResponse(JsonDocument document)
    {
        _document = document;
        _root = document.RootElement;
    }

    public Guid? DecisionId
        => _root.TryGetProperty("decisionId", out var value)
            ? value.GetGuid()
            : null;

    public T? GetResult<T>()
        => _root.TryGetProperty("decisionId", out var value)
            ? value.Deserialize<T>()
            : default;

    public bool IsEmpty
        => _root.ValueKind is JsonValueKind.Object &&
            _root.EnumerateObject().Any();

    public void Dispose()
        => _document.Dispose();
}
