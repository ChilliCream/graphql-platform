using System.Text.Json;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaResult : IDisposable
{
    private readonly IDisposable? _resource;

    public SourceSchemaResult(
        Path path,
        IDisposable resource,
        SourceResultElement data,
        SourceResultElement errors,
        SourceResultElement extensions,
        FinalMessage final = FinalMessage.Undefined)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(resource);

        _resource = resource;
        Path = path;
        Data = data;
        RawErrors = errors;
        Errors = SourceSchemaErrors.From(errors);
        Extensions = extensions;
        Final = final;
    }

    public Path Path { get; }

    public SourceResultElement Data { get; }

    public SourceSchemaErrors? Errors { get; }

    public SourceResultElement RawErrors { get; }

    public SourceResultElement Extensions { get; }

    public FinalMessage Final { get; }

    public void Dispose() => _resource?.Dispose();
}
