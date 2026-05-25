namespace HotChocolate.AspNetCore.Parsers;

internal class KeyPathSegment : IVariablePathSegment
{
    public KeyPathSegment(string value, IVariablePathSegment? next)
    {
        Value = value;
        Next = next;
    }

    public string Value { get; }

    public IVariablePathSegment? Next { get; }

    public override string ToString() => Value;
}
