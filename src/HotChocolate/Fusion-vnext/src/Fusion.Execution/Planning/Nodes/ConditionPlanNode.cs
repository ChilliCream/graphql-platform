using System.Text.Json;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class ConditionPlanNode : PlanNode, ISerializablePlanNode, IPlanNodeProvider
{
    private readonly List<PlanNode> _nodes = [];

    public ConditionPlanNode(
        string variableName,
        bool passingValue,
        PlanNode? parent = null)
    {
        VariableName = variableName;
        PassingValue = passingValue;
        Parent = parent;
    }

    /// <summary>
    /// The name of the variable that controls if this node is executed.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// The value the <see cref="VariableName"/> has to be, in order
    /// for this node to be executed.
    /// </summary>
    public bool PassingValue { get; }

    public IReadOnlyList<PlanNode> Nodes => _nodes;

    public void AddChildNode(PlanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes.Add(node);
        node.Parent = this;
    }

    public PlanNodeKind Kind => PlanNodeKind.Condition;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        SerializationHelper.WriteKind(writer, this);
        writer.WriteString("variableName", VariableName);
        writer.WriteBoolean("passingValue", PassingValue);
        SerializationHelper.WriteChildNodes(writer, this);
        writer.WriteEndObject();
    }
}

public record Condition(string VariableName, bool PassingValue);
