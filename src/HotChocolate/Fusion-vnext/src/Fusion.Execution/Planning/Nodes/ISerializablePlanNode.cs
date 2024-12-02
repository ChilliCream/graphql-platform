using System.Text.Json;

namespace HotChocolate.Fusion.Planning;

public interface ISerializablePlanNode
{
    PlanNodeKind Kind { get; }

    void Serialize(Utf8JsonWriter writer);
}
