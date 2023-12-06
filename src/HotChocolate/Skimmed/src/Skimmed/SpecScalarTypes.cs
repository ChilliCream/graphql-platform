namespace HotChocolate.Skimmed;

public static class SpecScalarTypes
{
    public const string String = "String";
    public const string Boolean = "Boolean";
    public const string Float = "Float";
    public const string ID = "ID";
    public const string Int = "Int";

    private static readonly HashSet<string> _specScalars =
        new(StringComparer.Ordinal)
        {
            String,
            Boolean,
            Float,
            ID,
            Int
        };

    public static bool IsSpecScalar(string name)
        => _specScalars.Contains(name);
}
