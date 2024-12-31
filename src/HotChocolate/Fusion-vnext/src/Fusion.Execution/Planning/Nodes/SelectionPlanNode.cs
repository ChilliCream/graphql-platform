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
    private List<DataRequirement>? _requirements;
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
    public IReadOnlyList<DataRequirement> DataRequirements
        => _requirements ?? (IReadOnlyList<DataRequirement>)Array.Empty<DataRequirement>();

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

    public void AddDataRequirement(SelectionSetNode selectionSet, ImmutableStack<SelectionPlanNode> path)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);
        ArgumentNullException.ThrowIfNull(path);
        (_requirements ??= []).Add(new DataRequirement(selectionSet, path));
    }

    public void AddDataRequirement(DataRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        (_requirements ??= []).Add(requirement);
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

    public bool TryMoveRequirementsToParent()
    {
        if (Parent is not SelectionPlanNode parentSelection)
        {
            return false;
        }

        return TryMoveRequirementsTo(parentSelection);
    }

    public bool TryMoveRequirementsTo(SelectionPlanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_unresolvableSelections is not null)
        {
            foreach (var unresolvable in _unresolvableSelections)
            {
                node.AddUnresolvableSelection(unresolvable);
            }

            _unresolvableSelections.Clear();
            _unresolvableSelections = null;
        }

        if(_requirements is not null)
        {
            foreach (var requirement in _requirements)
            {
                node.AddDataRequirement(requirement);
            }

            _requirements.Clear();
            _requirements = null;
        }

        return true;
    }

    public IReadOnlyList<SelectionSetNode> TakeDataRequirements(DataRequirementKind kind = DataRequirementKind.All)
    {
        if(_requirements is null && _unresolvableSelections is null)
        {
            return Array.Empty<SelectionSetNode>();
        }

        var requirements = new List<SelectionSetNode>();

        if (_requirements is not null && kind.HasFlag(DataRequirementKind.DataRequirements))
        {
            foreach (var (selectionSet, path) in _requirements)
            {
                requirements.Add(CreateSelectionSetFromPath(this, selectionSet, path));
            }

            _requirements.Clear();
            _requirements = null;
        }

        if (_unresolvableSelections is not null && kind.HasFlag(DataRequirementKind.UnresolvableSelections))
        {
            foreach (var (selection, path) in _unresolvableSelections)
            {
                requirements.Add(CreateSelectionSetFromPath(this, new SelectionSetNode([selection]), path));
            }

            _unresolvableSelections.Clear();
            _unresolvableSelections = null;
        }

        return requirements;
    }

    private static SelectionSetNode CreateSelectionSetFromPath(
        SelectionPlanNode parent,
        SelectionSetNode selectionSet,
        ImmutableStack<SelectionPlanNode> path)
    {
        path = path.Pop(out var segment);

        if(ReferenceEquals(segment, parent))
        {
            return selectionSet;
        }

        var current = CreateSelectionFromNode(segment, selectionSet);

        while (!path.IsEmpty)
        {
            path = path.Pop(out segment);

            if (ReferenceEquals(segment, parent))
            {
                return new SelectionSetNode([current]);
            }

            current = CreateSelectionFromNode(segment, new SelectionSetNode([current]));
        }

        return new SelectionSetNode([current]);
    }

    private static ISelectionNode CreateSelectionFromNode(
        SelectionPlanNode node,
        SelectionSetNode selectionSet)
    {
        switch (node)
        {
            case FieldPlanNode field:
                return field.FieldNode.WithSelectionSet(selectionSet);

            case InlineFragmentPlanNode fragment:
                return new InlineFragmentNode(
                    null,
                    new NamedTypeNode(fragment.DeclaringType.Name),
                    fragment.DirectiveNodes,
                    selectionSet);

            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
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

[Flags]
public enum DataRequirementKind
{
    UnresolvableSelections = 1,
    DataRequirements = 2,
    All = DataRequirements | UnresolvableSelections,
}
