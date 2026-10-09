using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public interface ISelectionVisitorContext
{
    [Obsolete("Use IncludeConditionFlags instead.")]
    ulong IncludeFlags => ResolverContext.IncludeConditionFlags.Word0;

    ConditionFlags IncludeConditionFlags => ResolverContext.IncludeConditionFlags;

    Operation Operation => ResolverContext.Operation;

    Stack<Selection> Selections { get; }

    Stack<ITypeDefinition?> ResolvedTypes { get; }

    IResolverContext ResolverContext { get; }

    SelectionEnumerator GetSelections(
        ObjectType typeContext,
        Selection? selection = null,
        bool allowInternals = false);
}
