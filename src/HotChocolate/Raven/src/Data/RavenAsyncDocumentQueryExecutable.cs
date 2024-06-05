using System.Collections;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven;

/// <summary>
/// Is the base class for a executable for the Raven.
/// </summary>
public sealed class RavenAsyncDocumentQueryExecutable<T> : IExecutable<T>
{
    public RavenAsyncDocumentQueryExecutable(IAsyncDocumentQuery<T> query)
    {
        Query = query;
    }

    /// <summary>
    /// The underlying <see cref="IAsyncDocumentQuery{T}"/>
    /// </summary>
    public IAsyncDocumentQuery<T> Query { get; }

    /// <inheritdoc />
    public object Source => Query;

    /// <inheritdoc />
    public async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => await Query.ToListAsync(cancellationToken);

    /// <inheritdoc />
    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken);
    
    /// <inheritdoc />
    public string Print() => Query.ToString() ?? "<<empty>>";
}
