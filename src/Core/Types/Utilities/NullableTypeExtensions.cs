using System.Reflection;

#nullable enable

namespace HotChocolate.Utilities
{
    internal static class NullableTypeExtensions
    {
        public static IExtendedType GetExtendedReturnType(this PropertyInfo property)
        {
            return new NullableHelper(property.DeclaringType).GetPropertyInfo(property);
        }

        public static IExtendedMethodTypeInfo GetExtendedMethodTypeInfo(this MethodInfo method)
        {
            return new NullableHelper(method.DeclaringType).GetMethodInfo(method);
        }
    }
}
