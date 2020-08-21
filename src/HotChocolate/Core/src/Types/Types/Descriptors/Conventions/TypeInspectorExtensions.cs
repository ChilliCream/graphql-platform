using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public static class TypeInspectorExtensions
    {
        public static ITypeReference GetInputReturnType(
            this ITypeInspector typeInspector,
            MemberInfo member)
        {
            return typeInspector.GetReturnTypeRef(member, TypeContext.Input);
        }

        public static ITypeReference GetOutputReturnType(
            this ITypeInspector typeInspector,
            MemberInfo member)
        {
            return typeInspector.GetReturnTypeRef(member, TypeContext.Output);
        }
    }
}
