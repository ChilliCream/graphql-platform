using System.Collections.Immutable;
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
    private List<SelectionSetNode>? _requirements;
    private List<UnresolvableSelection>? _unresolvableSelections;
    private bool? _isConditional;
    private string? _skipVariable;
    private string? _includeVariable;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionPlanNode"/>.
    /// </summary>
    /// <param name="declaringType">
    /// The type on which this selection is declared on.
    /// </param>
    /// <param name="directiveNodes">
    /// The directives applied to this selection.
    /// </param>
    /// <param name="selectionNodes">
    /// The child selection syntax nodes of this selection.
    /// </param>
    protected SelectionPlanNode(
        ICompositeNamedType declaringType,
        IReadOnlyList<DirectiveNode> directiveNodes,
        IReadOnlyList<ISelectionNode>? selectionNodes)
    {
        DeclaringType = declaringType;
        IsEntity = declaringType.IsEntity();
        DirectiveNodes = directiveNodes;
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
    /// Gets the directives nodes that are annotated to the selection.
    /// </summary>
    public IReadOnlyList<DirectiveNode> DirectiveNodes { get; }

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
    /// Gets the requirements that are needed to execute this selection.
    /// </summary>
    public IReadOnlyList<SelectionSetNode> RequirementNodes
        => _requirements ?? (IReadOnlyList<SelectionSetNode>)Array.Empty<SelectionSetNode>();

    public IReadOnlyList<UnresolvableSelection> UnresolvableSelections =>
        _unresolvableSelections ?? (IReadOnlyList<UnresolvableSelection>)Array.Empty<UnresolvableSelection>();

    /// <summary>
    /// Defines if the selection is conditional.
    /// </summary>
    public bool IsConditional
    {
        get
        {
            InitializeConditions();
            return _isConditional ?? false;
        }
    }

    /// <summary>
    /// Gets the skip variable name if the selection is conditional.
    /// </summary>
    public string? SkipVariable
    {
        get
        {
            InitializeConditions();
            return _skipVariable;
        }
        set
        {
            _skipVariable = value;
            _isConditional = _skipVariable is not null || _includeVariable is not null;
        }
    }

    /// <summary>
    /// Gets the include variable name if the selection is conditional.
    /// </summary>
    public string? IncludeVariable
    {
        get
        {
            InitializeConditions();
            return _includeVariable;
        }
        set
        {
            _includeVariable = value;
            _isConditional = _skipVariable is not null || _includeVariable is not null;
        }
    }

    /// <summary>
    /// Adds a child selection to this selection.
    /// </summary>
    /// <param name="selection">
    /// The child selection that shall be added.
    /// </param>
    public void AddSelection(SelectionPlanNode selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (selection is OperationPlanNode)
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

    public bool RemoveDirective(CompositeDirective directive)
        => _directives?.Remove(directive) == true;

    public void AddRequirementNode(SelectionSetNode selectionSet)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);
        (_requirements ??= []).Add(selectionSet);
    }

    public void AddUnresolvableSelection(ISelectionNode selectionNode, ImmutableStack<SelectionPlanNode> path)
    {
        ArgumentNullException.ThrowIfNull(selectionNode);
        ArgumentNullException.ThrowIfNull(path);

        (_unresolvableSelections ??= []).Add(new UnresolvableSelection(selectionNode, path));
    }

    public void AddUnresolvableSelection(UnresolvableSelection unresolvable)
    {
        ArgumentNullException.ThrowIfNull(unresolvable);
        (_unresolvableSelections ??= []).Add(unresolvable);
    }

    public void ClearUnresolvableSelections()
    {
        _unresolvableSelections?.Clear();
    }

    private void InitializeConditions()
    {
        if (_isConditional.HasValue)
        {
            return;
        }

        foreach (var directive in DirectiveNodes)
        {
            if (_skipVariable is not null && _includeVariable is not null)
            {
                break;
            }

            if (directive.Name.Value.Equals("skip"))
            {
                _skipVariable = GetVariableName(directive);
                continue;
            }

            if (directive.Name.Value.Equals("include"))
            {
                _includeVariable = GetVariableName(directive);
            }
        }

        _isConditional = _skipVariable is not null || _includeVariable is not null;
        return;

        string? GetVariableName(DirectiveNode directive)
        {
            var ifArgument = directive.Arguments.FirstOrDefault(t => t.Name.Value.Equals("if"));

            if (ifArgument?.Value is VariableNode variableNode)
            {
                return variableNode.Name.Value;
            }

            return null;
        }
    }
}
