using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Utilities
{
    internal static class NullableTypeExtensions
    {
        public static IExtendedType GetExtendedReturnType(this PropertyInfo property)
        {
            return NullableHelper.GetReturnType(property);
        }

        public static IExtendedMethodTypeInfo GetExtendedMethodTypeInfo(this MethodInfo method)
        {
            return NullableHelper.GetExtendedMethodInfo(method);
        }
    }
}
