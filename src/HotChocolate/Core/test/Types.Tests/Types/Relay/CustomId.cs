namespace HotChocolate.Types.Relay;

public struct CustomId
{
    public CustomId(int value)
    {
        Value = value;
    }

    public int Value { get; }
}
