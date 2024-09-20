using System.Collections;
using System.Runtime.CompilerServices;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven;

/// <summary>
/// Is the base class for an executable for the Raven.
/// </summary>
public sealed class RavenAsyncDocumentQueryExecutable<T>(IAsyncDocumentQuery<T> query) : IExecutable<T>
{
    /// <summary>
    /// The underlying <see cref="IAsyncDocumentQuery{T}"/>
    /// </summary>
    public IAsyncDocumentQuery<T> Query { get; } = query;

    /// <inheritdoc />
    public object Source => Query;

    /// <inheritdoc />
    public async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => await Query.ToListAsync(cancellationToken);

    /// <inheritdoc />
    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var list = await Query.ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var list = await Query.ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.SingleOrDefaultAsync(cancellationToken);

    public async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => await Query.CountAsync(cancellationToken);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public string Print() => Query.ToString() ?? "<<empty>>";
}
