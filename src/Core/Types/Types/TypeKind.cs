namespace HotChocolate.Types
{
    public enum TypeKind
    {
        Interface = 0,
        Object = 1,
        Union = 2,
        InputObject = 4,
        Enum = 8,
        Scalar = 16,
        List = 32,
        NonNull = 64
    }
}
