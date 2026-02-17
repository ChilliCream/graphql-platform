namespace HotChocolate.Fusion.Execution.Clients;

public abstract class SourceSchemaClientResponse : IDisposable
{
    public abstract Uri Uri { get; }

    public abstract string ContentType { get; }

    public abstract bool IsSuccessful { get; }

    public abstract IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
        CancellationToken cancellationToken = default);

    public abstract void Dispose();
}
