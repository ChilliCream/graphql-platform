using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class Fragment : IFragment
{
    private readonly long _includeCondition;

    public Fragment(
        int id,
        IObjectType typeCondition,
        ISyntaxNode syntaxNode,
        IReadOnlyList<DirectiveNode> directives,
        int selectionSetId,
        ISelectionSet selectionSet,
        long includeCondition,
        bool isInternal = false)
    {
        Id = id;
        TypeCondition = typeCondition;
        SyntaxNode = syntaxNode;
        Directives = directives;
        SelectionSetId = selectionSetId;
        SelectionSet = selectionSet;
        _includeCondition = includeCondition;
        IsInternal = isInternal;
    }

    public int Id { get; }

    public IObjectType TypeCondition { get; }

    public ISyntaxNode SyntaxNode { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public int SelectionSetId { get; }

    public ISelectionSet SelectionSet { get; }

    public bool IsInternal { get; }

    public bool IsConditional => _includeCondition is not 0;

    public string? GetLabel(IVariableValueCollection variables)
        => Directives.GetDeferDirective(variables)?.Label;

    public bool IsIncluded(long includeFlags, bool allowInternals = false)
        => (includeFlags & _includeCondition) == _includeCondition &&
            (!IsInternal || allowInternals);
}
