namespace HotChocolate.Data.ExpressionNodes;

public readonly record struct Identifier(int Value);

public struct IdentifierRegistry
{
    private int _current = 0;

    public IdentifierRegistry()
    {
    }

    public Identifier Next() => new(_current++);
}

public readonly record struct Identifier<T>(Identifier Id)
{
    public int Value => Id.Value;
    public static implicit operator Identifier(Identifier<T> id) => id.Id;
}
