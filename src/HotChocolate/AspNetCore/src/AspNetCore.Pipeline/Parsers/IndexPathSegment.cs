namespace HotChocolate.AspNetCore.Parsers;

internal class IndexPathSegment : IVariablePathSegment
{
    public IndexPathSegment(int value, IVariablePathSegment? next)
    {
        Value = value;
        Next = next;
    }

    public int Value { get; }

    public IVariablePathSegment? Next { get; }

    public override string ToString() => Value.ToString();
}
