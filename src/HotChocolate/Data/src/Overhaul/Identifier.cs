namespace HotChocolate.Data.ExpressionNodes;

public readonly record struct Identifier(int Value)
{
    public int AsIndex() => Value - 1;
    public static Identifier FromIndex(int i) => new(i + 1);
}

public struct SequentialIdentifierGenerator
{
    private int _current = 0;

    public SequentialIdentifierGenerator()
    {
    }

    public Identifier Next() => new(++_current);
    public readonly int Count => _current;
}

public readonly record struct Identifier<T>(Identifier Id)
{
    public int Value => Id.Value;
    public static implicit operator Identifier(Identifier<T> id) => id.Id;
}
