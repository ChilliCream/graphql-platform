namespace HotChocolate.Fusion.Execution;

public class ObjectFieldResult : FieldResult
{
    public ObjectResult? Value { get; set; }

    public override void SetNextValueNull()
    {
        Value = null;
    }

    public override void SetNextValue(ResultData value)
    {
        if (value is not ObjectResult objectResult)
        {
            throw new ArgumentException("Value is not a ObjectResult.", nameof(value));
        }

        Value = objectResult;
    }

    public override void Reset()
    {
        Value = null;
        base.Reset();
    }
}
