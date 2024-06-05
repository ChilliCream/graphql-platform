using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate;

/// <summary>
/// Represents a data source query that is not yet executed.
/// </summary>
/// <typeparam name="T">
/// The type of the elements that are returned by the query.
/// </typeparam>
public abstract class Executable<T> : IExecutable<T>
{
    public abstract object Source { get; }

    public abstract ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken);


    public abstract ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);

    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken);

    public abstract ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default);

    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken);

    public virtual string Print() => Source.ToString() ?? Source.GetType().FullName ?? Source.GetType().Name;

    public sealed override string ToString() => Print();

    public abstract IAsyncEnumerable<T> ToAsyncEnumerable(CancellationToken cancellationToken = default);

    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var element in ToAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            yield return element;
        }
    }
}
