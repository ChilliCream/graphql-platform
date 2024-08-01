using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class Fragment : IFragment
{
    private readonly long _includeCondition;
    private readonly long _deferIfCondition;

    public Fragment(
        int id,
        IObjectType typeCondition,
        ISyntaxNode syntaxNode,
        IReadOnlyList<DirectiveNode> directives,
        ISelectionSet selectionSet,
        long includeCondition,
        long deferIfCondition,
        bool isInternal = false)
    {
        Id = id;
        TypeCondition = typeCondition;
        SyntaxNode = syntaxNode;
        Directives = directives;
        SelectionSet = selectionSet;
        _includeCondition = includeCondition;
        _deferIfCondition = deferIfCondition;
        IsInternal = isInternal;
    }

    public int Id { get; }

    public IObjectType TypeCondition { get; }

    public ISyntaxNode SyntaxNode { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public ISelectionSet SelectionSet { get; }

    public bool IsInternal { get; }

    public bool IsConditional => _includeCondition is not 0 || _deferIfCondition is not 0;

    public string? GetLabel(IVariableValueCollection variables)
        => Directives.GetDeferDirective(variables)?.Label;

    public bool IsIncluded(long includeFlags, bool allowInternals = false)
        => (includeFlags & _includeCondition) == _includeCondition &&
            (_deferIfCondition is 0 || (includeFlags & _deferIfCondition) != _deferIfCondition) &&
            (!IsInternal || allowInternals);
}
