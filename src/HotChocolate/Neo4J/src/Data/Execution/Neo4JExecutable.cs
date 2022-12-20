using System.Collections;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Neo4J.Sorting;
using HotChocolate.Language;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Execution;

/// <summary>
/// Represents a executable for Neo4j database.
/// </summary>
public class Neo4JExecutable<T>
    : INeo4JExecutable
    , IExecutable<T>
{
    private static Node Node => Cypher.NamedNode(typeof(T).Name);

    private readonly IAsyncSession _session;

    /// <summary>
    /// The filter definition that was set by <see cref="WithFiltering"/>
    /// </summary>
    private CompoundCondition? _filters;

    /// <summary>
    /// The sort definition that was set by <see cref="WithSorting"/>
    /// </summary>
    private Neo4JSortDefinition[]? _sorting;

    /// <summary>
    /// The projection definition that was set by <see cref="WithProjection"/>
    /// </summary>
    private object[]? _projection;

    /// <summary>
    /// The skip paging definition
    /// </summary>
    private int? _skip;

    /// <summary>
    /// The limit paging definition
    /// </summary>
    private int? _limit;

    public Neo4JExecutable(IAsyncSession session)
    {
        _session = session;
    }

    /// <inheritdoc />
    public object Source => _session;

    /// <inheritdoc />
    public async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
    {
        var cursor = await _session.RunAsync(Pipeline().Build());
        return await cursor.MapAsync<T>().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public string Print() => Pipeline().Build();

    public Neo4JExecutable<T> WithSkip(int skip)
    {
        _skip = skip;
        return this;
    }

    public Neo4JExecutable<T> WithLimit(int limit)
    {
        _limit = limit;
        return this;
    }

    /// <inheritdoc />
    public INeo4JExecutable WithFiltering(CompoundCondition filters)
    {
        if (!filters.IsEmpty)
        {
            _filters = filters;
        }

        return this;
    }

    /// <inheritdoc />
    public INeo4JExecutable WithSorting(Neo4JSortDefinition[] sorting)
    {
        _sorting = sorting;
        return this;
    }

    /// <inheritdoc />
    public INeo4JExecutable WithProjection(object[] projection)
    {
        _projection = projection;
        return this;
    }

    public StatementBuilder Pipeline()
    {
        var statement = Cypher.Match(Node).Return(Node);

        ApplyFilters(statement);
        ApplyProjections(statement);
        ApplyPaging(statement);
        ApplySorting(statement);

        return statement;
    }

    private void ApplyFilters(StatementBuilder statement)
    {
        if (_filters is null)
        {
            return;
        }

        statement.Match(new Where(_filters), Node);
    }

    private void ApplyProjections(StatementBuilder statement)
    {
        if (_projection is null)
        {
            return;
        }

        statement.Return(Node.Project(_projection));
    }

    private void ApplyPaging(StatementBuilder statement)
    {
        if (_limit is not null)
        {
            statement.Limit((int)_limit);
        }

        if (_skip is not null)
        {
            statement.Skip((int)_skip);
        }
    }

    private void ApplySorting(StatementBuilder statement)
    {
        if (_sorting is null)
        {
            return;
        }

        var sorts = new List<SortItem>(_sorting.Length);

        foreach (var sort in _sorting)
        {
            var sortItem = Cypher.Sort(Node.Property(sort.Field));
            if (sort.Direction == SortDirection.Ascending)
            {
                sorts.Push(sortItem.Ascending());
            }
            else if (sort.Direction == SortDirection.Descending)
            {
                sorts.Push(sortItem.Descending());
            }
        }

        statement.OrderBy(sorts);
    }
}
