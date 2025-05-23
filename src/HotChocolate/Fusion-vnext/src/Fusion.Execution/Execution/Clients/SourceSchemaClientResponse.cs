namespace HotChocolate.Fusion.Execution.Clients;

public abstract class SourceSchemaClientResponse : IDisposable
{
    public abstract IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
        CancellationToken cancellationToken = default);

    public abstract bool IsSuccessful { get; }

    public abstract void Dispose();
}
