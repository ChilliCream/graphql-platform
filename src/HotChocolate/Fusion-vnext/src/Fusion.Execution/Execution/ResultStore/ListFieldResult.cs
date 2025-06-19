namespace HotChocolate.Fusion.Execution;

public class ListFieldResult : FieldResult
{
    public ListResult? Value { get; set; }

    public override void SetNextValueNull()
    {
        Value = null;
    }

    public override void SetNextValue(ResultData value)
    {
        if (value is not ListResult listResult)
        {
            throw new ArgumentException("Value is not a ListResult.", nameof(value));
        }

        Value = listResult;
    }

    public override void Reset()
    {
        Value = null;
        base.Reset();
    }
}
