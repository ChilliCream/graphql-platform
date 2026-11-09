using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Elastic.Esql.Core;
using Elastic.Esql.Extensions;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// An executable that runs an ES|QL queryable asynchronously.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class ElasticSearchExecutable<T>(IQueryable<T> source) : IQueryableExecutable<T>
{
    /// <inheritdoc />
    public IQueryable<T> Source { get; } = source;

    /// <inheritdoc />
    object IExecutable.Source => Source;

    /// <inheritdoc />
    public bool IsInMemory => false;

    /// <inheritdoc />
    public IQueryableExecutable<T> WithSource(IQueryable<T> source)
        => new ElasticSearchExecutable<T>(source);

    /// <inheritdoc />
    public IQueryableExecutable<TQuery> WithSource<TQuery>(IQueryable<TQuery> source)
        => new ElasticSearchExecutable<TQuery>(source);

    /// <inheritdoc />
    public async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => await Query.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in await ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in await ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await Query.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => await Query.CountAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public string Print() => Source.AsEsqlQueryable().ToEsqlString();

    private IEsqlQueryable<T> Query
    {
        get
        {
            if (new SkipFinder().Contains(Source.Expression))
            {
                throw ThrowHelper.Paging_SkipNotSupported();
            }

            return Source.AsEsqlQueryable();
        }
    }

    private sealed class SkipFinder : ExpressionVisitor
    {
        private bool _found;

        public bool Contains(Expression expression)
        {
            Visit(expression);
            return _found;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            _found |= node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == nameof(Queryable.Skip);
            return base.VisitMethodCall(node);
        }
    }
}
