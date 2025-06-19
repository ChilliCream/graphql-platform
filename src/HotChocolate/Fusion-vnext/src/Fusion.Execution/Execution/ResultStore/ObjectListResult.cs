namespace HotChocolate.Fusion.Execution;

public class ObjectListResult : ListResult
{
    public List<ObjectResult?> Items { get; } = [];

    public override void SetNextValueNull()
    {
        Items.Add(null);
    }

    public override void SetNextValue(ResultData value)
    {
        if (value is not ObjectResult objectResult)
        {
            throw new ArgumentException("Value is not a ObjectResult.", nameof(value));
        }

        Items.Add(objectResult);
    }

    public override void Reset()
    {
        Items.Clear();
        base.Reset();
    }
}
