using HotChocolate.Language;
using HotChocolate.Properties;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// Provides utility methods to work with <see cref="IType"/>.
/// </summary>
public static class TypeExtensions
{
    internal static IInputType EnsureInputType(this IType type)
    {
        if (type.NamedType() is not IInputType)
        {
            throw InputTypeExpected(type);
        }

        return (IInputType)type;
    }

    internal static IOutputType EnsureOutputType(this IType type)
    {
        if (type.NamedType() is not IOutputType)
        {
            throw OutputTypeExpected(type);
        }

        return (IOutputType)type;
    }

    public static string TypeName(this IType type)
        => type.NamedType().Name;

    public static Type ToRuntimeType(this IType type)
    {
        if (type.IsListType())
        {
            var elementType = ToRuntimeType(type.ElementType());
            return typeof(List<>).MakeGenericType(elementType);
        }

        if (type.IsLeafType())
        {
            return LeafTypeToRuntimeType(type);
        }

        if (type.IsNonNullType())
        {
            return ToRuntimeType(type.InnerType());
        }

        if (type is IRuntimeTypeProvider t)
        {
            return t.RuntimeType;
        }

        return typeof(object);
    }

    private static Type LeafTypeToRuntimeType(IType type)
    {
        if (type.IsLeafType() && type.NamedType() is IRuntimeTypeProvider t)
        {
            if (!type.IsNonNullType() && t.RuntimeType.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(t.RuntimeType);
            }

            return t.RuntimeType;
        }

        throw new NotSupportedException();
    }

    public static ITypeNode RenameName(this ITypeNode typeNode, string name)
        => typeNode switch
        {
            NonNullTypeNode nonNull => new NonNullTypeNode((INullableTypeNode)RenameName(nonNull.Type, name)),
            ListTypeNode list => new ListTypeNode(RenameName(list.Type, name)),
            NamedTypeNode named => named.WithName(named.Name.WithValue(name)),
            _ => throw new NotSupportedException(TypeResources.TypeExtensions_KindIsNotSupported)
        };

    public static bool IsInstanceOfType(this IInputType type, IValueNode literal)
    {
        ArgumentNullException.ThrowIfNull(literal);

        while (true)
        {
            if (literal.Kind is SyntaxKind.NullValue)
            {
                return type.Kind is not TypeKind.NonNull;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    type = (IInputType)((NonNullType)type).NullableType;
                    continue;

                case TypeKind.List:
                    if (literal.Kind is SyntaxKind.ListValue)
                    {
                        var list = (ListValueNode)literal;

                        if (list.Items.Count == 0)
                        {
                            return true;
                        }

                        literal = list.Items[0];
                    }

                    type = (IInputType)((ListType)type).ElementType;
                    continue;

                case TypeKind.InputObject:
                    return literal.Kind == SyntaxKind.ObjectValue;

                default:
                    return ((ILeafType)type).IsValueCompatible(literal);
            }
        }
    }

    public static IType RewriteToNullableType(this IType type)
        => type.Kind is TypeKind.NonNull
            ? type.InnerType()
            : type;
}
