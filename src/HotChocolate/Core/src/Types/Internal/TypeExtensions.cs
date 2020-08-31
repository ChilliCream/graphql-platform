using System;

namespace HotChocolate.Internal
{
    public static class TypeExtensions
    {
        public static bool IsSchemaType(this Type type) =>
            ExtendedType.Tools.IsSchemaType(type);

        internal static bool IsNonGenericSchemaType(this Type type) =>
            ExtendedType.Tools.IsNonGenericBaseType(type);
    }
}
