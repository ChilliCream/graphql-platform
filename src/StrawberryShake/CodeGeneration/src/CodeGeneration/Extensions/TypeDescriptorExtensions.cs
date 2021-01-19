namespace StrawberryShake.CodeGeneration.Extensions
{
    public static class TypeDescriptorExtensions
    {
        public static bool IsLeafType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor.Kind == TypeKind.LeafType;
        }

        public static bool IsEntityType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor.Kind == TypeKind.EntityType;
        }

        public static bool IsDataType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor.Kind == TypeKind.DataType;
        }

        public static bool IsNullableType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor is not NonNullTypeDescriptor;
        }

        public static bool IsListType(this ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor is ListTypeDescriptor;
        }
    }
}
