using System.Collections.Generic;
using System.Text.Json;

namespace HotChocolate.Execution.Processing.Plan;

internal readonly struct StreamPlanNode
{
    public StreamPlanNode(int id, QueryPlanNode root)
    {
        Id = id;
        Root = root;
    }

    public int Id { get; }

    public QueryPlanNode Root { get; }

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", Id);
        writer.WritePropertyName("root");
        Root.Serialize(writer);
        writer.WriteEndObject();
    }

    public object Serialize()
        => new Dictionary<string, object> { { "id", Id }, { "root", Root.Serialize() } };
}
