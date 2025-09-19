namespace HotChocolate.Fusion.Types;

[Flags]
public enum ScalarValueKind
{
    Any = 0,
    String = 1,
    Integer = 2,
    Float = 4,
    Boolean = 8,
    Object = 16,
    List = 32
}
