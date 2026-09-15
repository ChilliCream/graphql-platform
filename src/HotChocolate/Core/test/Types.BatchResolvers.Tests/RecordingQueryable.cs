using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// A minimal <see cref="IQueryable{T}"/> wrapping <see cref="EnumerableQuery{T}"/> that records the
/// final LINQ expression tree (via <see cref="Expression.ToString"/>) onto a <see cref="BatchProbe"/>
/// under the member name "Expression" the moment the query is actually enumerated. Lets a test
/// observe the exact shape (for example a projection's <c>Select</c>) a batch middleware built
/// against an in-memory queryable, without a real database provider.
/// </summary>
public sealed class RecordingQueryable<T> : IOrderedQueryable<T>
{
    private readonly BatchProbe _probe;

    public RecordingQueryable(IEnumerable<T> source, BatchProbe probe)
        : this(((IQueryable<T>)new EnumerableQuery<T>(source)).Expression, probe)
    {
    }

    private RecordingQueryable(Expression expression, BatchProbe probe)
    {
        Expression = expression;
        _probe = probe;
        Provider = new RecordingQueryProvider(probe);
    }

    public Type ElementType => typeof(T);

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator()
    {
        _probe.Record("Expression", [Expression.ToString()]);
        return ((IEnumerable<T>)new EnumerableQuery<T>(Expression)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class RecordingQueryProvider(BatchProbe probe) : IQueryProvider
    {
        private static readonly MethodInfo CreateQueryMethod = typeof(RecordingQueryProvider)
            .GetMethods()
            .Single(m => m.Name == nameof(CreateQuery) && m.IsGenericMethodDefinition);

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments()[0];
            return (IQueryable)CreateQueryMethod.MakeGenericMethod(elementType).Invoke(this, [expression])!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new RecordingQueryable<TElement>(expression, probe);

        public object? Execute(Expression expression)
            => ((IQueryProvider)new EnumerableQuery<T>(expression)).Execute(expression);

        public TResult Execute<TResult>(Expression expression)
            => ((IQueryProvider)new EnumerableQuery<T>(expression)).Execute<TResult>(expression);
    }
}
