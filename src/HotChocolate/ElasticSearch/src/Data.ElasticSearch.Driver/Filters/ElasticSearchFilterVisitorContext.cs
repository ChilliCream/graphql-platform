using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <inheritdoc />
public class ElasticSearchFilterVisitorContext
    : FilterVisitorContext<ISearchOperation>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ElasticSearchFilterVisitorContext"/>
    /// </summary>
    public ElasticSearchFilterVisitorContext(
        IFilterInputType initialType,
        IAbstractElasticClient elasticClient)
        : base(initialType)
    {
        ElasticClient = elasticClient;
        RuntimeTypes = new Stack<IExtendedType>();
        RuntimeTypes.Push(initialType.EntityType);
    }

    /// <summary>
    /// The already visited runtime types
    /// </summary>
    public Stack<IExtendedType> RuntimeTypes { get; }

    /// <inheritdoc />
    public override FilterScope<ISearchOperation> CreateScope() => new ElasticSearchFilterScope();

    /// <summary>
    /// The client that is used to execute the query
    /// </summary>
    public IAbstractElasticClient ElasticClient { get; }

    /// <summary>
    /// The path from the root to the current position in the input object
    /// </summary>
    public Stack<string> Path { get; } = new();
}
