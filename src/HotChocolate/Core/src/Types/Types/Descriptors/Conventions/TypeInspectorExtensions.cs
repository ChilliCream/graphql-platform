using System;
using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public static class TypeInspectorExtensions
    {
        public static ITypeReference GetInputReturnTypeRef(
            this ITypeInspector typeInspector,
            MemberInfo member)
        {
            return typeInspector.GetReturnTypeRef(member, TypeContext.Input);
        }

        public static ITypeReference GetInputTypeRef(
            this ITypeInspector typeInspector,
            Type type)
        {
            return typeInspector.GetReturnTypeRef(type, TypeContext.Input);
        }

        public static ITypeReference GetOutputReturnTypeRef(
            this ITypeInspector typeInspector,
            MemberInfo member)
        {
            return typeInspector.GetReturnTypeRef(member, TypeContext.Output);
        }

        public static ITypeReference GetOutputTypeRef(
            this ITypeInspector typeInspector,
            Type type)
        {
            return typeInspector.GetTypeRef(type, TypeContext.Output);
        }
    }
}
