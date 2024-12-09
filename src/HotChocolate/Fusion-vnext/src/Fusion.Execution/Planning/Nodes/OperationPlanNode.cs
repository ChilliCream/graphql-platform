using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

/// <summary>
/// Represents an operation to resolve data from a specific source schema.
/// </summary>
public sealed class OperationPlanNode : SelectionPlanNode
{
    private static readonly IReadOnlyDictionary<string, VariableDefinitionNode> _emptyVariableMap =
        new Dictionary<string, VariableDefinitionNode>();
    private readonly List<OperationPlanNode> _dependants = [];
    // private List<OperationPlanNode>? _operations;
    private Dictionary<string, FieldRequirementPlanNode>? _requirements;
    private Dictionary<string, VariableDefinitionNode>? _variables;

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType declaringType,
        SelectionSetNode selectionSet,
        PlanNode? parent = null)
        : base(declaringType, [], selectionSet.Selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType declaringType,
        IReadOnlyList<ISelectionNode> selections,
        PlanNode? parent = null)
        : base(declaringType, [], selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public string SchemaName { get; }

    // todo: variable representations are missing.
    // todo: how to we represent state?

    public IReadOnlyDictionary<string, FieldRequirementPlanNode> Requirements
        => _requirements ??= new Dictionary<string, FieldRequirementPlanNode>();

    public IReadOnlyDictionary<string, VariableDefinitionNode> VariableDefinitions
        => _variables ?? _emptyVariableMap;

    public IReadOnlyList<OperationPlanNode> Dependants => _dependants;

    public void AddRequirement(FieldRequirementPlanNode requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        (_requirements ??= new Dictionary<string, FieldRequirementPlanNode>()).Add(requirement.Name, requirement);
        requirement.Parent = this;
    }

    public void AddVariableDefinition(VariableDefinitionNode variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        (_variables ??= new Dictionary<string, VariableDefinitionNode>()).Add(variable.Variable.Name.Value, variable);
    }

    public void AddDependantOperation(OperationPlanNode operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _dependants.Add(operation);
    }

    public OperationDefinitionNode ToSyntaxNode()
    {
        return new OperationDefinitionNode(
            null,
            null,
            OperationType.Query,
            VariableDefinitions.Values.OrderBy(t => t.Variable.Name.Value).ToArray(),
            Directives.ToSyntaxNode(),
            Selections.ToSyntaxNode());
    }
}
