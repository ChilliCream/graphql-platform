using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Abstractions;

namespace HotChocolate.Data.Projections;

public interface ISelectionVisitorContext
{
    Stack<ISelection> Selection { get; }

    Stack<ITypeDefinition?> ResolvedType { get; }

    [Obsolete("Use ResolverContext")]
    IResolverContext Context => ResolverContext;

    IResolverContext ResolverContext { get; }
}
