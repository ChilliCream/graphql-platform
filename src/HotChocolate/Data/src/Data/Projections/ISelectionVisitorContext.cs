using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public interface ISelectionVisitorContext
{
    Stack<ISelection> Selection { get; }

    Stack<ITypeDefinition?> ResolvedType { get; }

    IResolverContext ResolverContext { get; }
}
