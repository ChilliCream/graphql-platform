using System.Text.Json;

namespace HotChocolate.Fusion.Execution;

public class LeafFieldResult : FieldResult
{
    public JsonElement Value { get; set; }

    public override void SetNextValueNull()
    {
        Value = default;
    }

    public override void SetNextValue(JsonElement value)
    {
        Value = value;
    }

    public override void Reset()
    {
        Value = default;
        base.Reset();
    }
}
