namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal static class __TypeKind
{
    public static ReadOnlySpan<byte> Scalar => "SCALAR"u8;
    public static ReadOnlySpan<byte> Object => "OBJECT"u8;
    public static ReadOnlySpan<byte> Interface => "INTERFACE"u8;
    public static ReadOnlySpan<byte> Union => "UNION"u8;
    public static ReadOnlySpan<byte> Enum => "ENUM"u8;
    public static ReadOnlySpan<byte> InputObject => "INPUT_OBJECT"u8;
    public static ReadOnlySpan<byte> List => "LIST"u8;
    public static ReadOnlySpan<byte> NonNull => "NON_NULL"u8;
}
