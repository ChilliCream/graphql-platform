using System.Collections;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven;

/// <summary>
/// Is the base class for a executable for the Raven.
/// </summary>
public class RavenAsyncDocumentQueryExecutable<T>
    : IExecutable<T>
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
    public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        => await Query.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public string Print() => Query.ToString() ?? "<<empty>>";
}
