using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class SelectionVisitorContext(IResolverContext context) : ISelectionVisitorContext
{
    public Stack<ISelection> Selection { get; } = new();

    public Stack<SelectionSetNode?> SelectionSetNodes { get; } = new();

    public Stack<INamedType?> ResolvedType { get; } = new();

    public IResolverContext ResolverContext { get; } = context;
}
