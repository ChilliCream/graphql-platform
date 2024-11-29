using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class ExtendedType
{
    private static class SchemaType
    {
        public static ExtendedType FromType(Type type, TypeCache cache) =>
            FromType(type, null, true, cache);

        private static ExtendedType FromType(
            Type type,
            Type? source,
            bool nullable,
            TypeCache cache)
        {
            type = Helper.RemoveNonEssentialTypes(type);

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();

                if (definition == typeof(NonNullType<>))
                {
                    return FromType(type.GetGenericArguments()[0], type, false, cache);
                }

                if (definition == typeof(ListType<>))
                {
                    return cache.GetOrCreateType(
                        source is not null ? source : type,
                        () =>
                        {
                            var elementType =
                                FromType(type.GetGenericArguments()[0], null, true, cache);

                            return new ExtendedType(
                                type,
                                ExtendedTypeKind.Schema,
                                typeArguments: new[] { elementType, },
                                source: source,
                                definition: typeof(ListType<>),
                                isNullable: nullable,
                                isList: true,
                                elementType: elementType);
                        });
                }
            }

            return cache.GetOrCreateType(
                source ?? type,
                () =>
                {
                    var definition = type.IsGenericType
                        ? type.GetGenericTypeDefinition()
                        : null;

                    return new ExtendedType(
                        type,
                        ExtendedTypeKind.Schema,
                        typeArguments: SystemType.GetGenericArguments(type, cache),
                        source: source,
                        definition: definition,
                        isNullable: nullable,
                        isNamedType: true);
                });
        }
    }
}
