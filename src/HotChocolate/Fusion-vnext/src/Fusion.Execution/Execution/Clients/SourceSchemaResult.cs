using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaResult : IDisposable
{
    private readonly IDisposable? _resource;

    public SourceSchemaResult(Path path, JsonDocument document)
    {
        _resource = document;
        Path = path;

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (root.TryGetProperty("data", out var data))
        {
            Data = data;
        }

        if (root.TryGetProperty("errors", out var errors))
        {
            Errors = errors;
        }

        if (root.TryGetProperty("extensions", out var extensions))
        {
            Extensions = extensions;
        }
    }

    public SourceSchemaResult(Path path, IDisposable resource, JsonElement data, JsonElement errors, JsonElement extensions)
    {
        _resource = resource;
        Path = path;
        Data = data;
        Errors = errors;
        Extensions = extensions;
    }

    public Path Path { get; }

    public JsonElement Data { get; }

    public JsonElement Errors { get; }

    public JsonElement Extensions { get; }

    public void Dispose() => _resource?.Dispose();
}
