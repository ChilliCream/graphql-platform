using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public interface ISelectionVisitorContext
{
    ulong IncludeFlags => ResolverContext.IncludeFlags;

    /// <summary>
    /// Gets the include flag overflow words (condition indexes 64 and above);
    /// empty for operations with at most 64 include conditions.
    /// </summary>
    ReadOnlySpan<ulong> WideIncludeFlags => ResolverContext.WideIncludeFlags;

    Operation Operation => ResolverContext.Operation;

    Stack<Selection> Selections { get; }

    Stack<ITypeDefinition?> ResolvedTypes { get; }

    IResolverContext ResolverContext { get; }

    SelectionEnumerator GetSelections(
        ObjectType typeContext,
        Selection? selection = null,
        bool allowInternals = false);
}
