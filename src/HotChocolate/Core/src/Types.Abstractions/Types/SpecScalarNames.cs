namespace HotChocolate.Types;

public static class SpecScalarNames
{
    public const string String = nameof(String);

    public const string Boolean = nameof(Boolean);

    public const string Float = nameof(Float);

    public const string ID = nameof(ID);

    public const string Int = nameof(Int);

    public static bool IsSpecScalar(string name)
        => name switch
        {
            String => true,
            Boolean => true,
            Float => true,
            ID => true,
            Int => true,
            _ => false
        };
}
