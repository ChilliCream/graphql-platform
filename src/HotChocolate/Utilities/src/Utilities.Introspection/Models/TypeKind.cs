#pragma warning disable CA1812
#pragma warning disable CA1720
#pragma warning disable CA1707
#nullable disable

namespace HotChocolate.Utilities.Introspection;

public enum TypeKind
{
    INTERFACE = 0,
    OBJECT = 1,
    UNION = 2,
    INPUT_OBJECT = 4,
    ENUM = 8,
    SCALAR = 16,
    LIST = 32,
    NON_NULL = 64,
}
#pragma warning restore CA1707
#pragma warning restore CA1720
#pragma warning restore CA1812
