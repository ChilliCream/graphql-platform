#nullable enable

namespace HotChocolate.Internal;

public static class ExtendedTypeExtensions
{
    public static bool IsAssignableFrom(
        this Type type,
        IExtendedType extendedType) =>
        type.IsAssignableFrom(extendedType.Type);
}
