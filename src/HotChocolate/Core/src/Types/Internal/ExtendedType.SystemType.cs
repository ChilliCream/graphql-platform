#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class ExtendedType
{
    private static class SystemType
    {
        public static ExtendedType FromType(Type type, TypeCache cache) =>
            cache.GetOrCreateType(type, () => FromTypeInternal(type, cache));

        private static ExtendedType FromTypeInternal(Type type, TypeCache cache)
        {
            type = Helper.RemoveNonEssentialTypes(type);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var inner = type.GetGenericArguments()[0];

                return new ExtendedType(
                    inner,
                    ExtendedTypeKind.Runtime,
                    typeArguments: GetGenericArguments(inner, cache),
                    source: type,
                    isNullable: true);
            }

            var elementType =
                Helper.GetInnerListType(type) is { } e
                    ? FromType(e, cache)
                    : null;

            var typeArguments =
                type.IsArray && elementType is not null
                    ? new[] { elementType, }
                    : GetGenericArguments(type, cache);

            return new ExtendedType(
                type,
                ExtendedTypeKind.Runtime,
                typeArguments: typeArguments,
                source: type,
                isNullable: !type.IsValueType,
                isList: Helper.IsListType(type),
                elementType: elementType);
        }

        public static IReadOnlyList<ExtendedType> GetGenericArguments(
            Type type,
            TypeCache cache)
        {
            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();
                var extendedArguments = new ExtendedType[arguments.Length];

                for (var i = 0; i < arguments.Length; i++)
                {
                    extendedArguments[i] = ExtendedType.FromType(arguments[i], cache);
                }

                return extendedArguments;
            }

            return Array.Empty<ExtendedType>();
        }
    }
}
