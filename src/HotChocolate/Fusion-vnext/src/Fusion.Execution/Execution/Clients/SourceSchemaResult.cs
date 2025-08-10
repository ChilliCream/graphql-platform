using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaResult : IDisposable
{
    private readonly IDisposable? _resource;

    public SourceSchemaResult(
        Path path,
        IDisposable resource,
        JsonElement data,
        JsonElement errors,
        JsonElement extensions,
        FinalMessage final = FinalMessage.Undefined)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(resource);

        _resource = resource;
        Path = path;
        Data = data;
        Errors = ErrorTrie.From(errors);
        Extensions = extensions;
        Final = final;
    }

    public Path Path { get; }

    public JsonElement Data { get; }

    public ErrorTrie? Errors { get; }

    public JsonElement Extensions { get; }

    public FinalMessage Final { get; }

    public void Dispose() => _resource?.Dispose();
}
