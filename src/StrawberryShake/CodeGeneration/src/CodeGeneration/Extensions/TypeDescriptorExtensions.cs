namespace StrawberryShake.CodeGeneration.Extensions
{
    public static class TypeDescriptorExtensions
    {
        public static bool IsLeafType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor.Kind == TypeKind.LeafType;

        public static bool IsEntityType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor.Kind == TypeKind.EntityType;

        public static bool IsDataType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor.Kind == TypeKind.DataType;

        public static bool IsInterface(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is NamedTypeDescriptor { IsInterface: true };

        public static bool IsNullableType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is not NonNullTypeDescriptor;

        public static bool IsNonNullableType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is NonNullTypeDescriptor;

        public static bool IsListType(this ITypeDescriptor typeDescriptor) =>
            typeDescriptor is ListTypeDescriptor ||
            typeDescriptor is NonNullTypeDescriptor { InnerType: ListTypeDescriptor };

        public static ITypeDescriptor InnerType(this ITypeDescriptor typeDescriptor)
        {
            if (typeDescriptor is NonNullTypeDescriptor n)
            {
                return n.InnerType;
            }

            if (typeDescriptor is ListTypeDescriptor l)
            {
                return l.InnerType;
            }

            return typeDescriptor;
        }
    }
}
