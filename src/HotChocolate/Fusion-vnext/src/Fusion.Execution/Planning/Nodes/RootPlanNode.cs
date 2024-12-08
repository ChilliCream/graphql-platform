using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class RootPlanNode : PlanNode, IPlanNodeProvider, ISerializablePlanNode
{
    private static readonly JsonWriterOptions SerializerOptions = new()
    {
        Indented = true,
    };
    private readonly List<PlanNode> _nodes = [];

    public IReadOnlyList<PlanNode> Nodes => _nodes;

    public void AddChildNode(PlanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes.Add(node);
        node.Parent = this;
    }

    public PlanNodeKind Kind => PlanNodeKind.Root;

    public void Serialize(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        using var jsonWriter = new Utf8JsonWriter(writer, SerializerOptions);
        Serialize(jsonWriter);
    }

    public void Serialize(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        SerializationHelper.WriteKind(writer, this);
        SerializationHelper.WriteChildNodes(writer, this);
        writer.WriteEndObject();

        writer.Flush();
    }
}
