using System.Text.Json;

namespace HotChocolate.Fusion.Planning.Nodes;

public static class SerializationHelper
{
    public static void WriteChildNodes(Utf8JsonWriter writer, IPlanNodeProvider planNodeProvider)
    {
        if (planNodeProvider.Nodes.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("nodes");
        writer.WriteStartArray();

        foreach (var node in planNodeProvider.Nodes.OfType<ISerializablePlanNode>())
        {
            node.Serialize(writer);
        }

        writer.WriteEndArray();
    }

    public static void WriteKind(Utf8JsonWriter writer, ISerializablePlanNode serializablePlanNode)
    {
        writer.WriteString("kind", serializablePlanNode.Kind.ToString());
    }
}
