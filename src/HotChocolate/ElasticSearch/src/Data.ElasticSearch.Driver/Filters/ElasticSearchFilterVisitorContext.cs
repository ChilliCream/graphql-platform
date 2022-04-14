using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <inheritdoc />
public class ElasticSearchFilterVisitorContext
    : FilterVisitorContext<ISearchOperation >
{
    public ElasticSearchFilterVisitorContext(IFilterInputType initialType)
        : base(initialType)
    {
        RuntimeTypes = new Stack<IExtendedType>();
        RuntimeTypes.Push(initialType.EntityType);
    }

    /// <summary>
    /// The already visited runtime types
    /// </summary>
    public Stack<IExtendedType> RuntimeTypes { get; }

    /// <inheritdoc />
    public override FilterScope<ISearchOperation> CreateScope() => new ElasticSearchFilterScope();
}
