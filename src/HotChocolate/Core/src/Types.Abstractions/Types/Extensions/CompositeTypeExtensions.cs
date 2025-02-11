#pragma warning disable IDE0130
using System.Reflection;

namespace HotChocolate.Types;
#pragma warning restore IDE0130

public static class HotChocolateBaseTypeExtensions
{
    public static ITypeDefinition NamedType(this IType type)
        => type.Kind switch
        {
            TypeKind.NonNull => NamedType(((INonNullType)type).NullableType),
            TypeKind.List => NamedType(((IListType)type).ElementType),
            TypeKind.Object or
                TypeKind.Interface or
                TypeKind.Union or
                TypeKind.InputObject or
                TypeKind.Enum or
                TypeKind.Scalar => (ITypeDefinition)type,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    public static bool Equals(this IType thisType, IType? otherType, TypeComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(thisType, nameof(thisType));

        if(otherType is null)
        {
            return false;
        }

        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(thisType, otherType);
        }

        return thisType.IsStructurallyEqual(otherType);
    }

    private static bool IsStructurallyEqual(this IType thisType, IType otherType)
    {
        if (thisType.Kind != otherType.Kind)
        {
            return false;
        }

        if(thisType.Kind == TypeKind.NonNull)
        {
            return IsStructurallyEqual(((INonNullType)thisType).NullableType, ((INonNullType)otherType).NullableType);
        }

        if(thisType.Kind == TypeKind.List)
        {
            return IsStructurallyEqual(((IListType)thisType).ElementType, ((IListType)otherType).ElementType);
        }

        if(thisType.Kind == TypeKind.Object
            || thisType.Kind == TypeKind.Interface
            || thisType.Kind == TypeKind.Union
            || thisType.Kind == TypeKind.InputObject
            || thisType.Kind == TypeKind.Enum
            || thisType.Kind == TypeKind.Scalar)
        {
            return ((ITypeDefinition)thisType).Name.Equals(((ITypeDefinition)otherType).Name);
        }

        throw new InvalidOperationException("The specified type kind is not supported.");
    }
}
