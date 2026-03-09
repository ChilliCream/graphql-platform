using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class SelectionVisitorContext(IResolverContext context) : ISelectionVisitorContext
{
    public Stack<Selection> Selections { get; } = new();

    public Stack<SelectionSetNode?> SelectionSetNodes { get; } = new();

    public Stack<ITypeDefinition?> ResolvedTypes { get; } = new();

    public IResolverContext ResolverContext { get; } = context;

    public SelectionEnumerator GetSelections(
        ObjectType typeContext,
        Selection? selection = null,
        bool allowInternals = false)
        => ResolverContext.GetSelections(typeContext, selection, allowInternals);
}
