using System.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

/// <summary>
/// Represents an operation to resolve data from a specific source schema.
/// </summary>
public sealed class OperationPlanNode : SelectionPlanNode, IPlanNodeProvider, ISerializablePlanNode
{
    private static readonly IReadOnlyDictionary<string, VariableDefinitionNode> _emptyVariableMap =
        new Dictionary<string, VariableDefinitionNode>();
    private readonly List<PlanNode> _nodes = [];
    private Dictionary<string, VariableDefinitionNode>? _variables;

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType declaringType,
        SelectionSetNode selectionSet,
        PlanNode? parent = null)
        : base(declaringType, selectionSet.Selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType declaringType,
        IReadOnlyList<ISelectionNode> selections,
        PlanNode? parent = null)
        : base(declaringType, selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public string SchemaName { get; }

    // todo: variable representations are missing.
    // todo: how to we represent state?

    public IReadOnlyDictionary<string, VariableDefinitionNode> VariableDefinitions
        => _variables ?? _emptyVariableMap;

    public IReadOnlyList<PlanNode> Nodes => _nodes;

    public void AddVariableDefinition(VariableDefinitionNode variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        (_variables ??= new Dictionary<string, VariableDefinitionNode>()).Add(variable.Variable.Name.Value, variable);
    }

    public void AddChildNode(PlanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes.Add(node);
        node.Parent = this;
    }

    public OperationDefinitionNode ToSyntaxNode()
    {
        return new OperationDefinitionNode(
            null,
            null,
            OperationType.Query,
            _variables?.Values.OrderBy(t => t.Variable.Name.Value).ToArray() ?? [],
            Directives.ToSyntaxNode(),
            Selections.ToSyntaxNode());
    }

    public PlanNodeKind Kind => PlanNodeKind.Operation;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        SerializationHelper.WriteKind(writer, this);
        writer.WriteString("schema", SchemaName);
        writer.WriteString("document", ToSyntaxNode().ToString(indented: false));
        SerializationHelper.WriteChildNodes(writer, this);
        writer.WriteEndObject();
    }
}
