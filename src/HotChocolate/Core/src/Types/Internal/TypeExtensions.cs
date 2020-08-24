using System;

namespace HotChocolate.Internal
{
    public static class TypeExtensions
    {
        public static bool IsSchemaType(this Type type) =>
            ExtendedType.IsSchemaTypeInternal(type);

        internal static bool IsNonGenericSchemaType(this Type type) =>
            ExtendedType.IsSchemaTypeInternal(type);
    }
}
