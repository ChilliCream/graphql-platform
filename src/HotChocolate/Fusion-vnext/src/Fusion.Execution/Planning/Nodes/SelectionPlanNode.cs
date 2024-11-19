using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The base class for plan nodes that can have child selections.
/// </summary>
public abstract class SelectionPlanNode : PlanNode
{
    private List<CompositeDirective>? _directives;
    private List<SelectionPlanNode>? _selections;

    protected SelectionPlanNode(
        ICompositeNamedType type,
        IReadOnlyList<ISelectionNode>? selectionNodes)
    {
        Type = type;
        IsEntity = type.IsEntity();
        SelectionNodes = selectionNodes;
    }

    public ICompositeNamedType Type { get; }

    public bool IsEntity { get; }

    public IReadOnlyList<CompositeDirective> Directives
        => _directives ?? (IReadOnlyList<CompositeDirective>)Array.Empty<CompositeDirective>();

    public IReadOnlyList<ISelectionNode>? SelectionNodes { get; }

    public IReadOnlyList<SelectionPlanNode> Selections
        => _selections ?? (IReadOnlyList<SelectionPlanNode>)Array.Empty<SelectionPlanNode>();

    public void AddSelection(SelectionPlanNode selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        (_selections ??= []).Add(selection);
        selection.Parent = this;
    }

    public void AddDirective(CompositeDirective selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        (_directives ??= []).Add(selection);
    }
}
