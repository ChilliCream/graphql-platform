namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaError(IError error, Path path)
{
    public IError Error { get; } = error;

    public Path Path { get; } = path;
}
