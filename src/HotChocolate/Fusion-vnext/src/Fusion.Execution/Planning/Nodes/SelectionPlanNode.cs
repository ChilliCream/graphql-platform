using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

/// <summary>
/// The base class for plan nodes that can have child selections.
/// </summary>
public abstract class SelectionPlanNode : PlanNode
{
    private List<CompositeDirective>? _directives;
    private List<SelectionPlanNode>? _selections;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionPlanNode"/>.
    /// </summary>
    /// <param name="declaringType">
    /// The type on which this selection is declared on.
    /// </param>
    /// <param name="selectionNodes">
    /// The child selection syntax nodes of this selection.
    /// </param>
    protected SelectionPlanNode(
        ICompositeNamedType declaringType,
        IReadOnlyList<ISelectionNode>? selectionNodes)
    {
        DeclaringType = declaringType;
        IsEntity = declaringType.IsEntity();
        SelectionNodes = selectionNodes;
    }

    /// <summary>
    /// Gets the type on which this selection is declared on.
    /// </summary>
    public ICompositeNamedType DeclaringType { get; }

    /// <summary>
    /// Defines if the selection is declared on an entity type.
    /// </summary>
    public bool IsEntity { get; }

    /// <summary>
    /// Gets the directives that are annotated to this selection.
    /// </summary>
    public IReadOnlyList<CompositeDirective> Directives
        => _directives ?? (IReadOnlyList<CompositeDirective>)Array.Empty<CompositeDirective>();

    /// <summary>
    /// Gets the child selection syntax nodes of this selection.
    /// </summary>
    public IReadOnlyList<ISelectionNode>? SelectionNodes { get; }

    /// <summary>
    /// Gets the child selections of this selection.
    /// </summary>
    public IReadOnlyList<SelectionPlanNode> Selections
        => _selections ?? (IReadOnlyList<SelectionPlanNode>)Array.Empty<SelectionPlanNode>();

    /// <summary>
    /// Adds a child selection to this selection.
    /// </summary>
    /// <param name="selection">
    /// The child selection that shall be added.
    /// </param>
    public void AddSelection(SelectionPlanNode selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if(selection is OperationPlanNode)
        {
            throw new NotSupportedException(
                "An operation cannot be a child of a selection.");
        }

        (_selections ??= []).Add(selection);
        selection.Parent = this;
    }

    /// <summary>
    /// Adds a directive to the selection.
    /// </summary>
    /// <param name="directive">
    /// The directive that shall be added.
    /// </param>
    public void AddDirective(CompositeDirective directive)
    {
        ArgumentNullException.ThrowIfNull(directive);
        (_directives ??= []).Add(directive);
    }
}
