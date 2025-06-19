namespace HotChocolate.Fusion.Execution;

public class NestedListResult : ListResult
{
    public List<ListResult?> Items { get; } = [];

    public override void SetNextValueNull()
    {
        Items.Add(null);
    }

    public override void SetNextValue(ResultData value)
    {
        if (value is not ListResult listResult)
        {
            throw new ArgumentException("Value is not a ListResult.", nameof(value));
        }

        Items.Add(listResult);
    }

    public override void Reset()
    {
        Items.Clear();
        base.Reset();
    }
}
