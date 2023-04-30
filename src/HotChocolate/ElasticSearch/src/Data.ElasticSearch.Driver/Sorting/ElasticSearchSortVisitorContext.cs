using HotChocolate.Data.Sorting;
using HotChocolate.Internal;

namespace HotChocolate.Data.ElasticSearch.Sorting;

public class ElasticSearchSortVisitorContext
    : SortVisitorContext<ElasticSearchSortOperation>
{
    /// <inheritdoc />
    public ElasticSearchSortVisitorContext(ISortInputType initialType) : base(initialType)
    {
        RuntimeTypes = new Stack<IExtendedType>();
        RuntimeTypes.Push(initialType.EntityType);
    }

    /// <summary>
    /// The already visited runtime types
    /// </summary>
    public Stack<IExtendedType> RuntimeTypes { get; }

    /// <summary>
    /// The path from the root to the current position in the input object
    /// </summary>
    public Stack<string> Path { get; } = new();
}
