namespace HotChocolate.Stitching.Introspection.Models
{
    public enum TypeKind
    {
        Interface = 0,
        Object = 1,
        Union = 2,
        Input_Object = 4,
        Enum = 8,
        Scalar = 16,
        List = 32,
        Non_Null = 64
    }
}
