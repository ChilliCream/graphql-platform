using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;

namespace HotChocolate.Data.ElasticSearch.Sorting;

public class ElasticSearchSortVisitorContext
    : SortVisitorContext<ISearchOperation>
{
    /// <inheritdoc />
    public ElasticSearchSortVisitorContext(
        ISortInputType initialType,
        IAbstractElasticClient elasticClient) : base(initialType)
    {
        ElasticClient = elasticClient;
        RuntimeTypes = new Stack<IExtendedType>();
        RuntimeTypes.Push(initialType.EntityType);
    }

    /// <summary>
    /// The already visited runtime types
    /// </summary>
    public Stack<IExtendedType> RuntimeTypes { get; }

    /// <summary>
    /// The client that is used to execute the query
    /// </summary>
    public IAbstractElasticClient ElasticClient { get; }

    /// <summary>
    /// The path from the root to the current position in the input object
    /// </summary>
    public Stack<string> Path { get; } = new();
}
