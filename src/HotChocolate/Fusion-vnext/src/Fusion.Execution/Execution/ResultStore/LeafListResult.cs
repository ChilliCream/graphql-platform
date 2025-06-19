using System.Text.Json;

namespace HotChocolate.Fusion.Execution;

public class LeafListResult : ListResult
{
    public List<JsonElement> Items { get; } = [];

    public override void SetNextValueNull()
    {
        Items.Add(default);
    }

    public override void SetNextValue(JsonElement value)
    {
        Items.Add(value);
    }

    public override void Reset()
    {
        Items.Clear();
        base.Reset();
    }
}
