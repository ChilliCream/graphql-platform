namespace HotChocolate.Fusion.Types;

[Flags]
public enum ScalarResultType
{
    Unknown = 0,
    String = 1,
    Int = 2,
    Float = 4,
    Boolean = 8
}
