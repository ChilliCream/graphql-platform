using System;
using System.Reflection;

namespace HotChocolate.Types.Descriptors;

public static class TypeInspectorExtensions
{
    public static TypeReference GetInputReturnTypeRef(
        this ITypeInspector typeInspector,
        MemberInfo member)
    {
        return typeInspector.GetReturnTypeRef(member, TypeContext.Input);
    }

    public static TypeReference GetInputTypeRef(
        this ITypeInspector typeInspector,
        Type type)
    {
        return typeInspector.GetReturnTypeRef(type, TypeContext.Input);
    }

    public static TypeReference GetOutputReturnTypeRef(
        this ITypeInspector typeInspector,
        MemberInfo member)
    {
        return typeInspector.GetReturnTypeRef(member, TypeContext.Output);
    }

    public static TypeReference GetOutputTypeRef(
        this ITypeInspector typeInspector,
        Type type)
    {
        return typeInspector.GetTypeRef(type, TypeContext.Output);
    }
}
